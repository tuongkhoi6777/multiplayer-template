using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Core
{
    // A class that manages global game functions, such as cursor visibility and game exit.
    public class SystemManager
    {
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
                        EventManager.emitter.Emit(parts[1]);
                    }
                }
            }
        }
    }
}