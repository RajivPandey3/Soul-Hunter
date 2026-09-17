using UnityEngine;
using System.Collections;

namespace SoulHunter.UI
{
    /// <summary>
    /// Learning Comment:
    /// Ye script Loading screen par picture dikhane ke baad
    /// thodi der mein automatically Main Menu load kar dega.
    /// </summary>
    public class LoadingScreenTimer : MonoBehaviour
    {
        [Tooltip("Kitne second tak picture dikhani hai")]
        public float delayTime = 3f;

        private void Start()
        {
            // Start the loading coroutine
            StartCoroutine(LoadNextSceneAfterDelay());
        }

        private IEnumerator LoadNextSceneAfterDelay()
        {
            // 3 second wait karo (taaki player picture dekh sake)
            yield return new WaitForSecondsRealtime(delayTime);

            // Uske baad Main Menu load kar do
            Debug.Log("[LoadingScreen] Loading MainMenu...");
            // Loading is a boundary scene: it must not wait on the previous
            // Bootstrap load operation or it can remain on the loading frame.
            // MainMenu is already in Build Settings, so this direct transition
            // is deterministic for both normal startup and Editor Play Mode.
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu");
            else
                Debug.LogError("[LoadingScreen] MainMenu is missing from Build Settings.");
        }
    }
}
