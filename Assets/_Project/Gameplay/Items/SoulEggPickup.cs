using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// A Soul Egg lying on the ground (VS Golden Egg; dropped by a stage item's guardian, ItemGuardian). Walking over it
    /// gives the egg to the character being played, exactly as one bought from the Wandering Merchant: one random stat,
    /// saved, counting from that character's next run (SoulEggs). It shows the notice and is listed on the Results.
    /// The look is the approved Soul Egg icon over the Soul Lantern's ground glow, as stage items lie.
    /// </summary>
    public sealed class SoulEggPickup : MonoBehaviour
    {
        public const string PickupName = "Soul Egg";

        public bool IsTaken { get; private set; }
        /// <summary>The stat the last taken egg raised (tests).</summary>
        public static SoulEggs.Stat LastStat { get; private set; }

        public static SoulEggPickup Create(Vector3 position)
        {
            var go = new GameObject("SoulEgg");
            go.transform.position = position;
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;
            trigger.center = new Vector3(0f, 0.8f, 0f);
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var egg = go.AddComponent<SoulEggPickup>();
            egg.BuildVisual();
            return egg;
        }

        private void BuildVisual()
        {
            var pivot = new GameObject("Visual").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(0f, 1.1f, 0f);
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            var icon = new GameObject("Icon").AddComponent<SpriteRenderer>();
            icon.transform.SetParent(pivot, false);
            icon.sprite = catalog != null ? catalog.SoulEggIcon : null;
            var cam = Camera.main;
            if (cam != null) icon.transform.rotation = cam.transform.rotation; // face the tilted game camera
            if (icon.sprite != null)
            {
                float size = Mathf.Max(icon.sprite.bounds.size.x, icon.sprite.bounds.size.y);
                icon.transform.localScale = Vector3.one * (1.3f / Mathf.Max(size, 0.01f));
            }
            var lanternGlow = catalog != null && catalog.SoulLantern != null ? catalog.SoulLantern.transform.Find("GroundGlow") : null;
            if (lanternGlow != null)
            {
                var glow = Instantiate(lanternGlow.gameObject, transform);
                glow.name = "GroundGlow";
                glow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                glow.transform.localScale = Vector3.one * 2.2f;
            }
            var motion = gameObject.AddComponent<SoulHunter.Gameplay.VFX.PickupIdleMotion>();
            motion.Visual = pivot;
            motion.BobHeight = 0.15f;
            motion.SpinDegreesPerSecond = 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsTaken) return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController>() == null) return;
            Take();
        }

        /// <summary>Gives the egg (also called by tests). Returns true when it was written to the save.</summary>
        public bool Take()
        {
            if (IsTaken) return false;
            IsTaken = true;
            SaveService save = null;
            GameServices.Instance?.TryGet(out save);
            var data = save != null ? save.CurrentData : null;
            bool given = data != null && !string.IsNullOrEmpty(data.SelectedCharacterName);
            if (given)
            {
                LastStat = SoulEggs.Add(data, data.SelectedCharacterName, new System.Random());
                save.SaveInBackground();
                string line = $"Soul Egg: {SoulEggs.Describe(LastStat)} for {data.SelectedCharacterName} (from the next run)";
                var session = SoulHunter.Gameplay.Core.GameSessionManager.Instance;
                var notice = session != null ? session.GetComponent<SoulHunter.Gameplay.Core.CharacterUnlockTracker>() : null;
                if (notice != null) notice.ShowMessage(line);
                else Debug.Log($"[SoulEggPickup] {line}");
            }
            var stats = SoulHunter.Gameplay.Core.RunStatsTracker.Instance;
            if (stats != null) stats.RecordPickup(PickupName);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, transform.position + Vector3.up * 0.5f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UIConfirmSound, 0.8f);
            Destroy(gameObject);
            return given;
        }
    }
}
