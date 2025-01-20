using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;  // Import DOTween namespace
using System.Collections;
using TMPro;

namespace Core
{
    public class SceneManagerCustom : MonoBehaviour
    {
        // Singleton instance for easier access from other scripts
        public static SceneManagerCustom Instance { get; private set; }

        [Header("Fade Animation Settings")]
        public GameObject canvas;
        public CanvasGroup blackCover;  // Reference to CanvasGroup for fade effect
        public float fadeDuration = 1.0f;    // Duration of fade in/out effect

        [Header("Loading Screen UI (Optional)")]
        public TextMeshProUGUI loadingText;             // Loading text (optional)

        // Defines scenes name here
        public static string SceneMain = "main";
        public static string SceneLobby = "lobby";
        public static string SceneGamePlay = "gameplay";

        private void Awake()
        {
            // Ensure the Singleton pattern is respected
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);  // Keep this instance across scenes

                Init();
            }
            else
            {
                Destroy(gameObject);  // Destroy the duplicate instance
            }
        }

        private void Init()
        {
            canvas.SetActive(false);
            blackCover.alpha = 0f;
        }

        // Load a scene by its name, with fade effects and loading screen
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneWithFade(sceneName));
        }

        // Coroutine to handle scene loading with fade-in and fade-out effects
        private IEnumerator LoadSceneWithFade(string sceneName)
        {
            // Fade out before loading the new scene
            yield return StartCoroutine(FadeIn());

            // Optionally, show the loading screen during the scene load
            if (loadingText != null)
            {
                loadingText.text = "Loading 0%";
            }

            // Start loading the scene asynchronously
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;  // Prevent the scene from activating immediately

            // Update progress bar as the scene loads
            while (!asyncLoad.isDone)
            {
                if (loadingText != null)
                {
                    loadingText.text = $"Loading {asyncLoad.progress * 100}%";
                }

                // If the scene has loaded to 90%, we can proceed to activate it
                if (asyncLoad.progress >= 0.9f)
                {
                    // Allow the scene to activate after it reaches 90%
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            // Fade in the scene after it's loaded
            yield return StartCoroutine(FadeOut());
        }

        // Fade-in effect using DOTween (CanvasGroup alpha goes from 0 to 1)
        private IEnumerator FadeIn()
        {
            canvas.SetActive(true);

            // Fade to 1 (fully opaque)
            blackCover.DOFade(1.0f, fadeDuration);

            // Wait for the duration of the fade
            yield return new WaitForSeconds(fadeDuration);
        }

        // Fade-out effect using DOTween (CanvasGroup alpha goes from 1 to 0)
        private IEnumerator FadeOut()
        {
            // Fade to 0 (fully transparent)
            blackCover.DOFade(0f, fadeDuration);

            // Wait for the duration of the fade
            yield return new WaitForSeconds(fadeDuration);

            canvas.SetActive(false);
        }
    }
}