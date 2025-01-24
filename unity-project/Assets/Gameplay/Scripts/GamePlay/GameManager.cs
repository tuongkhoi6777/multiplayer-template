using System;
using System.Threading.Tasks;
using Core;
using Mirror;
using UI;
using UnityEditor;
using UnityEngine;

namespace GamePlay
{
    public enum GameState
    {
        WarmUp, // warm up state, waiting for all clients to connect
        PreGame, // after all clients connect, count down 3s to start the game
        InGame, // active game play state
        EndGame, // end game and show result
    }

    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }
        public Team team1 = new();
        public Team team2 = new();
        public Transform spawn1;
        public Transform spawn2;
        public GameState gameState = GameState.WarmUp;

        void Awake()
        {
            Instance = this;
        }

        public async void StartGameServer(PlayerRoomInfo[] playerRoomInfos)
        {
            foreach (var player in playerRoomInfos)
            {
                var team = player.team == 0 ? team1 : team2;
                team.players.Add(player.id);
            }

            await Task.Delay(1 * 60 * 1000);
            EndGameServer();
        }

        public void OnPlayerJoin(NetworkConnectionToClient conn)
        {
            if (conn.authenticationData is string playerId)
            {
                if (team1.players.Contains(playerId))
                {
                    SpawnPlayer(spawn1, conn);
                }
                else if (team2.players.Contains(playerId))
                {
                    SpawnPlayer(spawn2, conn);
                }
            }
        }

        public void SpawnPlayer(Transform spawn, NetworkConnectionToClient conn)
        {
            var playerPrefab = NetworkManager.singleton.playerPrefab;
            GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);
            player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
            NetworkServer.AddPlayerForConnection(conn, player);
        }

        public async void EndGameServer()
        {
            // notify clients to end game
            RpcEndGame();

            // wait a little bit for all clients to handle end game
            await Task.Delay(1000);

            // stop server
            NetworkManager.singleton.StopServer();

            // wait a little bit for server to stop and clean up
            await Task.Delay(1000);

            // exit game server
            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false; // Stop play mode in the editor
#endif
        }

        [ClientRpc]
        public void RpcEndGame()
        {
            // stop and disconnect client
            NetworkManager.singleton.StopClient();

            // TODO: show end scene or result scene shere
            SceneManagerCustom.Instance.LoadScene(SceneManagerCustom.SceneLobby);
        }

        public void EndRoundServer()
        {

        }
    }
}