using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private CanvasGroup loadingCanvasGroup; // Reference to canvas group for fading

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Ease fadeEase = Ease.InOutQuad;

    [Header("Loading Text Animation")]
    [SerializeField] private float dotAnimationSpeed = 0.5f;
    [SerializeField] private int maxDots = 3;

    [Header("Spinner Animation")]
    [SerializeField] private float spinnerRotationSpeed = 1f;
    [SerializeField] private bool clockwise = true;

    // Singleton instance
    public static LoadingManager Instance { get; private set; }

    private Tween spinnerTween;
    private Coroutine dotAnimationCoroutine;

    // Make this GameObject persistent between scenes
    private void Awake()
    {
        // Singleton pattern with persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Make sure we have a canvas group
            if (loadingCanvasGroup == null)
                loadingCanvasGroup = loadingPanel.GetComponent<CanvasGroup>() ?? loadingPanel.AddComponent<CanvasGroup>();

            // Hide loading panel at start
            loadingPanel.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start the loading process
    public void StartLoading(string sceneToLoad, float minLoadTime, string sceneToUnload = "")
    {
        StartCoroutine(LoadSceneRoutine(sceneToLoad, minLoadTime, sceneToUnload));
    }

    private IEnumerator AnimateLoadingText()
    {
        if (loadingText == null) yield break;

        string baseText = "Loading";
        int dotCount = 0;

        while (true)
        {
            string dots = new string('.', dotCount);
            loadingText.text = baseText + dots;

            dotCount = (dotCount + 1) % (maxDots + 1);

            yield return new WaitForSeconds(dotAnimationSpeed);
        }
    }

    private void StartSpinnerAnimation()
    {
        if (spinnerImage == null) return;

        // Kill any existing rotation animation
        spinnerTween?.Kill();

        // Reset rotation
        spinnerImage.localRotation = Quaternion.identity;

        // Direction of rotation
        float rotationAmount = clockwise ? -360f : 360f;

        // Create infinite spinning animation with DOTween
        spinnerTween = spinnerImage.DOLocalRotate(
            new Vector3(0, 0, rotationAmount),
            1f / spinnerRotationSpeed,
            RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    private void StopAnimations()
    {
        // Stop dot animation
        if (dotAnimationCoroutine != null)
        {
            StopCoroutine(dotAnimationCoroutine);
            dotAnimationCoroutine = null;
        }

        // Stop spinner animation
        spinnerTween?.Kill();
    }

    private void CheckForEventSystem()
    {
        // Count EventSystems in the scene
        UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);

        if (eventSystems.Length > 1)
        {
            Debug.Log($"Found {eventSystems.Length} EventSystems. Removing duplicates.");

            // Keep the first one (usually the one that was DontDestroyOnLoad)
            for (int i = 1; i < eventSystems.Length; i++)
            {
                Destroy(eventSystems[i].gameObject);
            }
        }
    }

    private void CheckForAudioListener()
    {
        // Count AudioListeners in the scene
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        if (listeners.Length > 1)
        {
            Debug.Log($"Found {listeners.Length} AudioListeners. Removing duplicates.");

            // Find a potential music manager listener to keep
            AudioListener listenerToKeep = null;

            // First priority: try to find the listener in a music manager if you have one
            /* If you have a music manager, uncomment this
            if (MusicManager.Instance != null)
            {
                listenerToKeep = MusicManager.Instance.GetComponent<AudioListener>();
            }
            */

            // Second priority: if no music manager, keep the main camera's listener
            if (listenerToKeep == null)
            {
                foreach (AudioListener listener in listeners)
                {
                    if (listener.gameObject.CompareTag("MainCamera"))
                    {
                        listenerToKeep = listener;
                        break;
                    }
                }
            }

            // If still no listener found, just keep the first one
            if (listenerToKeep == null && listeners.Length > 0)
            {
                listenerToKeep = listeners[0];
            }

            // Remove all other listeners
            foreach (AudioListener listener in listeners)
            {
                if (listener != listenerToKeep)
                {
                    Destroy(listener);
                }
            }
        }
    }

    private IEnumerator LoadSceneRoutine(string sceneToLoad, float minLoadTime, string sceneToUnload)
    {
        // Show and prepare loading panel
        loadingPanel.SetActive(true);
        loadingCanvasGroup.alpha = 0;

        // Start animations
        dotAnimationCoroutine = StartCoroutine(AnimateLoadingText());
        StartSpinnerAnimation();

        // Fade in loading panel
        loadingCanvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeEase);

        // Wait for fade in
        yield return new WaitForSeconds(fadeInDuration);

        // Start actual loading
        progressSlider.value = 0;
        if (progressText != null)
            progressText.text = "0%";

        // Load the new scene additively
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
        loadOperation.allowSceneActivation = false;

        // Track loading progress
        float startTime = Time.time;
        float progress = 0;

        // Update the progress until either the scene is loaded or minimum time has passed
        while (!loadOperation.isDone && (Time.time - startTime) < minLoadTime)
        {
            // Determine progress - blend actual loading with time-based progress
            float timeProgress = Mathf.Clamp01((Time.time - startTime) / minLoadTime);
            float sceneProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);

            // Use the lower of the two to ensure smooth progression
            progress = Mathf.Lerp(progress, Mathf.Max(timeProgress, sceneProgress), Time.deltaTime * 3f);

            // Update UI
            progressSlider.value = progress;
            if (progressText != null)
                progressText.text = Mathf.Round(progress * 100) + "%";

            yield return null;
        }

        // Make sure the slider hits 100% at the end
        progressSlider.DOValue(1f, 0.5f).SetEase(Ease.OutQuad);
        if (progressText != null)
            progressText.text = "100%";

        // Allow the scene to activate
        loadOperation.allowSceneActivation = true;

        // Wait for the scene to fully load
        while (!loadOperation.isDone)
            yield return null;

        // Create a black overlay in the new scene that will fade out
        Scene newScene = SceneManager.GetSceneByName(sceneToLoad);
        SceneManager.SetActiveScene(newScene);

        CheckForEventSystem();
        CheckForAudioListener();

        // Find or create the SceneTransitionManager in the new scene
        SceneTransitionManager transitionManager = FindFirstObjectByType<SceneTransitionManager>();
        if (transitionManager == null)
        {
            // Create a new transition manager in the scene if one doesn't exist
            GameObject transitionObj = new GameObject("SceneTransitionManager");
            SceneManager.MoveGameObjectToScene(transitionObj, newScene);
            transitionManager = transitionObj.AddComponent<SceneTransitionManager>();
            transitionManager.SetupTransition();
        }

        // Fade out loading screen
        loadingCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(fadeEase).OnComplete(() =>
        {
            loadingPanel.SetActive(false);
            StopAnimations();

            // Trigger fade-in of the new scene
            transitionManager.StartFadeIn();

            // Unload old scene after loading screen is gone
            if (!string.IsNullOrEmpty(sceneToUnload))
            {
                SceneManager.UnloadSceneAsync(sceneToUnload);
            }
        });
    }

    private void OnDestroy()
    {
        // Clean up any ongoing animations
        StopAnimations();
    }
}