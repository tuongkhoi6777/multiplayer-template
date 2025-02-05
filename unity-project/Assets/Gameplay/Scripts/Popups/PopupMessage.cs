using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using Core;

namespace Popups
{
    public class PopupMessage : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public Button btnOK;

        public void Init(string message, UnityAction onClick = null)
        {
            text.text = message;
            onClick ??= () => PopupManager.HidePopup(gameObject);
            btnOK.onClick.RemoveAllListeners();
            btnOK.onClick.AddListener(onClick);
        }
    }
}