using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerInfoItem : MonoBehaviour
    {
        public TextMeshProUGUI playerName;
        public GameObject bg_1;
        public GameObject bg_2;
        public GameObject host;
        public int team { get; set;}

        public void Init(string name, int t)
        {
            transform.localScale = new Vector3(1, 1, 1);
            
            playerName.text = name;
            host.SetActive(false);

            OnTeamChange(t);
        }

        public void OnTeamChange(int newTeam)
        {
            team = newTeam;
            bg_1.SetActive(team == 0);
            bg_2.SetActive(team == 1);
        }

        public void SetHost()
        {
            host.SetActive(true);
        }
    }
}