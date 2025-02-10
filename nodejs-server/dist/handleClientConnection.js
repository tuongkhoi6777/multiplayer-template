"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.handler = void 0;
const events_1 = __importDefault(require("events"));
const server_1 = require("./server");
const emitter = new events_1.default();
class ClientConnectionHandler {
    generateRejoinRoomEventName(userId) {
        return `RejoinRoom:${userId}`;
    }
    generateReconnectEventName(userId) {
        return `ReconnectServer:${userId}`;
    }
    // Handle client reconnecting to WebSocket with a new session by disconnecting the old one
    handleClientReconnect(user, ws) {
        let eventName = this.generateReconnectEventName(user.userInfo.userId);
        emitter.emit(eventName);
        let listener = () => {
            // If the same user connects from a new session, disconnect the old one
            (0, server_1.sendResponse)(ws, 'disconnect', false, "Same user connected from another session!");
            ws === null || ws === void 0 ? void 0 : ws.close();
        };
        emitter.on(eventName, listener);
        // Emit rejoin event if the player was in a room before disconnecting
        const rejoinEventName = this.generateRejoinRoomEventName(user.userInfo.userId);
        emitter.emit(rejoinEventName, user);
        return listener;
    }
    // Remove reconnect listener when the client disconnects
    removeReconnectListener(userId, listener) {
        let eventName = this.generateReconnectEventName(userId);
        emitter.off(eventName, listener);
    }
    // Register a listener for rejoining when a client joins a room
    registerRejoinListener(player, room) {
        const eventName = this.generateRejoinRoomEventName(player.userInfo.userId);
        emitter.on(eventName, (user) => {
            room.sendServerConnectionInfo(user.ws, "reconnectGame");
            room.addPlayer(user);
        });
    }
    // Remove all rejoin listeners when a client leaves a room
    clearRejoinListeners(userId) {
        const eventName = this.generateRejoinRoomEventName(userId);
        emitter.removeAllListeners(eventName);
    }
}
exports.handler = new ClientConnectionHandler();
