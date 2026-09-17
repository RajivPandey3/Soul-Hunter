using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Tests.PlayMode
{
    public class WanderingMerchantPlayModeTests
    {
        [UnityTest]
        public IEnumerator ForgottenVillage_SpawnsWanderingMerchant()
        {
            var playerGo = new GameObject("PlayerController");
            playerGo.AddComponent<PlayerController>();
            playerGo.AddComponent<HealthController>();

            var go = new GameObject("CampaignSignatureSystem");
            var sys = go.AddComponent<CampaignSignatureSystem>();
            
            var pmGo = new GameObject("LevelProgressionManager");
            var pm = pmGo.AddComponent<LevelProgressionManager>();
            
            yield return null;
            
            pm.StartStage(3); // Level 3 is Forgotten Village
            yield return null;
            
            var merchant = GameObject.Find("WanderingMerchant");
            Assert.IsNotNull(merchant, "Wandering Merchant should be spawned in Level 3 (Forgotten Village).");
            var comp = merchant.GetComponent<WanderingMerchant>();
            Assert.IsNotNull(comp, "Merchant should have WanderingMerchant component.");
            
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(pmGo);
            if (merchant != null) Object.DestroyImmediate(merchant);
        }
    }
}
