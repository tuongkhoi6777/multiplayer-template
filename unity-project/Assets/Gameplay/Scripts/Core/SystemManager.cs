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
    }
}