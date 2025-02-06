using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using Core;

namespace Popups
{
    public class PopupError : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public Button btnOK;

        public void Init(string message, UnityAction onClick)
        {
            text.text = message;
            btnOK.onClick.RemoveAllListeners();
            btnOK.onClick.AddListener(onClick);
        }
    }
}