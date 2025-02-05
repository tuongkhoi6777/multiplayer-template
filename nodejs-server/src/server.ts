import * as WebSocket from 'ws';
import { Room } from './room';
import { validateToken } from './authentication';
import { IncomingMessage } from 'http';
import * as os from 'os';
import * as crypto from 'crypto';
import { handler } from './handleClientConnection';

export interface USER_INFO {
    userId: string,
    name: string,
}

export interface PLAYER {
    ws: WebSocket,
    userInfo: USER_INFO,
    currentRoomId: string | null
}

export interface SEND_MESSAGE_FORMAT {
    type: string,
    data: any,
    success: boolean,
    message: string
}

export function sendMessage(ws: WebSocket, payload: SEND_MESSAGE_FORMAT) {
    const { type, data, success, message } = payload;
    ws.send(JSON.stringify({ type, data, success, message }));
}

interface MESSAGE {
    type: string,
    payload: any,
    key: string,
}

const getLocalIp = () => {
    const networkInterfaces = os.networkInterfaces();

    for (const ifaceName in networkInterfaces) {
        const ifaceConfigs = networkInterfaces[ifaceName];
        if (!ifaceConfigs) continue; // Skip undefined or null

        for (const config of ifaceConfigs) {
            // Look for IPv4 addresses that are not internal
            if (config.family === 'IPv4' && !config.internal) {
                return config.address;
            }
        }
    }

    return null; // No valid IP address found
};

export const localIp = getLocalIp();

const runningPort = 8080;
const availablePorts = Array.from({ length: 65535 - 10000 }, (_, i) => i + 10000); // Array of available ports
const allocatedPorts: Set<number> = new Set<number>(); // Set of used ports
const rooms = new Map<string, Room>();

// Create WebSocket server instance
const wss = new WebSocket.Server({
    port: runningPort,
    verifyClient: (info, done) => {
        const token = getToken(info.req.url);

        if (token && validateToken(token)) {
            done(true); // Accept connection
        } else {
            done(false, 403, 'Forbidden'); // Reject connection
        }
    },
});

console.log(`WebSocket server is running on ws://${localIp}:${runningPort}`);

wss.on('connection', (ws: WebSocket, req: IncomingMessage) => {
    const token = getToken(req.url);
    const info = validateToken(token);

    // check if user token is valid and send response
    if (!token || !info) {
        sendResponse(ws, 'connection', false, "Invalid token!");
        ws.close(); // Close the connection immediately
        return;
    }    

    let player: PLAYER = { ws, userInfo: info, currentRoomId: null };
    sendResponse(ws, 'connection', true, "Success!", player.userInfo);

    let listener = handler.handleClientReconnect(player, ws);

    ws.on('message', (data: WebSocket.Data) => {
        const msg = typeof data === "string" ? data : data.toString('utf8')
        const parsed: MESSAGE = JSON.parse(msg);

        console.log(parsed);

        switch (parsed.type) {
            case "getInfo": {
                sendResponse(ws, parsed.key, true, "Success!", player.userInfo);
                break;
            }
            case "createRoom": {
                handleCreateRoom(ws, player, parsed);
                break;
            }
            case "joinRoom": {
                handleJoinRoom(ws, player, parsed);
                break;
            }
            case "changeTeam": {
                handleChangeTeam(ws, player, parsed);
                break;
            }
            case "kickPlayer": {
                handleKickPlayer(ws, player, parsed);
                break;
            }
            case "exitRoom": {
                handleExitRoom(ws, player, parsed);
                break;
            }
            case "getAllRooms": {
                handleGetAllRooms(ws, parsed);
                break;
            }
            case "startGame": {
                handleStartGame(ws, player, parsed);
                break;
            }
            default:
                sendResponse(ws, parsed.key, false, `Unknown action: ${parsed.type}`);
        }

    });

    ws.on('close', () => {
        // handle player disconnect
        handler.removeReconnectListener(info.userId, listener);
        handlePlayerDisconnect(player);
    });
});

// Get token from url query string
function getToken(url: string | undefined) {
    if (!url) return null;

    const urlParams = new URLSearchParams(url.split('?')[1]);
    return urlParams.get('token');
}

// Helper function to send responses, with type is the request key from client
export function sendResponse(ws: WebSocket, type: string, success: boolean, message: string, data: any = null) {
    sendMessage(ws, { type, success, message, data });
}

// Handle creating a new room
async function handleCreateRoom(ws: WebSocket, player: PLAYER, parsed: MESSAGE) {
    const { roomName } = parsed.payload;
    if (!roomName) return handleInvalidRequest(ws, parsed.type);

    let success = createNewRoom(player, roomName);
    if (!success) {
        sendResponse(ws, parsed.key, false, "No room available at current time, try again later!");
        return;
    }

    sendResponse(ws, parsed.key, true, "Success!");
}

// Handle joining a room
async function handleJoinRoom(ws: WebSocket, player: PLAYER, parsed: MESSAGE) {
    const { roomId } = parsed.payload;
    if (!roomId) return handleInvalidRequest(ws, parsed.type);

    const room = getRoomOrNotify(ws, roomId, parsed.key);
    if (!room) return;

    const { success, message } = room.addPlayer(player);
    sendResponse(ws, parsed.key, success, message);
}

// Handle joining a room
async function handleChangeTeam(ws: WebSocket, player: PLAYER, parsed: MESSAGE) {
    const roomId = player.currentRoomId;
    if (!roomId) return handleInvalidRequest(ws, parsed.type);

    const { teamIndex } = parsed.payload;
    if (
        typeof teamIndex !== 'number' || // Must be a number
        isNaN(teamIndex) ||             // Must not be NaN
        !Number.isFinite(teamIndex) ||  // Must be finite
        teamIndex < 0                   // Must be non-negative
    ) {
        return handleInvalidRequest(ws, parsed.type);
    }

    const room = getRoomOrNotify(ws, roomId, parsed.key);
    if (!room) return;

    const success = room.updatePlayerTeam(player.userInfo.userId, teamIndex);
    const message = success ? "Success!" : `Can't find yourself in the room!`;
    sendResponse(ws, parsed.key, success, message);
}

// Handle kicking a player from a room
async function handleKickPlayer(ws: WebSocket, player: PLAYER, parsed: MESSAGE) {
    const { playerId } = parsed.payload;
    const roomId = player.currentRoomId;
    if (!roomId || !playerId) return handleInvalidRequest(ws, parsed.type);

    const room = getRoomOrNotify(ws, roomId, parsed.key);
    if (!room) return;

    if (!room.isHost(player.userInfo.userId) || player.userInfo.userId === playerId) {
        sendResponse(ws, parsed.key, false, "You can't kick yourself or don't have permission!");
        return;
    }

    let success = room.removePlayer(playerId, true);
    let message = success ? "Success!" : `Player ${playerId} is no longer in the room!`;
    sendResponse(ws, parsed.key, success, message);
}

// Handle exiting a room
async function handleExitRoom(ws: WebSocket, player: PLAYER, parsed: MESSAGE) {
    const roomId = player.currentRoomId;
    if (!roomId) return handleInvalidRequest(ws, parsed.type);

    const room = getRoomOrNotify(ws, roomId, parsed.key);
    if (!room) return;

    let success = room.removePlayer(player.userInfo.userId, false);
    let message = success ? "Success!" : "You are no longer in this room!";
    sendResponse(ws, parsed.key, success, message);
}

// Handle getting all room details
async function handleGetAllRooms(ws: WebSocket, parsed: MESSAGE) {
    sendResponse(ws, parsed.key, true, "Success!", getRoomDetails());
}

// Handle starting the game
async function handleStartGame(ws: WebSocket, player: PLAYER, parsed: MESSAGE) {
    const roomId = player.currentRoomId;
    if (!roomId) return handleInvalidRequest(ws, parsed.type);

    const room = getRoomOrNotify(ws, roomId, parsed.key);
    if (!room) return;

    const canStart = room.canStartGame(player.userInfo.userId);
    const message = canStart ? "Success!" : "You don't have permission or the game already started!"
    sendResponse(ws, parsed.key, canStart, message);

    if (canStart) room.startGame();
}

function handleInvalidRequest(ws: WebSocket, type: string) {
    sendResponse(ws, type, false, `Request is invalid!`);
}

// Get room by ID or send an error message
function getRoomOrNotify(ws: WebSocket, roomId: string, type: string): Room | undefined {
    const room = rooms.get(roomId);
    if (!room) {
        sendResponse(ws, type, false, `Room with ID ${roomId} not found!`);
    }
    return room;
}

// Create a new room
function createNewRoom(player: PLAYER, name: string) {
    let port = availablePorts.shift();
    if (port) {
        let roomId = crypto.randomUUID(); // Generate a unique room ID
        rooms.set(roomId, new Room(roomId, port, player, name));
        allocatedPorts.add(port);
    }
    return !!port;
}

// Get all rooms details
function getRoomDetails() {
    return {
        list: Array.from(rooms.values()).map(room => ({
            roomId: room.roomId,
            roomName: room.name,
            numOfPlayers: room.players.length,
            isStarted: room.isGameStarted,
        }))
    };
}

// Handle cleanup, remove player from room
function handlePlayerDisconnect(player: PLAYER) {
    if (player.currentRoomId) {
        // Find current room of player and remove them
        const room = rooms.get(player.currentRoomId);
        room && room.removePlayer(player.userInfo.userId, false);
    }
    
    // Clear WebSocket and user info references properly
    player.ws.terminate();
    player.ws = undefined as any;
    player.userInfo = undefined as any;
}

// Execute when all players exit room
export function removeRoomAndReleasePort(roomId: string, port: number) {
    rooms.delete(roomId);
    allocatedPorts.delete(port) && availablePorts.push(port);
}