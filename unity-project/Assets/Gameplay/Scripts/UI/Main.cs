using System;
using Core;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

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
            // add select room event listener
            EventManager.emitter.On(EventManager.SELECT_ROOM, OnRoomSelect);

            // add button onclick listener
            btnJoinRoom.onClick.AddListener(JoinRoom);
            btnCreateRoom.onClick.AddListener(CreateRoom);
            btnExitGame.onClick.AddListener(SystemManager.ExitGame);
            btnRefreshList.onClick.AddListener(GetAllRooms);
        }

        void OnDestroy()
        {
            // remove select room event listener
            EventManager.emitter.Off(EventManager.SELECT_ROOM);
        }

        async void Start()
        {
            await SystemManager.ConnectServer();

            GetAllRooms();
        }

        public void OnRoomSelect()
        {
            btnJoinRoom.interactable = VariableManager.CurrentRoomId != "";
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
                PopupManager.ShowMessage("Error get rooms:\n" + ex.Message);
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
                PopupManager.ShowMessage("Error create room:\n" + ex.Message);
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
                PopupManager.ShowMessage("Error join room:\n" + ex.Message);
            }
        }
    }
}