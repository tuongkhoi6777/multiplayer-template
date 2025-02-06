using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Popups;
using System.Linq;

namespace Core
{
    public enum Popup
    {
        Error,
        Message,
    }

    public class PopupManager : Singleton<PopupManager>
    {
        public Transform Active;
        public Transform Inactive;

        [Header("Popup Prefabs")]
        public List<GameObject> Popups = new(); // List of prefabs
        private Dictionary<Popup, Queue<GameObject>> PopupPools = new(); // Pool for each Popup type

        protected override void Init()
        {
            // Initialize pools with corresponding popup types
            foreach (Popup popupEnum in System.Enum.GetValues(typeof(Popup)))
            {
                PopupPools[popupEnum] = new Queue<GameObject>();
            }
        }

        // Get a popup from pool or instantiate a new one
        public GameObject GetPopup(Popup popupEnum)
        {
            if (PopupPools[popupEnum].Count > 0)
            {
                return PopupPools[popupEnum].Dequeue(); // Return pooled popup
            }

            // Instantiate a new popup if pool is empty
            GameObject popup = Instantiate(Popups[(int)popupEnum]);
            popup.name = Popups[(int)popupEnum].name; // Rename to avoid "(Clone)"
            return popup;
        }

        // Show popup by enum
        public static GameObject ShowPopup(Popup popupEnum)
        {
            var popup = Instance.GetPopup(popupEnum);

            // Set parent to Active and show the popup
            popup.transform.SetParent(Instance.Active);
            popup.SetActive(true);

            // Animate scaling and fading
            popup.transform.localScale = Vector3.zero;
            popup.transform.DOScale(1f, 0.3f);
            popup.GetComponent<CanvasGroup>().DOFade(1f, 0.3f);

            return popup;
        }

        // Hide popup and return to pool
        public static void HidePopup(GameObject popup)
        {
            popup.GetComponent<CanvasGroup>().DOFade(0f, 0.3f).OnComplete(() =>
            {
                popup.SetActive(false);
                popup.transform.SetParent(Instance.Inactive); // Move to Inactive

                // Find the correct pool and enqueue the popup
                Popup popupEnum = Instance.GetPopupEnumByName(popup.name);
                Instance.PopupPools[popupEnum].Enqueue(popup);
            });
        }

        // Hide popup by enum
        public static void HidePopup(Popup popupEnum)
        {
            // Find the popup by enum and hide it
            string name = Instance.Popups[(int)popupEnum].name;

            // Assuming popups are directly under Active
            // Find first popup which has same name, reverse to find from bottom
            Transform popupTransform = Instance.Active.Cast<Transform>().Reverse().FirstOrDefault(t => t.name == name);
            if (popupTransform != null)
            {
                HidePopup(popupTransform.gameObject);
            }
        }

        // Helper method to get Popup enum from name (this will be used when returning to pool)
        private Popup GetPopupEnumByName(string name)
        {
            return (Popup)Popups.FindIndex(e => e.name == name);
        }

        // Show an error popup with a custom message
        public static void ShowError(string message)
        {
            var popup = ShowPopup(Popup.Error);
            popup.GetComponent<PopupError>().Init(message);

            Debug.LogError(message);
        }

        // Show a notify popup with a custom message
        public static void ShowMessage(string message)
        {
            var popup = ShowPopup(Popup.Message);
            popup.GetComponent<PopupMessage>().Init(message);

            Debug.Log(message);
        }
    }
}
