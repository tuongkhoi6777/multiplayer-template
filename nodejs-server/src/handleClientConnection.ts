import EventEmitter from "events";
import { PLAYER, sendResponse } from "./server";
import { Room } from "./room";
import * as WebSocket from 'ws';

const emitter = new EventEmitter();

class ClientConnectionHandler {
    generateRejoinRoomEventName(userId: string) {
        return `RejoinRoom:${userId}`;
    }

    generateReconnectEventName(userId: string) {
        return `ReconnectServer:${userId}`;
    }

    // Handle client reconnecting to WebSocket with a new session by disconnecting the old one
    handleClientReconnect(user: PLAYER, ws: WebSocket) {
        let eventName = this.generateReconnectEventName(user.userInfo.userId);
        emitter.emit(eventName);

        let listener = () => {
            // If the same user connects from a new session, disconnect the old one
            sendResponse(ws, 'connection', false, "Same user connected from another session!");
            ws.close();
        };
        emitter.on(eventName, listener);

        // Emit rejoin event if the player was in a room before disconnecting
        const rejoinEventName = this.generateRejoinRoomEventName(user.userInfo.userId);
        emitter.emit(rejoinEventName, user);

        return listener;
    }

    // Remove reconnect listener when the client disconnects
    removeReconnectListener(userId: string, listener: () => void) {
        let eventName = this.generateReconnectEventName(userId);
        emitter.off(eventName, listener);
    }

    // Register a listener for rejoining when a client joins a room
    registerRejoinListener(player: PLAYER, room: Room) {
        const eventName = this.generateRejoinRoomEventName(player.userInfo.userId);

        emitter.on(eventName, (user) => {
            room.sendServerConnectionInfo(player.ws, "rejoinGame");
            room.addPlayer(user);
        });
    }

    // Remove all rejoin listeners when a client leaves a room
    clearRejoinListeners(userId: string) {
        const eventName = this.generateRejoinRoomEventName(userId);
        emitter.removeAllListeners(eventName);
    }
}

export const handler = new ClientConnectionHandler();