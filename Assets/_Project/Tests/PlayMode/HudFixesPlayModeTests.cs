using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Bug MIS B06 (damage numbers made the HUD unreadable), B07 (numbers cut by models) and B14 ("Total Kills:" wrapping),
    /// 2026-09-30.
    /// </summary>
    public class HudFixesPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator HudLabels_StayOnOneLine_AndHaveABackingBehindTheirText()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var hud = Object.FindFirstObjectByType<GameHUDController>();
            var kills = GetPrivateField<TextMeshProUGUI>(hud, "_killText");
            kills.text = "Total Kills: 107893";
            yield return null;
            yield return null;
            kills.ForceMeshUpdate();
            Assert.That(kills.textInfo.lineCount, Is.EqualTo(1), "Six-digit kills stay on one line (B14).");

            var backings = Object.FindObjectsByType<HudBacking>(FindObjectsSortMode.None);
            Assert.That(backings.Length, Is.GreaterThanOrEqualTo(3), "Kills, gold and time have a backing (B06).");
            var killBacking = backings.FirstOrDefault(b => b.name == kills.name + "_Backing");
            Assert.IsNotNull(killBacking, "The kill label has its backing.");
            Assert.That(killBacking.transform.GetSiblingIndex(), Is.LessThan(kills.transform.GetSiblingIndex()), "Drawn behind the label.");
            Rect back = ScreenRect((RectTransform)killBacking.transform);
            var b = kills.textBounds;
            Vector3 min = kills.rectTransform.TransformPoint(b.min), max = kills.rectTransform.TransformPoint(b.max);
            Assert.That(back.xMin <= min.x + 1f && back.xMax >= max.x - 1f && back.yMin <= min.y + 1f && back.yMax >= max.y - 1f, Is.True,
                $"The backing {back} covers the text ({min} - {max}).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator DamageNumbers_SitInFrontOfTheModels_AtTheSameScreenSpot()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var cam = Camera.main;
            Assert.That(cam.orthographic, Is.True, "The game camera is orthographic.");
            Vector3 hit = PlayerController.Instance.transform.position + Vector3.forward * 3f + Vector3.up;
            DamagePopupManager.Instance.ShowDamage(30, hit);
            var popup = Object.FindObjectsByType<DamagePopup>(FindObjectsSortMode.None).First(p => p.gameObject.activeInHierarchy);
            Vector3 screenHit = cam.WorldToScreenPoint(hit), screenPopup = cam.WorldToScreenPoint(popup.transform.position);
            // Bug MIS v2 C12: a number now starts up to PopupSpread from its hit point (so hits on one spot do not stack).
            float spread = Vector2.Distance(screenHit, cam.WorldToScreenPoint(hit + Vector3.right * DamagePopupManager.PopupSpread));
            Assert.That(Vector2.Distance(screenHit, screenPopup), Is.LessThan(spread + 1f), "At its hit point on screen (within the spread).");
            Assert.That(Vector3.Dot(popup.transform.position - hit, -cam.transform.forward), Is.EqualTo(DamagePopup.TowardCamera).Within(0.05f),
                "Pulled toward the camera, in front of the models (B07).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static Rect ScreenRect(RectTransform t)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
