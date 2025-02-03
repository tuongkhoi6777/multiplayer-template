using System;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public class WebSocketClient
    {
        private static ClientWebSocket ws;
        private static Dictionary<string, TaskCompletionSource<object>> responseHandlers = new();

        public static async Task StartAsync(string server, string token)
        {
            // TODO: get token from steam
            StoredManager.ClientToken = token;

            if (StoredManager.IsDebug) return;

            ws = new ClientWebSocket();

            Uri uri = new($"{server}?token={token}");

            await ws.ConnectAsync(uri, CancellationToken.None);

            // Start receiving messages asynchronously
            await ReceiveMessagesAsync();
        }

        private static async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024];

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleMessage(message);
                }
            }
        }

        private static void HandleMessage(string msg)
        {
            JObject jobject = JObject.Parse(msg);

            string type = jobject["type"].Value<string>();
            object data = jobject["data"].Value<object>();
            bool success = jobject["success"].Value<bool>();
            string message = jobject["message"].Value<string>();

            Debug.Log(msg);

            // Check if a message is a response from request or a notification by type
            // If type is a unique key then it is a response, if normal string is a notification
            if (responseHandlers.TryGetValue(type, out TaskCompletionSource<object> tcs))
            {
                responseHandlers.Remove(type);

                if (success)
                {
                    tcs.SetResult(data); // Set the result on the promise
                }
                else
                {
                    tcs.SetException(new Exception(message)); // Set the error on the promise
                }
            }
            else
            {
                // Handle notifications (non-response messages)
                switch (type)
                {
                    case "connection":
                        // TODO: handle connection fail
                        // if not success show popup message, exit game if press button OK
                        // there will be 2 case when connection fail: token invalid or new session connect
                        break;
                    case "rejoinGame":
                        // TODO: switch to gameplay scene from main
                        break;
                    case "startGame":
                        // TODO: switch to gameplay scene from lobby
                        EventManager.emitter.Emit(EventManager.START_GAME, data);
                        break;
                    case "playerKicked":
                        // TODO: switch to main scene and show popup user has been kicked by host
                        break;
                    case "roomUpdate":
                        // TODO: update room status (players join or out)
                        EventManager.emitter.Emit(EventManager.ROOM_UPDATE, data);
                        break;
                }
            }
        }

        public static Task<object> SendMessageAsync(string type, object payload)
        {
            if (StoredManager.IsDebug) return Task.FromResult(MockApis(type));

            string key = Guid.NewGuid().ToString();
            var message = new { type, payload, key };
            ArraySegment<byte> msg = new(Encoding.UTF8.GetBytes(JObject.FromObject(message).ToString()));

            // Create a TaskCompletionSource to represent the promise
            var tcs = new TaskCompletionSource<object>();
            responseHandlers.Add(key, tcs);

            // Send the message asynchronously
            ws.SendAsync(msg, WebSocketMessageType.Text, true, CancellationToken.None);

            return tcs.Task; // Return the Task that represents the async operation
        }

        public static object MockApis(string type)
        {
            switch (type)
            {
                case "getAllRooms":
                    {
                        object[] data = new object[3];
                        data[0] = new { roomId = "0", roomName = "asdadasd", numOfPlayers = 1, isStarted = false };
                        data[1] = new { roomId = "1", roomName = "3213123123", numOfPlayers = 5, isStarted = false };
                        data[2] = new { roomId = "2", roomName = "hgfhfghfgh", numOfPlayers = 8, isStarted = true };
                        return new { list = data };
                    }
                default:
                    return null;
            }
        }
    }
}
