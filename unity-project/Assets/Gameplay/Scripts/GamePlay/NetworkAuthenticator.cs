using System;
using System.Linq;
using Core;
using Mirror;

namespace GamePlay
{
    public class NetworkAuthenticatorCustom : NetworkAuthenticator
    {
        public struct AuthRequestMessage : NetworkMessage
        {
            public string token;
        }

        public struct AuthResponseMessage : NetworkMessage
        {
            public bool success;
            public string message;
        }

        public bool VerifyToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;

            var team1 = GameManager.Instance.team1.players;
            var team2 = GameManager.Instance.team2.players;

            // check if any team has player
            return team1.Contains(token) || team2.Contains(token);
        }

        public override void OnStartServer()
        {
            // register a handler for the authentication request we expect from client
            NetworkServer.RegisterHandler((NetworkConnectionToClient conn, AuthRequestMessage msg) =>
            {
                AuthResponseMessage authResponseMessage = new()
                {
                    success = true,
                    message = "Connect to server successfully!",
                };

                // if token is empty or invalid, reject client
                if (!VerifyToken(msg.token))
                {
                    authResponseMessage.success = false;
                    authResponseMessage.message = "Don't have permission to connect!";

                    conn.isAuthenticated = false;

                    // maybe we should wait a little bit for client handle reject message before kick client
                    ServerReject(conn);
                }
                else
                {
                    conn.authenticationData = msg.token;

                    // after server accept invoke all OnServerAuthenticated
                    ServerAccept(conn);
                }

                conn.Send(authResponseMessage);
            }, false);
        }

        public override void OnStartClient()
        {
            // register a handler for the authentication response we expect from server
            NetworkClient.RegisterHandler((AuthResponseMessage msg) =>
            {
                if (msg.success)
                {
                    // TODO: handle connect success
                    // after client accept invoke all OnClientAuthenticated
                    ClientAccept();
                    return;
                }

                // TODO: handle connect fail
                NetworkManager.singleton.StopClient();
                SceneManagerCustom.Instance.LoadScene(SceneManagerCustom.SceneMain);
            }, false);
        }

        // this method is called after client is connect
        public override void OnClientAuthenticate()
        {
            NetworkClient.Send(new AuthRequestMessage { token = VariableManager.ClientToken });
        }
    }
}