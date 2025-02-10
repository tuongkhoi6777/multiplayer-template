using System;
using System.Collections.Generic;

namespace Core
{
    public class EventManager
    {
        public static EventEmitter emitter = new();

        // Events that using in gameplay scene
        public static string PLAYER_JUMP = "PLAYER_JUMP";
        public static string PLAYER_FIRE = "PLAYER_FIRE";
        public static string PLAYER_TAKE_DAMAGE = "PLAYER_TAKE_DAMAGE";
        public static string PLAYER_DEATH = "PLAYER_DEATH";

        // Events that using in main scene
        public static string SELECT_ROOM = "SELECT_ROOM";

        // Events that using in lobby scene
        public static string ROOM_UPDATE = "ROOM_UPDATE";

        // Global events that using in multiple scenes
        public static string DISCONNECT = "DISCONNECT";

        // Events that using for communicate with nodejs (game server)
        public static string SERVER_READY = "SERVER_READY";
        public static string GAME_OVER = "GAME_OVER";
    }
    
    public class EventEmitter
    {
        // A dictionary to store events and their associated listeners
        private Dictionary<string, List<Action<object[]>>> eventListeners = new Dictionary<string, List<Action<object[]>>>();

        // Subscribe a listener to an event
        public void On(string eventName, Action<object[]> listener)
        {
            if (!eventListeners.ContainsKey(eventName))
            {
                eventListeners[eventName] = new List<Action<object[]>>();
            }
            eventListeners[eventName].Add(listener);
        }

        // Subscribe a listener without arguments to an event
        public void On(string eventName, Action listener)
        {
            void wrapper(object[] args)
            {
                listener();
            }

            On(eventName, wrapper);
        }

        // Subscribe a listener to an event, but it will only be called once
        public void Once(string eventName, Action<object[]> listener)
        {
            // Create a wrapper for the listener to automatically remove itself after the first call
            void onceListener(object[] args)
            {
                listener(args); // Call the original listener
                Off(eventName, onceListener); // Remove the listener after the first invocation
            }

            On(eventName, onceListener); // Add the wrapper listener
        }

        // Remove a listener from an event
        public void Off(string eventName, Action<object[]> listener)
        {
            if (eventListeners.ContainsKey(eventName))
            {
                eventListeners[eventName].Remove(listener);
                if (eventListeners[eventName].Count == 0)
                {
                    eventListeners.Remove(eventName);
                }
            }
        }

        // Remove all listeners for a specific event
        public void Off(string eventName)
        {
            if (eventListeners.ContainsKey(eventName))
            {
                eventListeners.Remove(eventName);
            }
        }

        // Emit an event and call all listeners with the provided arguments
        public void Emit(string eventName, params object[] args)
        {
            if (eventListeners.ContainsKey(eventName))
            {
                foreach (var listener in eventListeners[eventName])
                {
                    listener(args);
                }
            }
        }
    }
}