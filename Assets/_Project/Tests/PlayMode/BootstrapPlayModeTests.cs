using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using SoulHunter.Core.Services;
using SoulHunter.Core.Scenes;

namespace SoulHunter.Tests.PlayMode
{
    public class BootstrapPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_InitializesGameServices_Successfully()
        {
            // Load the Bootstrap scene which is the entry point of the game
            AsyncOperation loadOp = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(loadOp, "Failed to initiate load for Bootstrap scene. Is it in Build Settings?");

            yield return new WaitUntil(() => loadOp.isDone);

            // Wait a few frames to ensure Awake/Start sequence completes
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.IsNotNull(GameServices.Instance, "GameServices.Instance should not be null after Bootstrap loads.");

            // Verify a known core service was registered
            var sceneService = GameServices.Instance.Get<SceneService>();
            Assert.IsNotNull(sceneService, "SceneService should be registered in GameServices.");
        }
    }
}
