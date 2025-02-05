import path from 'path';
import * as WebSocket from 'ws';
import { execFile } from 'child_process';
import { localIp, PLAYER, removeRoomAndReleasePort, sendMessage, USER_INFO } from './server';
import { handler } from './handleClientConnection';

const executablePathWindows = path.resolve(__dirname, '../build-server/FPS.exe');
const executablePathLinux = path.resolve(__dirname, '../build-linux-server/build-linux-server.x86_64');

export class Room {
    players: { team: number, data: PLAYER }[] = []; // Value of team is 0 or 1
    isGameStarted: boolean = false;

    constructor(public roomId: string, public port: number, public host: PLAYER, public name: string) {
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

        wsList.forEach(ws => sendMessage(ws, {
            type: "roomUpdate",
            data,
            success: true,
            message: "Success!"
        }));
    }

    // Add a player to the room
    addPlayer(user: PLAYER) {
        if (this.players.some(player => player.data.userInfo.userId === user.userInfo.userId)) {
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

    findPlayerIndex(userId: string) {
        return this.players.findIndex(player => player.data.userInfo.userId === userId);
    }

    // Change player’s team
    updatePlayerTeam(userId: string, teamIndex: number) {
        let index = this.findPlayerIndex(userId);
        if (index === -1) return false;

        this.players[index].team = teamIndex;
        this.notifyRoomUpdate();
        return true;
    }

    // Remove a player from the room
    removePlayer(userId: string, isKicked: boolean) {
        let index = this.findPlayerIndex(userId);
        if (index === -1) return false;

        let removedPlayer = this.players.splice(index, 1)[0];
        removedPlayer.data.currentRoomId = null;

        if (isKicked) {
            sendMessage(removedPlayer.data.ws, {
                type: "playerKicked",
                data: null,
                success: true,
                message: "You have been kicked by the host!"
            });
        }

        if (this.isEmpty()) {
            removeRoomAndReleasePort(this.roomId, this.port);
        } else {
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

    canStartGame(userId: string): boolean {
        return this.isHost(userId) && !this.isGameStarted;
    }

    isHost(userId: string): boolean {
        return this.host.userInfo.userId === userId;
    }

    getTeamWithFewerPlayers() {
        let teams: USER_INFO[][] = [];
        this.players.forEach(e => {
            if (!teams[e.team]) teams[e.team] = [];
            teams[e.team].push(e.data.userInfo);
        });

        return teams.reduce((minIndex, current, index) =>
            current.length < teams[minIndex]?.length ? index : minIndex, 0
        );
    }

    // Start the game
    startGame() {
        this.isGameStarted = true;

        const initData = {
            port: this.port,
            players: this.getRoomInfo().players,
        };

        const jsonString = JSON.stringify(initData);

        const mirrorServer = execFile(executablePathLinux, [jsonString, "-batchmode", "-nographics"],
            (error, stdout, stderr) => {
                if (error) {
                    console.error('Error starting server:', error);
                    return;
                }
            }
        );

        mirrorServer.stderr?.on('data', (data) => {
            console.log('Error output from child process:', data.toString());
        });

        mirrorServer.on('exit', (code, signal) => {
            console.log(`Game Server exited with code ${code}, signal ${signal}`);
            if (code !== 0) {
                console.error(`Unexpected error while running game server.`);
            }
            this.endGame();
        });
        mirrorServer.on('error', (err) => console.error('Failed to start Mirror server:', err));

        setTimeout(() => {
            this.notifyPlayersServerReady();
        }, 15000);
    }

    notifyPlayersServerReady() {
        console.log("SERVER_FORCE_READY");

        this.players.forEach((player) => {
            this.sendServerConnectionInfo(player.data.ws, "startGame");
            handler.registerRejoinListener(player.data, this);
        });
    }

    sendServerConnectionInfo(ws: WebSocket, type: string) {
        sendMessage(ws, {
            type,
            data: {
                serverIp: localIp,
                serverPort: this.port,
            },
            success: true,
            message: ""
        });
    }

    endGame() {
        this.isGameStarted = false;

        this.players.forEach((player) => {
            handler.clearRejoinListeners(player.data.userInfo.userId);
        });
    }
}
