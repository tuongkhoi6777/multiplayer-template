using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RoomItem : MonoBehaviour
    {
        public TextMeshProUGUI roomIndex;
        public TextMeshProUGUI roomName;
        public TextMeshProUGUI totalPlayer;
        public TextMeshProUGUI roomStatus;
        private string roomId = "";

        public void Init(int index, RoomsDetails details)
        {
            transform.localScale = new Vector3(1, 1, 1);
            
            var toggle = GetComponent<Toggle>();
            toggle.isOn = false;
            toggle.group = GetComponentInParent<ToggleGroup>();

            roomId = details.roomId;
            Debug.Log(roomId);
            roomIndex.text = $"{index}";
            roomName.text = details.roomName;
            totalPlayer.text = $"{details.numOfPlayers}/10";

            if (details.isStarted)
            {
                toggle.interactable = false;
                roomStatus.text = "Started";
                roomStatus.color = Color.red;
                totalPlayer.color = Color.red;
            }
            else
            {
                toggle.interactable = true;
                roomStatus.text = "Waiting";
                roomStatus.color = Color.cyan;
                totalPlayer.color = Color.cyan;
            }
        }

        public void OnToggle(bool isOn)
        {
            StoredManager.CurrentRoomId = isOn ? roomId : "";
            Debug.Log(StoredManager.CurrentRoomId);
            EventManager.emitter.Emit(EventManager.SELECT_ROOM);
        }
    }
}