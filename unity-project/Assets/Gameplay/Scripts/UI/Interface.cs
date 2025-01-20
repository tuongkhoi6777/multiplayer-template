using Core;
using Newtonsoft.Json.Linq;

namespace UI
{
    public class RoomsDetails
    {
        public string roomId { get; set; }
        public string roomName { get; set; }
        public int numOfPlayers { get; set; }
        public bool isStarted { get; set; }
    }

    public class RoomData
    {
        public string roomName { get; set; }
        public string host { get; set; }
        public PlayerRoomInfo[] players { get; set; }

        public RoomData()
        {
            EventManager.emitter.On(EventManager.ROOM_UPDATE, OnUpdate);
        }

        private void OnUpdate(object[] args)
        {
            var jobject = JObject.FromObject(args[0]);
            roomName = jobject["roomName"].Value<string>();
            host = jobject["host"].Value<string>();
            players = jobject["players"].Value<JArray>().ToObject<PlayerRoomInfo[]>();

            Lobby.OnRoomUpdate();
        }
    }

    public class PlayerRoomInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public int team { get; set; }

        public PlayerRoomInfo(string Id, string Name, int Team)
        {
            id = Id;
            name = Name;
            team = Team;
        }
    }
}