using System.Threading.Tasks;
using Core;
using Mirror;
using UI;
using UnityEngine;

namespace GamePlay
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerRoomInfo[] players;

        void Awake()
        {
            Instance = this;
        }
        public async void StartGameServer(PlayerRoomInfo[] playerRoomInfos)
        {
            Debug.Log("SERVER_READY");

            players = playerRoomInfos;

            await Task.Delay(1 * 60 * 1000);

            EndGameServer();
        }

        [Command]
        public async void EndGameServer()
        {
            // notify for client to end game
            ClientExitGame();

            await Task.Delay(5000);

            // stop server after 10s
            NetworkManager.singleton.StopServer();

            await Task.Delay(1000);

            // exit game server
            Application.Quit();
        }

        [ClientRpc]
        public void ClientExitGame()
        {
            // stop and disconnect client
            NetworkManager.singleton.StopClient();

            // switch to lobby scene
            SceneManagerCustom.Instance.LoadScene(SceneManagerCustom.SceneLobby);
        }
    }
}