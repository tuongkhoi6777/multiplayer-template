using System;
using Core;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI
{
    public class Main : MonoBehaviour
    {
        public Button btnCreateRoom;
        public Button btnJoinRoom;
        public Button btnExitGame;
        public Button btnRefreshList;
        public GameObject roomPrefab;
        public Transform roomContainer;
        void Awake()
        {
            EventManager.emitter.On(EventManager.SELECT_ROOM, () =>
            {
                btnJoinRoom.interactable = StoredManager.CurrentRoomId != "";
            });

            btnJoinRoom.onClick.AddListener(JoinRoom);
            btnCreateRoom.onClick.AddListener(CreateRoom);
            btnExitGame.onClick.AddListener(ExitGame);
            btnRefreshList.onClick.AddListener(GetAllRooms);
        }

        void OnDestroy()
        {
            EventManager.emitter.Off(EventManager.SELECT_ROOM);
        }

        async void Start()
        {
#if !UNITY_EDITOR
            // TESTING: set server address via command line arguments
            string[] args = StoredManager.CommandLineArgs;
            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                Debug.Log(args[1]);
                StoredManager.WebSocketServer = args[1];
            }
#endif

            // TODO: get server address from steam cloud and steam token from steam SDK
            string token = Guid.NewGuid().ToString();
            await WebSocketClient.StartAsync(StoredManager.WebSocketServer, token);

            GetAllRooms();
        }

        void GetServerAddress()
        {
            StoredManager.WebSocketServer = StoredManager.CommandLineArgs[1];
        }

        public async void GetAllRooms()
        {
            foreach (Transform child in roomContainer)
            {
                Destroy(child.gameObject);
            }

            try
            {
                object result = await WebSocketClient.SendMessageAsync("getAllRooms", new { });
                RoomsDetails[] list = JObject.FromObject(result)["list"].ToObject<RoomsDetails[]>();

                int index = 0;
                foreach (var room in list)
                {
                    var gameObject = Instantiate(roomPrefab);
                    gameObject.transform.SetParent(roomContainer);
                    gameObject.GetComponent<RoomItem>().Init(++index, room);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error get rooms: " + ex.Message);
            }
        }

        // create new custom room -> become host player then start game manual
        public async void CreateRoom()
        {
            try
            {
                string roomName = $"{StoredManager.ClientToken}'s room";
                await WebSocketClient.SendMessageAsync("createRoom", new { roomName });
                SceneManagerCustom.Instance.LoadScene(SceneManagerCustom.SceneLobby);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error create room: " + ex.Message);
            }

            if (StoredManager.IsDebug)
            {
                await RoomUpdateSimulatorAsync();
            }
        }

        // find and join any custom room that available
        public async void JoinRoom()
        {
            string roomId = StoredManager.CurrentRoomId;
            if (string.IsNullOrEmpty(roomId)) return;

            try
            {
                await WebSocketClient.SendMessageAsync("joinRoom", new { roomId });
                SceneManagerCustom.Instance.LoadScene(SceneManagerCustom.SceneLobby);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error join room: " + ex.Message);
            }
        }

        public void ExitGame()
        {
            Application.Quit();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false; // Stop play mode in the editor
#endif
        }

        public async Task RoomUpdateSimulatorAsync()
        {
            var list = new List<PlayerRoomInfo>();
            string id = StoredManager.ClientToken;
            string roomName = "";
            string host = id;

            // Add player
            list.Add(new PlayerRoomInfo(id, id, 0));
            EventManager.emitter.Emit(EventManager.ROOM_UPDATE, new { roomName, host, players = list.ToArray() });
            await Task.Delay(2000);  // Simulate WaitForSeconds(1)

            // Add player
            id = Guid.NewGuid().ToString();
            list.Add(new PlayerRoomInfo(id, id, 1));
            EventManager.emitter.Emit(EventManager.ROOM_UPDATE, new { roomName, host, players = list.ToArray() });
            await Task.Delay(2000);  // Simulate WaitForSeconds(1)

            // Change player team
            list[1].team = 0;
            EventManager.emitter.Emit(EventManager.ROOM_UPDATE, new { roomName, host, players = list.ToArray() });
            await Task.Delay(2000);  // Simulate WaitForSeconds(1)

            // Add player
            id = Guid.NewGuid().ToString();
            list.Add(new PlayerRoomInfo(id, id, 1));
            EventManager.emitter.Emit(EventManager.ROOM_UPDATE, new { roomName, host, players = list.ToArray() });
            await Task.Delay(2000);  // Simulate WaitForSeconds(1)

            // Remove player
            list.RemoveAt(2);
            EventManager.emitter.Emit(EventManager.ROOM_UPDATE, new { roomName, host, players = list.ToArray() });
        }
    }
}