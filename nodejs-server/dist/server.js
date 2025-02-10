"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.localIp = void 0;
exports.sendMessage = sendMessage;
exports.sendResponse = sendResponse;
exports.removeRoomAndReleasePort = removeRoomAndReleasePort;
const WebSocket = __importStar(require("ws"));
const room_1 = require("./room");
const authentication_1 = require("./authentication");
const os = __importStar(require("os"));
const crypto = __importStar(require("crypto"));
const handleClientConnection_1 = require("./handleClientConnection");
function sendMessage(ws, payload) {
    const { type, data, success, message } = payload;
    ws === null || ws === void 0 ? void 0 : ws.send(JSON.stringify({ type, data, success, message }));
}
const getLocalIp = () => {
    const networkInterfaces = os.networkInterfaces();
    for (const ifaceName in networkInterfaces) {
        const ifaceConfigs = networkInterfaces[ifaceName];
        if (!ifaceConfigs)
            continue; // Skip undefined or null
        for (const config of ifaceConfigs) {
            // Look for IPv4 addresses that are not internal
            if (config.family === 'IPv4' && !config.internal) {
                return config.address;
            }
        }
    }
    return null; // No valid IP address found
};
exports.localIp = getLocalIp();
const runningPort = 8080;
const availablePorts = Array.from({ length: 65535 - 10000 }, (_, i) => i + 10000); // Array of available ports
const allocatedPorts = new Set(); // Set of used ports
const rooms = new Map();
// Create WebSocket server instance
const wss = new WebSocket.Server({
    port: runningPort,
    verifyClient: (info, done) => {
        const token = getToken(info.req.url);
        if (token && (0, authentication_1.validateToken)(token)) {
            done(true); // Accept connection
        }
        else {
            done(false, 403, 'Forbidden'); // Reject connection
        }
    },
});
console.log(`WebSocket server is running on ws://${exports.localIp}:${runningPort}`);
wss.on('connection', (ws, req) => {
    const token = getToken(req.url);
    const info = (0, authentication_1.validateToken)(token);
    // check if user token is valid and send response
    if (!token || !info) {
        sendResponse(ws, 'connection', false, "Invalid token!");
        ws.close(); // Close the connection immediately
        return;
    }
    console.log(`New player connected, userId: ${info.userId}`);
    let player = { ws, userInfo: info, currentRoomId: null };
    sendResponse(ws, 'connection', true, "Success!", player.userInfo);
    let listener = handleClientConnection_1.handler.handleClientReconnect(player, ws);
    ws.on('message', (data) => {
        const msg = typeof data === "string" ? data : data.toString('utf8');
        const parsed = JSON.parse(msg);
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
        handleClientConnection_1.handler.removeReconnectListener(info.userId, listener);
        handlePlayerDisconnect(player);
    });
});
// Get token from url query string
function getToken(url) {
    if (!url)
        return null;
    const urlParams = new URLSearchParams(url.split('?')[1]);
    return urlParams.get('token');
}
// Helper function to send responses, with type is the request key from client
function sendResponse(ws, type, success, message, data = null) {
    sendMessage(ws, { type, success, message, data });
}
// Handle creating a new room
function handleCreateRoom(ws, player, parsed) {
    return __awaiter(this, void 0, void 0, function* () {
        const { roomName } = parsed.payload;
        if (!roomName)
            return handleInvalidRequest(ws, parsed.key);
        let success = createNewRoom(player, roomName);
        if (!success) {
            sendResponse(ws, parsed.key, false, "No room available at current time, try again later!");
            return;
        }
        sendResponse(ws, parsed.key, true, "Success!");
    });
}
// Handle joining a room
function handleJoinRoom(ws, player, parsed) {
    return __awaiter(this, void 0, void 0, function* () {
        const { roomId } = parsed.payload;
        if (!roomId)
            return handleInvalidRequest(ws, parsed.key);
        const room = getRoomOrNotify(ws, roomId, parsed.key);
        if (!room)
            return;
        const { success, message } = room.addPlayer(player);
        sendResponse(ws, parsed.key, success, message);
    });
}
// Handle joining a room
function handleChangeTeam(ws, player, parsed) {
    return __awaiter(this, void 0, void 0, function* () {
        const roomId = player.currentRoomId;
        if (!roomId)
            return handleInvalidRequest(ws, parsed.key);
        const { teamIndex } = parsed.payload;
        if (typeof teamIndex !== 'number' || // Must be a number
            isNaN(teamIndex) || // Must not be NaN
            !Number.isFinite(teamIndex) || // Must be finite
            teamIndex < 0 // Must be non-negative
        ) {
            return handleInvalidRequest(ws, parsed.key);
        }
        const room = getRoomOrNotify(ws, roomId, parsed.key);
        if (!room)
            return;
        const success = room.updatePlayerTeam(player.userInfo.userId, teamIndex);
        const message = success ? "Success!" : `Can't find yourself in the room!`;
        sendResponse(ws, parsed.key, success, message);
    });
}
// Handle kicking a player from a room
function handleKickPlayer(ws, player, parsed) {
    return __awaiter(this, void 0, void 0, function* () {
        const { playerId } = parsed.payload;
        const roomId = player.currentRoomId;
        if (!roomId || !playerId)
            return handleInvalidRequest(ws, parsed.key);
        const room = getRoomOrNotify(ws, roomId, parsed.key);
        if (!room)
            return;
        if (!room.isHost(player.userInfo.userId) || player.userInfo.userId === playerId) {
            sendResponse(ws, parsed.key, false, "You can't kick yourself or don't have permission!");
            return;
        }
        let success = room.removePlayer(playerId, true);
        let message = success ? "Success!" : `Player ${playerId} is no longer in the room!`;
        sendResponse(ws, parsed.key, success, message);
    });
}
// Handle exiting a room
function handleExitRoom(ws, player, parsed) {
    return __awaiter(this, void 0, void 0, function* () {
        const roomId = player.currentRoomId;
        if (!roomId)
            return handleInvalidRequest(ws, parsed.key);
        const room = getRoomOrNotify(ws, roomId, parsed.key);
        if (!room)
            return;
        let success = room.removePlayer(player.userInfo.userId, false);
        let message = success ? "Success!" : "You are no longer in this room!";
        sendResponse(ws, parsed.key, success, message);
    });
}
// Handle getting all room details
function handleGetAllRooms(ws, parsed) {
    return __awaiter(this, void 0, void 0, function* () {
        sendResponse(ws, parsed.key, true, "Success!", getRoomDetails());
    });
}
// Handle starting the game
function handleStartGame(ws, player, parsed) {
    return __awaiter(this, void 0, void 0, function* () {
        const roomId = player.currentRoomId;
        if (!roomId)
            return handleInvalidRequest(ws, parsed.key);
        const room = getRoomOrNotify(ws, roomId, parsed.key);
        if (!room)
            return;
        const canStart = room.canStartGame(player.userInfo.userId);
        const message = canStart ? "Success!" : "You don't have permission or the game already started!";
        sendResponse(ws, parsed.key, canStart, message);
        if (canStart)
            room.startGame();
    });
}
function handleInvalidRequest(ws, type) {
    sendResponse(ws, type, false, `Request is invalid!`);
}
// Get room by ID or send an error message
function getRoomOrNotify(ws, roomId, type) {
    const room = rooms.get(roomId);
    if (!room) {
        sendResponse(ws, type, false, `Room with ID ${roomId} not found!`);
    }
    return room;
}
// Create a new room
function createNewRoom(player, name) {
    let port = availablePorts.shift();
    if (port) {
        let roomId = crypto.randomUUID(); // Generate a unique room ID
        rooms.set(roomId, new room_1.Room(roomId, port, player, name));
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
function handlePlayerDisconnect(player) {
    if (player.currentRoomId) {
        // Find current room of player and remove them
        const room = rooms.get(player.currentRoomId);
        room && room.removePlayer(player.userInfo.userId, false);
    }
    // Clear WebSocket and user info references properly
    player.ws.terminate();
    player.ws = undefined;
}
// Execute when all players exit room
function removeRoomAndReleasePort(roomId, port) {
    rooms.delete(roomId);
    allocatedPorts.delete(port) && availablePorts.push(port);
}
