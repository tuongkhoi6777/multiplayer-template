"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Room = void 0;
const path_1 = __importDefault(require("path"));
const child_process_1 = require("child_process");
const server_1 = require("./server");
const handleClientConnection_1 = require("./handleClientConnection");
const executablePathWindows = path_1.default.resolve(__dirname, '../build-server/FPS.exe');
const executablePathLinux = path_1.default.resolve(__dirname, '../build-linux-server/build-linux-server.x86_64');
var UNITY_MESSAGE;
(function (UNITY_MESSAGE) {
    UNITY_MESSAGE["SERVER_READY"] = "SERVER_READY";
    UNITY_MESSAGE["GAME_OVER"] = "GAME_OVER";
})(UNITY_MESSAGE || (UNITY_MESSAGE = {}));
class Room {
    constructor(roomId, port, host, name) {
        this.roomId = roomId;
        this.port = port;
        this.host = host;
        this.name = name;
        this.players = []; // Value of team is 0 or 1
        this.isGameStarted = false;
        this.addPlayer(host);
    }
    getRoomInfo() {
        return {
            roomName: this.name,
            host: this.host.userInfo.userId,
            players: this.players.map(player => ({
                id: player.data.userInfo.userId,
                name: player.data.userInfo.name,
                team: player.team,
            }))
        };
    }
    // Notify all players in the room about updates
    notifyRoomUpdate() {
        let wsList = this.players.map(player => player.data.ws);
        let data = this.getRoomInfo();
        wsList.forEach(ws => (0, server_1.sendMessage)(ws, {
            type: "roomUpdate",
            data,
            success: true,
            message: "Success!"
        }));
    }
    // Add a player to the room
    addPlayer(user) {
        let findPlayer = this.players.find(player => player.data.userInfo.userId === user.userInfo.userId);
        // only rejoin allow if game is started
        if (this.isGameStarted) {
            if (findPlayer) {
                findPlayer.data = user;
                user.currentRoomId = this.roomId;
                return { success: true, message: "Rejoin room success" };
            }
            return { success: false, message: "You cannot join the room as the game has already started." };
        }
        if (findPlayer) {
            return { success: false, message: "Player already joined!" };
        }
        if (this.players.length >= 10) {
            return { success: false, message: "Room is full!" };
        }
        // Assign player to the team with fewer players
        let teamIndex = this.getTeamWithFewerPlayers();
        this.players.push({ team: teamIndex, data: user });
        this.notifyRoomUpdate();
        user.currentRoomId = this.roomId;
        return { success: true, message: "Joined room successfully!" };
    }
    findPlayerIndex(userId) {
        return this.players.findIndex(player => player.data.userInfo.userId === userId);
    }
    // Change player’s team
    updatePlayerTeam(userId, teamIndex) {
        let index = this.findPlayerIndex(userId);
        if (index === -1)
            return false;
        this.players[index].team = teamIndex;
        this.notifyRoomUpdate();
        return true;
    }
    // Remove a player from the room
    removePlayer(userId, isKicked) {
        // Return if game is started
        if (this.isGameStarted)
            return false;
        let index = this.findPlayerIndex(userId);
        if (index === -1)
            return false;
        let removedPlayer = this.players.splice(index, 1)[0];
        removedPlayer.data.currentRoomId = null;
        if (isKicked) {
            (0, server_1.sendMessage)(removedPlayer.data.ws, {
                type: "playerKicked",
                data: null,
                success: true,
                message: "You have been kicked by the host!"
            });
        }
        if (this.isEmpty()) {
            (0, server_1.removeRoomAndReleasePort)(this.roomId, this.port);
        }
        else {
            if (this.isHost(userId)) {
                this.host = this.players[0].data;
            }
            this.notifyRoomUpdate();
        }
        return true;
    }
    isEmpty() {
        return this.players.length === 0;
    }
    canStartGame(userId) {
        return this.isHost(userId) && !this.isGameStarted;
    }
    isHost(userId) {
        return this.host.userInfo.userId === userId;
    }
    getTeamWithFewerPlayers() {
        let teams = [];
        this.players.forEach(e => {
            if (!teams[e.team])
                teams[e.team] = [];
            teams[e.team].push(e.data.userInfo);
        });
        return teams.reduce((minIndex, current, index) => { var _a; return current.length < ((_a = teams[minIndex]) === null || _a === void 0 ? void 0 : _a.length) ? index : minIndex; }, 0);
    }
    // Start the game
    startGame() {
        var _a, _b;
        this.isGameStarted = true;
        const initData = {
            port: this.port,
            players: this.getRoomInfo().players,
        };
        const jsonString = JSON.stringify(initData);
        const mirrorServer = (0, child_process_1.execFile)(executablePathLinux, [jsonString, "-batchmode", "-nographics"], (error, stdout, stderr) => {
            if (error) {
                console.error('Error starting server:', error);
                return;
            }
        });
        // handle receive message from unity
        (_a = mirrorServer.stdout) === null || _a === void 0 ? void 0 : _a.on('data', (data) => {
            let str = data.toString().trim();
            let message = str.split("From Unity: ")[1];
            if (message) {
                switch (message) {
                    case UNITY_MESSAGE.SERVER_READY:
                        this.notifyPlayersServerReady();
                        break;
                    case UNITY_MESSAGE.GAME_OVER:
                        this.endGame();
                        break;
                }
                console.log(str);
            }
        });
        (_b = mirrorServer.stderr) === null || _b === void 0 ? void 0 : _b.on('data', (data) => {
            console.log('Error output from child process:', data.toString());
        });
        mirrorServer.on('exit', (code, signal) => {
            console.log(`Game Server exited with code ${code}, signal ${signal}`);
            if (code !== 0) {
                console.error(`Unexpected error while running game server.`);
            }
        });
        mirrorServer.on('error', (err) => console.error('Failed to start Mirror server:', err));
    }
    notifyPlayersServerReady() {
        this.players.forEach((player) => {
            this.sendServerConnectionInfo(player.data.ws, "startGame");
            handleClientConnection_1.handler.registerRejoinListener(player.data, this);
        });
    }
    sendServerConnectionInfo(ws, type) {
        (0, server_1.sendMessage)(ws, {
            type,
            data: {
                serverIp: server_1.localIp,
                serverPort: this.port,
            },
            success: true,
            message: ""
        });
    }
    endGame() {
        this.isGameStarted = false;
        this.players.forEach((player) => {
            handleClientConnection_1.handler.clearRejoinListeners(player.data.userInfo.userId);
        });
        this.notifyRoomUpdate();
    }
}
exports.Room = Room;
