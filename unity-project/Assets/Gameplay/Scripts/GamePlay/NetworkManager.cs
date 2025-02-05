using Core;
using kcp2k;
using Mirror;
using Newtonsoft.Json.Linq;
using UI;
using UnityEngine;

namespace GamePlay
{
    public class NetworkManagerCustom : NetworkManager
    {
        public override void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (VariableManager.IsDebug)
            {
                gameObject.AddComponent<NetworkManagerHUD>();
                return;
            }

            // If headless, mostly dedicated server
            if (Mirror.Utils.IsHeadless())
            {
                // Retrieve command line arguments
                // Deserialize into a JObject
                JObject parsedJson = JObject.Parse(VariableManager.CommandLineArgs[1]);

                // Accessing values dynamically
                (transport as KcpTransport).port = parsedJson["port"].Value<ushort>();
                var players = parsedJson["players"].Value<JArray>().ToObject<PlayerRoomInfo[]>();

                // Start the server with the custom port
                StartServer();

                GameManager.Instance.StartGameServer(players);
            }
            else
            {
                networkAddress = VariableManager.ServerAddress;
                (transport as KcpTransport).port = VariableManager.ServerPort;

                StartClient();
            }
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            GameManager.Instance.OnPlayerJoin(conn);
        }

        public override void OnClientDisconnect()
        {
            // TODO: show popup disconnect
            Debug.Log("Disconnected from server due to network problems.");

            base.OnClientDisconnect();
        }
    }
}