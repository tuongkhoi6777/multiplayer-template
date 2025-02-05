using System;
using System.Collections.Generic;
using Core;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class Lobby : MonoBehaviour
    {
        public Button btnStartGame;
        public Button btnChangeTeam;
        public Button btnExitRoom;
        public GameObject itemPrefab;
        public Transform team1;
        public Transform team2;
        static Lobby Instance = null;
        private Dictionary<string, PlayerInfoItem> items = new();
        void Awake()
        {
            Instance = this;

            HandleRoomUpdate();

            EventManager.emitter.On(EventManager.START_GAME, OnStartGame);

            btnStartGame.onClick.AddListener(StartGame);
            btnChangeTeam.onClick.AddListener(ChangeTeam);
            btnExitRoom.onClick.AddListener(ExitRoom);
        }

        void OnDestroy()
        {
            Instance = null;

            EventManager.emitter.Off(EventManager.START_GAME);
        }

        async void StartGame()
        {
            try
            {
                await WebSocketClient.SendMessageAsync("startGame", new { });
            }
            catch (Exception ex)
            {
                Debug.LogError("Error create room: " + ex.Message);
            }
        }

        async void ChangeTeam()
        {
            try
            {
                int currentTeam = Array.Find(VariableManager.roomData.players, player => player.id == VariableManager.ClientToken).team;
                await WebSocketClient.SendMessageAsync("changeTeam", new { teamIndex = 1 - currentTeam });
            }
            catch (Exception ex)
            {
                Debug.LogError("Error create room: " + ex.Message);
            }
        }

        async void ExitRoom()
        {
            try
            {
                await WebSocketClient.SendMessageAsync("exitRoom", new { });
                SceneManagerCustom.Instance.LoadScene(SceneManagerCustom.SceneMain);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error create room: " + ex.Message);
            }
        }

        public static void OnRoomUpdate()
        {
            Instance?.HandleRoomUpdate();
        }

        public void HandleRoomUpdate()
        {
            // Enable or disable the Start button based on whether the current client is the host
            btnStartGame.interactable = VariableManager.roomData.host == VariableManager.ClientToken;

            // Create a new dictionary to hold the updated player information
            var newDictionary = new Dictionary<string, PlayerInfoItem>();

            // Iterate through the updated list of players in the room
            foreach (var player in VariableManager.roomData.players)
            {
                // Try to get the player info item based on the player's ID
                items.TryGetValue(player.id, out PlayerInfoItem item);

                // Determine which team the player belongs to
                var team = player.team == 0 ? team1 : team2;

                // If the player doesn't already have an item (new player joining)
                if (item == null)
                {
                    var gameObject = Instantiate(itemPrefab);
                    gameObject.transform.SetParent(team);  // Set the team as parent for the new item

                    item = gameObject.GetComponent<PlayerInfoItem>();
                    item.Init(player.name, player.team);  // Initialize the player info
                }
                else if (item.team != player.team) // If the player changes team
                {
                    item.transform.SetParent(team);  // Move to the new team's parent
                    item.OnTeamChange(player.team);  // Update the team information
                }

                // Add or update the player item in the new dictionary
                newDictionary[player.id] = item;
            }

            // Remove items that are no longer present in the updated room state
            foreach (var item in items)
            {
                // Only destroy the items that were not part of the new player list
                if (!newDictionary.ContainsKey(item.Key))
                {
                    Destroy(item.Value.gameObject);
                }
            }

            // Update the main items dictionary with the new one
            items = newDictionary;

            // Set the host player item
            if (items.TryGetValue(VariableManager.roomData.host, out PlayerInfoItem hostItem))
            {
                hostItem.SetHost();
            }
        }

        void OnStartGame(object[] args)
        {
            var jobject = JObject.FromObject(args[0]);
            VariableManager.ServerAddress = jobject["serverIp"].Value<string>();
            VariableManager.ServerPort = jobject["serverPort"].Value<ushort>();

            SceneManagerCustom.Instance.LoadScene(SceneManagerCustom.SceneGamePlay);
        }
    }
}