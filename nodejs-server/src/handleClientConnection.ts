import EventEmitter from "events";
import { PLAYER } from "./server";

const emitter = new EventEmitter();

class HandleClientConnection {
    getRejoinRoomEventName(userId: string) {
        return `RejoinRoom:${userId}`;
    }

    getReconnectServerEventName(userId: string) {
        return `ReconnectServer:${userId}`;
    }

    // when client reconnect to websocket with new session, disconnect the old one
    onConnectToServer(user: PLAYER, listener: () => void) {
        let eventName = this.getReconnectServerEventName(user.userInfo.userId);
        emitter.emit(eventName);
        emitter.on(eventName, listener);

        // emit rejoin room if player has disconnected when joined a room
        const rejoinEventName = this.getRejoinRoomEventName(user.userInfo.userId);
        emitter.emit(rejoinEventName, user);
    }

    // when client disconnect, remove reconnect listener
    onDisconnectFromServer(userId: string, listener: () => void) {
        let eventName = this.getReconnectServerEventName(userId);
        emitter.off(eventName, listener);
    }

    // add rejoin listener when client join room, so when client reconnect to server, they can join room directly
    onRoomStart(userId: string, listener: (user: PLAYER) => void) {
        const eventName = this.getRejoinRoomEventName(userId);
        emitter.on(eventName, listener);
    }

    // when client leave room, remove all rejoin listener
    onRoomEnd(userId: string) {
        const eventName = this.getRejoinRoomEventName(userId);
        emitter.removeAllListeners(eventName);
    }
}

export const handler = new HandleClientConnection();