using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// A relic lying on a level (RelicBook; vampire.survivors.wiki/w/Relics, raw, fetched 2026-10-12: relics "are simply
    /// spawned on the map and are collected upon touching them. Once collected, they are permanently added to the
    /// collection menu and do not appear again"). Walking over it records it on the save with what it unlocks, lists it
    /// in the run's Results, shows the unlock notice with what it gives, and gives its boon to the run in play at once.
    /// The look is a placeholder, not owner-approved art: the approved sparkle sprite (VFX v2) in gold over the Soul
    /// Lantern's ground glow, as stage items lie.
    /// </summary>
    public sealed class RelicPickup : MonoBehaviour, SoulHunter.Gameplay.IMapMarker
    {
        public static readonly Color RelicColour = new Color(1f, 0.82f, 0.35f, 1f);

        /// <summary>Raised once when a relic is taken in a run, with its stable id (RelicBook.IdOf; RelicBook.Ids lists them).</summary>
        public static event System.Action<string> RelicFound;

        public RelicInfo Info { get; private set; }
        public bool IsTaken { get; private set; }

        // IMapMarker: the pause map and the guide arrows find it through this.
        public string MapKind => "Relic";
        public Vector3 MapPosition => transform.position;
        public bool MapVisible => !IsTaken;

        public static RelicPickup Create(RelicInfo info, Vector3 position)
        {
            var go = new GameObject("Relic_" + RelicBook.IdOf(info));
            go.transform.position = position;
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;
            trigger.center = new Vector3(0f, 0.8f, 0f);
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var relic = go.AddComponent<RelicPickup>();
            relic.Info = info;
            relic.BuildVisual();
            return relic;
        }

        private void BuildVisual()
        {
            var pivot = new GameObject("Visual").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(0f, 1.1f, 0f);
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            var texture = catalog != null ? catalog.ChestFxSparkle : null;
            if (texture != null)
            {
                var icon = new GameObject("Icon").AddComponent<SpriteRenderer>();
                icon.transform.SetParent(pivot, false);
                icon.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                icon.color = RelicColour;
                var cam = Camera.main;
                if (cam != null) icon.transform.rotation = cam.transform.rotation; // face the tilted game camera
                float size = Mathf.Max(icon.sprite.bounds.size.x, icon.sprite.bounds.size.y);
                icon.transform.localScale = Vector3.one * (1.6f / Mathf.Max(size, 0.01f));
            }
            var lanternGlow = catalog != null && catalog.SoulLantern != null ? catalog.SoulLantern.transform.Find("GroundGlow") : null;
            if (lanternGlow != null)
            {
                var glow = Instantiate(lanternGlow.gameObject, transform);
                glow.name = "GroundGlow";
                glow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                glow.transform.localScale = Vector3.one * 2.6f;
            }
            var motion = gameObject.AddComponent<SoulHunter.Gameplay.VFX.PickupIdleMotion>();
            motion.Visual = pivot;
            motion.BobHeight = 0.18f;
            motion.SpinDegreesPerSecond = 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsTaken) return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController>() == null) return;
            Take();
        }

        /// <summary>Gives the relic (also called by tests). Returns true when it was new on the save.</summary>
        public bool Take()
        {
            if (IsTaken) return false;
            IsTaken = true;
            SaveService save = null;
            GameServices.Instance?.TryGet(out save);
            var data = save != null ? save.CurrentData : null;
            var lines = RelicBook.MarkFound(data, Info);
            bool isNew = lines.Count > 0;
            var session = SoulHunter.Gameplay.Core.GameSessionManager.Instance;
            var notice = session != null ? session.GetComponent<SoulHunter.Gameplay.Core.CharacterUnlockTracker>() : null;
            var items = session != null ? session.GetComponent<SoulHunter.Gameplay.Core.ItemUnlockTracker>() : null;
            if (isNew)
            {
                // The relic's own line last, so it is the notice left on screen; every line is listed on the Results.
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (notice != null) notice.AnnounceUnlock(lines[i]);
                    else Debug.Log($"[RelicPickup] {lines[i]}");
                }
                if (items != null) items.ApplyBoon(Info); // this run gets the boon now; later runs get it at their start
                save.SaveGame();
            }
            // Results ("pickups and the amount of them"); CollectionTracker sees the same name, already found above.
            var stats = SoulHunter.Gameplay.Core.RunStatsTracker.Instance;
            if (stats != null) stats.RecordPickup(Info.Name);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, transform.position + Vector3.up * 0.5f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UIConfirmSound, 0.8f);
            if (isNew) RelicFound?.Invoke(RelicBook.IdOf(Info));
            Destroy(gameObject);
            return isNew;
        }
    }
}
