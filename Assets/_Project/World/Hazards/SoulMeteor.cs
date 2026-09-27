using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// One Soul Meteor, Soul Hunter's version of the VS "Shooting Star" (vampire.survivors.wiki, Gallo Tower: "rains
    /// group of falling stars that impact at the marker locations after 2 seconds, dealing 30 damage in the area"). A
    /// glowing marker lies on the ground and tightens for 2 s (game time), then the strike hits everything inside it:
    /// 30 damage to the player and 30 to every enemy. Visual: the approved VFX v2 magic circle as the marker
    /// (GameContentCatalog.HazardMarker) and the pooled explosion for the strike; no new art.
    /// </summary>
    public class SoulMeteor : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public float RemainingSeconds { get; set; }
        public float Radius { get; private set; } = StageHazardTable.MeteorRadius;
        public bool HasStruck { get; private set; }
        public bool HitPlayer { get; private set; }
        public int EnemiesHit { get; private set; }

        private Transform _visual;
        private Color _tint = Color.white;
        private MaterialPropertyBlock _block;
        private float _delay;

        /// <summary>Lays a marker at position (on the player's plane); it strikes after delaySeconds of game time.</summary>
        public static SoulMeteor Create(Vector3 position, Color tint, float delaySeconds = StageHazardTable.MeteorDelaySeconds,
            float radius = StageHazardTable.MeteorRadius)
        {
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            GameObject go;
            if (catalog != null && catalog.HazardMarker != null)
            {
                go = Instantiate(catalog.HazardMarker, position, Quaternion.identity);
            }
            else
            {
                // Placeholder when the builder has not been run: a plain flat quad.
                go = new GameObject("SoulMeteor_Marker");
                go.transform.position = position;
                var quad = new GameObject("Circle");
                quad.transform.SetParent(go.transform, false);
                quad.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                quad.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                var shader = Shader.Find("SoulHunter/VFX/Particle");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                quad.AddComponent<MeshRenderer>().sharedMaterial = new Material(shader);
            }
            go.name = "SoulMeteor_Marker";
            var meteor = go.GetComponent<SoulMeteor>();
            if (meteor == null) meteor = go.AddComponent<SoulMeteor>();
            meteor.Setup(tint, delaySeconds, radius);
            return meteor;
        }

        private void Setup(Color tint, float delaySeconds, float radius)
        {
            _tint = tint;
            _delay = Mathf.Max(0.05f, delaySeconds);
            RemainingSeconds = _delay;
            Radius = Mathf.Max(0.1f, radius);
            _visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            // The marker is a flat quad (1 unit across, turned to lie on the ground); its diameter matches the strike.
            if (_visual != null) _baseScale = new Vector3(Radius * 2f, Radius * 2f, 1f);
            Refresh(0f);
        }

        private Vector3 _baseScale = Vector3.one;

        private void Update()
        {
            if (HasStruck) return;
            RemainingSeconds -= Time.deltaTime; // game time: a paused game holds the strike
            float progress = 1f - Mathf.Clamp01(RemainingSeconds / _delay);
            Refresh(progress);
            if (RemainingSeconds <= 0f) Strike();
        }

        /// <summary>The marker spins, closes in from 1.3x to its strike size and brightens as the strike nears.</summary>
        private void Refresh(float progress)
        {
            if (_visual == null) return;
            _visual.localScale = _baseScale * Mathf.Lerp(1.3f, 1f, progress);
            _visual.Rotate(0f, 0f, 90f * Time.deltaTime, Space.Self);
            if (_block == null) _block = new MaterialPropertyBlock();
            var r = _visual.GetComponent<Renderer>();
            if (r == null) return;
            r.GetPropertyBlock(_block);
            Color c = _tint;
            float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(progress * Mathf.PI * (2f + progress * 6f)));
            c.a = Mathf.Lerp(0.35f, 1f, progress) * pulse;
            _block.SetColor(ColorId, c);
            r.SetPropertyBlock(_block);
        }

        /// <summary>The strike: damage in the circle, an explosion, and the marker goes.</summary>
        public void Strike()
        {
            if (HasStruck) return;
            HasStruck = true;
            Vector3 centre = transform.position;

            var player = PlayerController.Instance;
            if (player != null && Flat(player.transform.position - centre) <= Radius + 0.4f)
            {
                var health = player.GetComponent<HealthController>();
                if (health != null)
                {
                    health.TakeDamage(new DamagePacket(StageHazardTable.MeteorPlayerDamage, centre, Vector3.zero));
                    HitPlayer = true;
                }
            }

            var enemies = EnemyController.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue; // a kill removes enemies from the list
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                if (Flat(enemy.transform.position - centre) > Radius + enemy.CrowdRadius) continue;
                var health = enemy.GetComponent<HealthController>();
                if (health == null) continue;
                health.TakeDamage(new DamagePacket(StageHazardTable.MeteorEnemyDamage, centre, enemy.transform.position - centre));
                EnemiesHit++;
            }

            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, centre + Vector3.up * 0.3f);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.HitSpark, centre + Vector3.up * 0.8f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFXThrottled(audio.ExplosionSound, 0.08f, 0.6f);
            Destroy(gameObject);
        }

        private static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }
    }
}
