using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Core
{
    // A class that manages global game functions, such as cursor visibility and game exit.
    public class SystemManager
    {
        public static async Task ConnectServer()
        {
            if (WebSocketClient.IsConnected()) return;

#if !UNITY_EDITOR
            // TESTING: set server address via command line arguments
            string[] args = VariableManager.CommandLineArgs;
            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                Debug.Log(args[1]);
                VariableManager.WebSocketServer = args[1];
            }
#endif

            // TODO: get server address from steam cloud and steam token from steam SDK
            string token = SystemInfo.deviceUniqueIdentifier;
            await WebSocketClient.StartAsync(VariableManager.WebSocketServer, token);
        }
        public static void ShowCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        public static void HideCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void StartGameplay(object args)
        {
            var jobject = JObject.FromObject(args);
            VariableManager.ServerAddress = jobject["serverIp"].Value<string>();
            VariableManager.ServerPort = jobject["serverPort"].Value<ushort>();

            SceneManagerCustom.Instance.LoadScene(SceneManagerCustom.SceneGamePlay);
        }

        public static void ExitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false; // Stop play mode in the editor
#endif
        }

        public static void SendToNode(string message)
        {
            // Send message to Nodejs using stdout (write log)
            Console.WriteLine($"From Unity: {message}");
        }

        public static async void OnReceiveFromNode()
        {
            while (true)
            {
                // Wait for a message from Nodejs using stdout (read log)
                string input = await Task.Run(() => Console.ReadLine());
                if (!string.IsNullOrEmpty(input))
                {
                    // Message from node is start with "From Node: "
                    var parts = input.Split(new string[] { "From Node: " }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        // TODO: handle message from Nodejs
                        EventManager.emitter.Emit(parts[1]);
                    }
                }
            }
        }
    }
}