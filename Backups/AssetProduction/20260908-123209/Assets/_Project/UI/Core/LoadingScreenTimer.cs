using UnityEngine;
using SoulHunter.Core.Services;
using SoulHunter.Core.Scenes;
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
            var scenes = GameServices.Instance?.Get<SceneService>();
            if (scenes != null) scenes.LoadSceneAsync("MainMenu");
            else Debug.LogError("[LoadingScreen] Start from the Bootstrap scene.");
        }
    }
}
