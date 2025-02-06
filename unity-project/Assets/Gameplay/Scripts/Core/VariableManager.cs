using System;
using UI;

namespace Core
{
    // A class that stored all goblal variables that can access from every where
    public class VariableManager
    {
        public static string WebSocketServer = "ws://192.168.22.9:8080";
        public static string ServerAddress = "127.0.0.1";
        public static ushort ServerPort = 7777;
        public static string ClientToken = "";
        public static string CurrentRoomId = "";
        public static RoomData roomData = new();
        public static readonly bool IsDebug = false;
        public static string[] CommandLineArgs = Environment.GetCommandLineArgs();
    }
}