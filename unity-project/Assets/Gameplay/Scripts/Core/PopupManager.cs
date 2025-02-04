using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Popups;

namespace Core
{
    public enum Popup
    {
        Error,
        Message,
    }

    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }
        public Transform Active;
        public Transform Inactive;

        [Header("Popup Prefabs")]
        public List<GameObject> Popups = new(); // List of prefabs
        private Dictionary<Popup, Queue<GameObject>> PopupPools = new(); // Pool for each Popup type

        private void Awake()
        {
            // Ensure Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Init();
            }
            else
            {
                Destroy(gameObject); // Destroy duplicate
            }
        }

        private void Init()
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
            Transform popupTransform = Instance.Active.Find(name); // Assuming popups are directly under Active
            if (popupTransform != null)
            {
                HidePopup(popupTransform.gameObject);
            }
        }

        // Helper method to get Popup enum from name (this will be used when returning to pool)
        private Popup GetPopupEnumByName(string name)
        {
            foreach (Popup popupEnum in System.Enum.GetValues(typeof(Popup)))
            {
                if (Popups[(int)popupEnum].name == name)
                    return popupEnum;
            }
            return Popup.Error; // Default fallback, should be handled more gracefully
        }

        // Show an error popup with a custom message
        public static void ShowError(string message)
        {
            var popup = ShowPopup(Popup.Error);
            popup.GetComponent<PopupError>().Init(message);

            Debug.LogError(message);
        }
    }
}
