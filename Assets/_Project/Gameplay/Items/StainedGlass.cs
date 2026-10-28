using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// VS Stained Glass (vampire.survivors.wiki/w/Stained_Glass, raw 2026-10-28): "a destructible, akin to light sources,
    /// that can be found in Cappella Magna"; "When the player deals 50 damage to a flashy window, it explodes into vertical
    /// beams of light. Any enemies that are caught in the explosion are dealt 130 Base Damage" (infobox: damage 130, area
    /// 1.25). The reapers "take 10 times more damage from breaking Stained Glass windows" (The_Stalker, The_Drowner,
    /// The_Trickster). Soul Hunter's Levels 8-10 play Cappella Magna.
    /// Like a Soul Lantern it is tagged and layered as an enemy so every weapon breaks it, but it is not an enemy: nothing
    /// aims at it, it is no kill, and it never hurts or blocks the hero.
    /// Balance method (the wiki gives no sizes or places): the blast reaches 3 units x the window's area 1.25 x the hero's
    /// Area, damage is 130 x Might, and a window that broke does not come back on that stage. The look is a placeholder.
    /// </summary>
    [RequireComponent(typeof(HealthController))]
    public class StainedGlass : MonoBehaviour
    {
        public const int MaxHealth = 50;
        public const int BaseDamage = 130;
        public const float WeaponArea = 1.25f;
        public const float BlastBaseRadius = 3f;
        public const float ReaperMultiplier = 10f;
        public const string SourceName = "Stained Glass";

        private HealthController _health;
        private static readonly Collider[] Hits = new Collider[512];

        public int LastBlastHits { get; private set; }
        public bool IsBroken { get; private set; }

        /// <summary>A window, built in code (a placeholder: a tall glowing pane on a stone frame).</summary>
        public static StainedGlass Create(Vector3 at)
        {
            var go = new GameObject("SH10_StainedGlass");
            go.transform.position = at;
            go.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) go.layer = enemyLayer;
            var body = go.AddComponent<BoxCollider>();
            body.isTrigger = true;
            body.size = new Vector3(1.4f, 3f, 0.5f);
            body.center = new Vector3(0f, 1.5f, 0f);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            go.AddComponent<HealthController>();
            var pane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(pane.GetComponent<Collider>());
            pane.transform.SetParent(go.transform, false);
            pane.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            pane.transform.localScale = new Vector3(1.2f, 2.8f, 0.15f);
            var renderer = pane.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var glow = new MaterialPropertyBlock();
            glow.SetColor("_BaseColor", new Color(0.55f, 0.3f, 0.95f, 1f));
            glow.SetColor("_EmissionColor", new Color(0.7f, 0.45f, 1.2f, 1f));
            renderer.SetPropertyBlock(glow);
            return go.AddComponent<StainedGlass>();
        }

        private void Awake()
        {
            _health = GetComponent<HealthController>();
            _health.KnockbackResistance = 1f;
        }

        private void OnEnable()
        {
            _health.Initialize(MaxHealth);
            _health.IsInvincible = false;
            IsBroken = false;
            _health.OnDied += Break;
        }

        private void OnDisable() => _health.OnDied -= Break;

        private void Break()
        {
            if (IsBroken) return;
            IsBroken = true;
            Vector3 at = transform.position;
            var stats = SoulHunter.Gameplay.Player.PlayerController.Instance != null ? SoulHunter.Gameplay.Player.PlayerController.Instance.Stats : null;
            float might = stats != null ? stats.Might : 1f;
            float area = stats != null ? stats.Area : 1f;
            int damage = Mathf.Max(1, Mathf.RoundToInt(BaseDamage * might));
            float radius = BlastBaseRadius * WeaponArea * Mathf.Max(0.1f, area);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, at + Vector3.up);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, at + Vector3.up * 0.3f);
            int mask = 1 << Mathf.Max(0, LayerMask.NameToLayer("Enemy"));
            int count = UnityEngine.Physics.OverlapSphereNonAlloc(at, radius, Hits, mask, QueryTriggerInteraction.Collide);
            var done = new HashSet<HealthController>();
            LastBlastHits = 0;
            using (DamageSource.From(null, SourceName))
            {
                for (int i = 0; i < count; i++)
                {
                    var target = Hits[i] != null ? DamageableLookup.Of(Hits[i]) as HealthController : null;
                    if (target == null || target == _health || !done.Add(target)) continue;
                    if (target.GetComponent<StainedGlass>() != null || target.GetComponent<LightSource>() != null) continue;
                    if (target.GetComponent<EnemyController>() == null) continue;
                    bool reaper = target.GetComponent<SoulHunter.Gameplay.Hazards.DeathsShadow>() != null
                               || (target.TryGetComponent<EnemyController>(out var enemy) && enemy.IgnoresPentagram);
                    int dealt = reaper ? Mathf.RoundToInt(damage * ReaperMultiplier) : damage;
                    target.TakeDamage(new DamagePacket(dealt, at, (target.transform.position - at).normalized));
                    LastBlastHits++;
                }
            }
        }
    }
}
