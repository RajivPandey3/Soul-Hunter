using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.CameraSystem;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// How the player looks in play (owner request, 2026-09-25): the tilted camera keeps the player centred,
    /// the hero model turns smoothly toward where it walks (instead of being mirrored left/right), and when a
    /// weapon fires the hero faces the enemy and plays the attack pose on its upper body while the legs run.
    /// </summary>
    public class PlayerPresentationPlayModeTests : GameplayPlayModeTestBase
    {
        private static readonly System.Reflection.FieldInfo MoveInput =
            typeof(PlayerController).GetField("_eventBusInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        private static IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        private IEnumerator BootAsKael()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load);
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            GameServices.Instance.Get<SaveService>().CurrentData.SelectedCharacterName = "Kael"; // in memory only
            GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
            yield return WaitForFreshGameplay(previousPlayer: null);
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) Object.Destroy(stray.gameObject);
            yield return null;
        }

        /// <summary>The flat direction the hero model's front points to, from its legs (as PlayerController does).</summary>
        private static Vector3 ModelFront(PlayerController player)
        {
            var bones = player.GetComponentsInChildren<Transform>().Where(t => t.gameObject.activeInHierarchy).ToList();
            var left = bones.First(b => b.name == "Thigh.L");
            var right = bones.First(b => b.name == "Thigh.R");
            var front = Vector3.Cross(right.position - left.position, Vector3.up);
            front.y = 0f;
            return front.normalized;
        }

        [UnityTest]
        public IEnumerator TiltedCamera_KeepsThePlayerCentred()
        {
            yield return BootAsKael();
            var camera = Camera.main;
            var follow = camera.GetComponent<CameraFollow>();
            Assert.IsNotNull(follow, "Main camera has no CameraFollow. " + Errors());
            yield return WaitSeconds(0.5f);
            Assert.That(camera.transform.eulerAngles.x, Is.EqualTo(CameraFollow.DefaultPitch).Within(0.5f), "Camera pitch.");
            var player = PlayerController.Instance;
            MoveInput.SetValue(player, new Vector2(0f, 1f));
            yield return WaitSeconds(1.5f);
            MoveInput.SetValue(player, Vector2.zero);
            var viewport = camera.WorldToViewportPoint(player.transform.position);
            Assert.That(viewport.x, Is.EqualTo(0.5f).Within(0.05f), "Player stays centred horizontally while walking. " + Errors());
            Assert.That(viewport.y, Is.EqualTo(0.5f).Within(0.08f), "Player stays centred vertically while walking. " + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator HealthBar_IsSmall_AndFollowsUnderTheHero()
        {
            // VS: a small HP bar under the hero (it was a red strip across the whole bottom of the screen).
            yield return BootAsKael();
            var bar = Object.FindFirstObjectByType<SoulHunter.Gameplay.UI.HealthBarFollow>();
            Assert.IsNotNull(bar, "The HUD health bar does not follow the hero. " + Errors());
            var player = PlayerController.Instance;
            MoveInput.SetValue(player, new Vector2(1f, 0.5f));
            yield return WaitSeconds(1.2f);
            MoveInput.SetValue(player, Vector2.zero);
            yield return null;

            var rect = (RectTransform)bar.transform;
            var canvas = bar.GetComponentInParent<Canvas>().rootCanvas;
            float scale = canvas.scaleFactor;
            Assert.That(rect.rect.width * scale, Is.LessThan(Screen.width * 0.15f), "The bar is small.");
            var cam = Camera.main;
            Vector3 feet = cam.WorldToScreenPoint(bar.FeetPosition());
            Vector2 barTop = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.position);
            Assert.That(Mathf.Abs(barTop.x - feet.x), Is.LessThan(8f), "Centred under the hero.");
            Assert.That(feet.y - barTop.y, Is.InRange(0f, 40f * scale), "Just below the hero's feet.");
            Assert.That(bar.GetComponent<UnityEngine.UI.Slider>().value, Is.EqualTo(1f).Within(0.001f), "Full health shows a full bar.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator StageLight_CastsSoftenedShadows()
        {
            yield return BootAsKael();
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(l => l.type == LightType.Directional && l.isActiveAndEnabled);
            Assert.That(sun.shadows, Is.Not.EqualTo(LightShadows.None), "The stage light must cast shadows.");
            Assert.That(sun.shadowStrength, Is.InRange(0.4f, 0.8f), "Shadows are softened, not pitch black.");
            var hero = PlayerController.Instance.GetComponentsInChildren<SkinnedMeshRenderer>().First();
            Assert.That(hero.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.On), "The hero casts a shadow.");
            Assert.That(hero.sharedMaterial.FindPass("ShadowCaster"), Is.GreaterThanOrEqualTo(0), "The hero's shader has a shadow pass.");

            // Performance on the low-end reference laptop: the horde casts no shadows and does not animate off-screen.
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var enemyPrefabs = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs");
            var enemy = Object.Instantiate(enemyPrefabs[0], PlayerController.Instance.transform.position + new Vector3(0f, 0f, 8f), Quaternion.identity);
            yield return null;
            Assert.That(enemy.GetComponentsInChildren<Renderer>().All(r => r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off),
                "Horde enemies must not cast shadows.");
            Assert.That(enemy.GetComponentsInChildren<Animator>().All(a => a.cullingMode == AnimatorCullingMode.CullCompletely),
                "Off-screen enemies must not animate.");
            Object.Destroy(enemy);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Hero_TurnsTowardItsWalk_AndFacesTheEnemyWhenItAttacks()
        {
            yield return BootAsKael();
            var player = PlayerController.Instance;
            Assert.That(player.transform.localScale.x, Is.GreaterThan(0f));

            // Walk right (screen right is world +X with the camera's yaw 0): the model turns, it is not mirrored.
            MoveInput.SetValue(player, new Vector2(1f, 0f));
            yield return WaitSeconds(0.8f);
            Assert.That(Vector3.Dot(ModelFront(player), Vector3.right), Is.GreaterThan(0.95f), "Hero faces where it walks. " + Errors());
            Assert.That(player.transform.localScale, Is.EqualTo(Vector3.one), "The player root must not be mirrored.");
            MoveInput.SetValue(player, new Vector2(-1f, 0f));
            yield return null;
            yield return null;
            var midTurn = ModelFront(player);
            Assert.That(Vector3.Dot(midTurn, Vector3.right), Is.GreaterThan(0.2f), "Turning is smooth, not an instant flip.");
            yield return WaitSeconds(0.8f);
            Assert.That(Vector3.Dot(ModelFront(player), Vector3.left), Is.GreaterThan(0.95f), "Hero turned around. " + Errors());
            MoveInput.SetValue(player, Vector2.zero);

            // An enemy up the screen: when the Whip fires, the hero faces it and plays the attack pose.
            var animator = player.Animator.GetComponent<Animator>();
            int layer = animator.GetLayerIndex("UpperBodyAttack");
            Assert.That(layer, Is.GreaterThan(0), "Hero controller has no upper-body attack layer.");
            // Let a pose from a shot fired while walking (no enemy yet) finish first.
            yield return WaitUntilRealtime(() => animator.GetCurrentAnimatorStateInfo(layer).IsName("Empty") && !animator.IsInTransition(layer),
                "the previous attack pose to end");
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var enemyPrefabs = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs");
            var enemy = Object.Instantiate(enemyPrefabs[0], player.transform.position + new Vector3(0f, 0f, 3f), Quaternion.identity);
            var touch = enemy.GetComponent<TouchDamage>();
            if (touch != null) touch.enabled = false;

            bool posed = false;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!posed && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                posed = animator.GetCurrentAnimatorStateInfo(layer).IsName("Attack") || animator.GetNextAnimatorStateInfo(layer).IsName("Attack");
            }
            Assert.IsTrue(posed, "The attack pose never played when the weapon fired. " + Errors());
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"), Is.False, "Base layer (legs) keeps its idle/run state.");
            yield return WaitSeconds(0.25f);
            Assert.That(Vector3.Dot(player.FacingDirection, Vector3.forward), Is.GreaterThan(0.9f), "Facing points at the enemy. " + Errors());
            Assert.That(Vector3.Dot(ModelFront(player), Vector3.forward), Is.GreaterThan(0.8f), "The model turned to the enemy. " + Errors());
            if (enemy != null) Object.Destroy(enemy);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
