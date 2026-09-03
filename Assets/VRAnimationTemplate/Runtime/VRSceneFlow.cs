using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRAnimationTemplate
{
    /// <summary>Persistent controller for the complete menu-to-credits scene sequence.</summary>
    public sealed class VRSceneFlow : MonoBehaviour
    {
        public static VRSceneFlow Instance { get; private set; }

        [SerializeField] private string[] sceneNames =
        {
            "StartMenu", "Scene1", "Scene2", "Scene3", "EndCredits"
        };

        [SerializeField, Min(0f)] private float loadDelaySeconds = 0.15f;
        private bool isLoading;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            // Convenient editor/desktop preview shortcuts.
            if (Input.GetKeyDown(KeyCode.N)) NextScene();
            if (Input.GetKeyDown(KeyCode.M)) ReturnToMenu();
            if (Input.GetKeyDown(KeyCode.R)) RestartScene();
#endif
        }

        public void StartExperience() => LoadByIndex(1);
        public void ReturnToMenu() => LoadByIndex(0);
        public void ReplayExperience() => LoadByIndex(1);

        public void RestartScene()
        {
            if (!isLoading)
                StartCoroutine(LoadRoutine(SceneManager.GetActiveScene().name));
        }

        public void NextScene()
        {
            int current = IndexOf(SceneManager.GetActiveScene().name);
            LoadByIndex(current < 0 ? 0 : Mathf.Min(current + 1, sceneNames.Length - 1));
        }

        public void LoadByIndex(int index)
        {
            if (isLoading || sceneNames == null || sceneNames.Length == 0)
                return;

            index = Mathf.Clamp(index, 0, sceneNames.Length - 1);
            StartCoroutine(LoadRoutine(sceneNames[index]));
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            isLoading = true;
            if (loadDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(loadDelaySeconds);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            while (operation != null && !operation.isDone)
                yield return null;

            isLoading = false;
        }

        private int IndexOf(string sceneName)
        {
            for (int i = 0; i < sceneNames.Length; i++)
                if (sceneNames[i] == sceneName)
                    return i;
            return -1;
        }
    }
}
