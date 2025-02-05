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
                btnJoinRoom.interactable = VariableManager.CurrentRoomId != "";
            });

            btnJoinRoom.onClick.AddListener(JoinRoom);
            btnCreateRoom.onClick.AddListener(CreateRoom);
            btnExitGame.onClick.AddListener(SystemManager.ExitGame);
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

            GetAllRooms();
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
                string roomName = $"{VariableManager.ClientToken}'s room";
                await WebSocketClient.SendMessageAsync("createRoom", new { roomName });
                SceneManagerCustom.Instance.LoadScene(SceneManagerCustom.SceneLobby);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error create room: " + ex.Message);
            }

            if (VariableManager.IsDebug)
            {
                await RoomUpdateSimulatorAsync();
            }
        }

        // find and join any custom room that available
        public async void JoinRoom()
        {
            string roomId = VariableManager.CurrentRoomId;
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

        public async Task RoomUpdateSimulatorAsync()
        {
            var list = new List<PlayerRoomInfo>();
            string id = VariableManager.ClientToken;
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