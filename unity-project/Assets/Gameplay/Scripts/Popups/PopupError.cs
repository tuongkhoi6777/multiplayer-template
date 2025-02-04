using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

namespace Popups
{
    public class PopupError : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public Button btnOK;
        public void Init(string message, UnityAction onClick = null)
        {
            text.text = message;
            onClick ??= Application.Quit;
            btnOK.onClick.AddListener(onClick);
        }
    }
}