import { execFile } from 'child_process';
import path from 'path';
import { localIp, PLAYER, removeRoomAndReleasePort, sendMessage, USER_INFO } from './server';

const executablePathWindows = path.resolve(__dirname, '../build-server/FPS.exe');
const executablePathLinux = path.resolve(__dirname, '../build-linux-server/build-linux-server.x86_64');

export class Room {
    players: { team: number, data: PLAYER }[] = []; // value of team is 0 and 1
    isGameStarted: boolean = false;

    // Constructor to initialize properties
    constructor(public roomId: string, public port: number, public host: PLAYER, public name: string) {
        this.addPlayer(host);
    }

    getRoomInfo() {
        return {
            roomName: this.name,
            host: this.host.userInfo.userId,
            players: this.players.map(player => {
                return {
                    id: player.data.userInfo.userId,
                    name: player.data.userInfo.name,
                    team: player.team,
                }
            })
        };
    }

    // Create list of websockets for all players from both teams
    getWebSocketList() {
        return this.players.map(player => player.data.ws);
    }

    // Call whenever room is updated, like when a player is added or removed
    onRoomUpdate() {
        let wsList = this.getWebSocketList();
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
        // Check if player has already joined
        let playerList = this.players.map(player => player.data.userInfo.userId);
        if (playerList.includes(user.userInfo.userId)) {
            return { success: false, message: "Player already joined!" };
        }

        // Check if room has space for another player
        if (this.players.length >= 10) {
            return { success: false, message: "Room is full!" };
        }

        // Auto-join the team with fewest players
        let team = this.getAllTeamWithPlayers();

        // Find team index that has fewest players
        let minIndex = team.reduce((minIndex, current, index) => {
            return current.length < team[minIndex].length ? minIndex : index;
        }, 0);

        // Add new player with team index 
        this.players.push({ team: minIndex, data: user })

        // Notify room update to all players in the room
        this.onRoomUpdate();

        // Update the user's current room ID
        user.currentRoomId = this.roomId;

        // Return room info to the new player
        return { success: true, message: "Join room successfully!" };
    }

    findPlayerIndex(userId: string) {
        return this.players.findIndex(player => player.data.userInfo.userId == userId);
    }

    // Change player team
    changePlayerTeam(userId: string, teamIndex: number) {
        let index = this.findPlayerIndex(userId);

        // False if player not found
        if (!this.players[index]) return false;

        this.players[index].team = teamIndex;
        this.onRoomUpdate();

        return true;
    }

    // Remove a player from the room
    removePlayer(userId: string, isKicked: boolean) {
        // Try to remove player from team 1
        let index = this.findPlayerIndex(userId);
        let result = null;
        if (index >= 0) {
            result = this.players.splice(index, 1)[0]
        }

        // False if player not found
        if (!result) return false;

        // Update player's current room ID to null
        result.data.currentRoomId = null;

        // Notify player if they have been kicked
        if (isKicked) {
            sendMessage(result.data.ws, {
                type: "playerKicked",
                data: null,
                success: true,
                message: "You have been kicked by host!"
            });
        }

        // Check if room is empty after player removal
        if (this.isEmpty()) {
            // If room is empty, remove room and release port
            removeRoomAndReleasePort(this.roomId, this.port);
        } else {
            // Change host to other player if host is remove from room
            if (this.isHost(userId)) {
                this.host = this.players[0].data;
            }

            // Notify room update for other players
            this.onRoomUpdate();
        }

        return true;
    }

    // Check if the room is empty
    isEmpty() {
        return this.players.length === 0;
    }

    // Check if the player is the host and if the game has not started
    canStartGame(userId: string): boolean {
        return this.isHost(userId) && !this.isGameStarted;
    }

    // Check if the user is the host of the room
    isHost(userId: string): boolean {
        return this.host.userInfo.userId === userId;
    }

    findArrayWithMinLength(arr: USER_INFO[][]): USER_INFO[] {
        return arr.reduce((minArray, currentArray) =>
            currentArray.length < minArray.length ? currentArray : minArray
        );
    }

    getAllTeamWithPlayers() {
        this.players.map(player => {
            return {
                id: player.data.userInfo.userId,
                name: player.data.userInfo.name,
                team: player.team,
            }
        })
        let teams: USER_INFO[][] = [];
        this.players.forEach(e => {
            !teams[e.team] && (teams[e.team] = [])
            teams[e.team].push(e.data.userInfo);
        })

        return teams;
    }

    // Start the game in the room
    startGame() {
        this.isGameStarted = true;

        const initData = {
            port: this.port,
            players: this.getRoomInfo().players,
        };

        // Convert the object to a JSON string
        const jsonString = JSON.stringify(initData);

        // Execute the game server process
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
            if (code === 0) {
                // Game exited normally
            }

            this.endGame();
        });

        // Listen for error events (if the Mirror server fails to start)
        mirrorServer.on('error', (err) => {
            console.error('Failed to start Mirror server:', err);
        });

        setTimeout(() => {
            this.onServerReady();
        }, 15000)
    }

    onServerReady() {
        console.log("SERVER_FORCE_READY");

        // Notify players to connect to the Mirror server
        let wsList = this.getWebSocketList();
        wsList.forEach(ws => sendMessage(ws, {
            type: "startGame",
            data: {
                serverIp: localIp,
                serverPort: this.port,
            },
            success: true,
            message: ""
        }));
    }

    // End the game
    endGame() {
        this.isGameStarted = false;

        // Optional cleanup logic for ending the game can go here, such as resetting game state or notifying players.
    }
}