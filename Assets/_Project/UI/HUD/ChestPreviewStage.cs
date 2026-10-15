using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The 3D part of the chest-opening screen: a copy of the approved Soul Reliquary chest, far below the stage, filmed
    /// by its own camera into a RenderTexture the screen shows. The lid really opens (its parts are hinged at the back)
    /// and approved gold Soul Coins burst out of it. Everything runs on unscaled time, because the game is paused.
    /// </summary>
    public sealed class ChestPreviewStage : MonoBehaviour
    {
        /// <summary>Far below the stage floor (y = 0), out of the game camera's view.</summary>
        public static readonly Vector3 StagePosition = new Vector3(0f, -1000f, 0f);
        public const float LidOpenAngle = 105f, LidOpenSeconds = 0.35f;

        public RenderTexture Texture { get; private set; }
        public bool LidOpen => _lidT >= 1f;
        /// <summary>Laid out and done dropping in.</summary>
        public bool Landed => !_needsLayout && _dropT >= 1f;
        public int CoinsInFlight => _coins.Count;

        private Camera _camera;
        private Transform _chest, _hinge;
        private Quaternion _hingeBase = Quaternion.identity;
        private Light _glow;
        private float _lidSign = 1f, _lidT, _dropT = 1f, _baseY;
        private bool _needsLayout, _skipped;
        private bool _opening;
        private GameObject _coinTemplate;
        private float _coinsLeft, _coinTimer;
        private Bounds _bounds;

        private struct Coin { public Transform T; public Vector3 V; public float Age; public Vector3 Spin; }
        private readonly List<Coin> _coins = new List<Coin>();

        public static ChestPreviewStage Create(GameObject chestPrefab, GameObject coinPrefab, bool arcana,
            SoulHunter.Gameplay.Pickups.ChestKind kind = SoulHunter.Gameplay.Pickups.ChestKind.Silver)
        {
            var go = new GameObject("ChestPreviewStage");
            go.transform.position = StagePosition;
            var stage = go.AddComponent<ChestPreviewStage>();
            stage.Build(chestPrefab, coinPrefab, arcana);
            // The opened chest's own look (bronze, silver, Arcana) on the preview copy.
            if (stage._chest != null)
                SoulHunter.Gameplay.Pickups.ChestLook.Apply(stage._chest, arcana ? SoulHunter.Gameplay.Pickups.ChestKind.Arcana : kind);
            return stage;
        }

        private void Build(GameObject chestPrefab, GameObject coinPrefab, bool arcana)
        {
            // MSAA only as far as the device gives it: on the owner's phone (Vulkan) a 2-sample texture came back with 1, and
            // every chest preview frame logged "RenderPass: Attachment 0 was created with 1 samples but 2 samples were
            // requested" (2026-10-10). The camera's MSAA follows the texture (below).
            var desc = new RenderTextureDescriptor(512, 512, RenderTextureFormat.ARGB32, 16) { msaaSamples = 2 };
            // The device's own answer was 2 there too, so phones get none; PCs keep the supported count.
            int samples = Application.isMobilePlatform ? 1 : Mathf.Max(1, SystemInfo.GetRenderTextureSupportedMSAASampleCount(desc));
            Texture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "ChestPreview", antiAliasing = samples };

            var visual = chestPrefab != null ? chestPrefab.transform.Find("Visual") : null;
            if (visual != null)
            {
                _chest = Instantiate(visual.gameObject, transform).transform;
                _chest.localPosition = Vector3.zero;
                StripBehaviours(_chest.gameObject);
            }
            _coinTemplate = CoinVisual(coinPrefab);

            var camGo = new GameObject("ChestPreviewCamera");
            camGo.transform.SetParent(transform, false);
            _camera = camGo.AddComponent<Camera>();
            _camera.allowMSAA = Texture.antiAliasing > 1;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.fieldOfView = 28f;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 30f;
            _camera.targetTexture = Texture;
            _camera.allowHDR = false;
            _camera.enabled = false; // until Layout has placed it

            var lightGo = new GameObject("ChestGlow");
            lightGo.transform.SetParent(transform, false);
            _glow = lightGo.AddComponent<Light>();
            _glow.type = LightType.Point;
            _glow.color = arcana ? UITheme.Rewards.Arcana : UITheme.Rewards.Soul; // Theme Studio > Rewards
            _glow.intensity = 1.2f;
            _needsLayout = true;
        }

        /// <summary>
        /// Runs on the first Update: the renderer bounds of a copy made this frame are not up to date yet (they read the
        /// stage position), which put the camera and the hinge on the wrong side (found 2026-09-29).
        /// The approved chest's FBX is turned inside the prefab, so its own up (body to lid) and front (towards the lock
        /// plate) are measured, and the copy is turned upright with its front towards the camera (-Z).
        /// </summary>
        private void Layout()
        {
            _needsLayout = false;
            if (_chest != null)
            {
                var renderers = _chest.GetComponentsInChildren<Renderer>();
                var whole = RendererBounds(_chest);
                Vector3 lid = Vector3.zero, lockPlate = Vector3.zero;
                int lids = 0;
                bool hasLock = false;
                foreach (var r in renderers)
                {
                    if (r.name.StartsWith("Lid")) { lid += r.bounds.center; lids++; }
                    if (!hasLock && r.name.StartsWith("LockPlate")) { lockPlate = r.bounds.center; hasLock = true; }
                }
                Vector3 up = lids > 0 ? SnapToAxis(lid / lids - whole.center) : Vector3.up;
                Vector3 front = hasLock ? SnapToAxis(Vector3.ProjectOnPlane(lockPlate - whole.center, up)) : Vector3.back;
                if (Mathf.Abs(Vector3.Dot(front, up)) > 0.5f) front = Vector3.back;
                var turn = Quaternion.LookRotation(Vector3.back, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(front, up));

                // Each part's bounds after the turn (computed, since the renderers' bounds lag behind a transform change).
                var turned = new Dictionary<Renderer, Bounds>();
                foreach (var r in renderers)
                    turned[r] = new Bounds(whole.center + turn * (r.bounds.center - whole.center), Abs(turn * r.bounds.size));
                _chest.position = whole.center + turn * (_chest.position - whole.center);
                _chest.rotation = turn * _chest.rotation;
                _bounds = new Bounds(whole.center, Abs(turn * whole.size));
                BuildHinge(turned);
            }
            else _bounds = new Bounds(transform.position, Vector3.one);

            float size = Mathf.Max(0.5f, _bounds.size.magnitude);
            // Film the chest from its front (lock and skull, now at -Z), a little above.
            Vector3 target = _bounds.center + Vector3.up * size * 0.08f;
            _camera.transform.position = target + new Vector3(0f, size * 0.95f, -size * 2.1f);
            _camera.transform.LookAt(target);
            _camera.enabled = true;
            _glow.transform.position = _bounds.center + new Vector3(0f, _bounds.extents.y * 1.4f, -_bounds.extents.z * 0.6f);
            _glow.range = size * 3f;
            if (_chest != null) { _baseY = _chest.localPosition.y; _dropT = _skipped ? 1f : 0f; }
        }

        private static Vector3 SnapToAxis(Vector3 v)
        {
            Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
            if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            if (a.y >= a.z) return new Vector3(0f, Mathf.Sign(v.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(v.z));
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private static void StripBehaviours(GameObject root)
        {
            // PropMergedLook stays: it only tells ChestLook and the seam dimming which parts a merged mesh holds.
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(behaviour is SoulHunter.Gameplay.VFX.PropMergedLook)) Destroy(behaviour);
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Destroy(collider);
        }

        private static Bounds RendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.one);
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        /// <summary>
        /// Everything above the seam (the lid, its bands and the skull) is hung on a hinge along the back edge. The chest
        /// is upright with its front at -Z by now, so the hinge turns about world X.
        /// </summary>
        private void BuildHinge(Dictionary<Renderer, Bounds> turned)
        {
            // The seam is the bottom of the lid shell itself ("Lid"). The LidBand rings are wide circles that reach far below
            // it; counting them put the seam low, so most of the body swung up with the lid and an open chest showed a bare
            // slab under an upright panel (Bug MIS B11, 2026-09-30).
            float seam = float.MaxValue;
            foreach (var pair in turned)
                if (pair.Key.name.StartsWith("Lid") && !pair.Key.name.StartsWith("LidBand")) seam = Mathf.Min(seam, pair.Value.min.y);
            if (seam == float.MaxValue) seam = _bounds.center.y + _bounds.extents.y * 0.25f; // no named lid: the top quarter

            var hinge = new GameObject("LidHinge").transform;
            hinge.SetParent(_chest, true);
            hinge.position = new Vector3(_bounds.center.x, seam, _bounds.max.z); // the back edge (+Z)
            hinge.rotation = Quaternion.identity;
            var lidParts = new List<Transform>();
            foreach (var pair in turned)
                if (pair.Value.center.y > seam) lidParts.Add(pair.Key.transform);
            foreach (var part in lidParts) part.SetParent(hinge, true);
            _hinge = hinge;

            // Bug MIS v2 C07: an opened lid shows its inside, which single-sided materials culled, so it looked
            // see-through and banded. The preview's lid draws both faces (instances on this copy only).
            foreach (var r in hinge.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials;
                foreach (var m in mats)
                    if (m != null && m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
                r.materials = mats;
            }
            // The soul-light seam left in the body (its top fills the opening once the lid is up).
            _seams.Clear();
            foreach (var r in _chest.GetComponentsInChildren<Renderer>(true))
                if (!r.transform.IsChildOf(hinge) && SoulHunter.Gameplay.VFX.PropMergedLook.Draws(r, "Chest_Soul"))
                    _seams.Add(r);
            _hingeBase = hinge.rotation;

            // Lift the front edge (-Z) up and back.
            Vector3 front = new Vector3(0f, 0f, -_bounds.size.z);
            _lidSign = (Quaternion.AngleAxis(60f, Vector3.right) * front).y > 0f ? 1f : -1f;
        }

        /// <summary>The coin model the burst copies: the prefab's biggest tier (every tier is the gold coin since 2026-10-13).</summary>
        public static GameObject CoinVisual(GameObject coinPrefab)
        {
            if (coinPrefab == null) return null;
            foreach (string name in new[] { "Tier_3", "Tier_Gold" })
            {
                var gold = coinPrefab.transform.Find("Visual/" + name) ?? coinPrefab.transform.Find(name);
                if (gold != null) return gold.gameObject;
            }
            return null;
        }

        /// <summary>Opens the lid and starts the coin burst (<paramref name="coins"/> coins over about a second).</summary>
        public void Open(int coins)
        {
            _opening = true;
            _coinsLeft = coins;
            _glow.intensity = 1.2f;
            DimSeams();
            // Light pours out of the opening (the approved chest-open burst; it plays while the game is paused).
            if (_chest != null)
                SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.ChestOpen,
                    new Vector3(_bounds.center.x, _bounds.max.y, _bounds.center.z));
        }

        private readonly List<Renderer> _seams = new List<Renderer>();

        /// <summary>The seam glows dimly once the lid is up (Bug MIS v2 C07: it filled the opening as a flat cyan slab).</summary>
        public const float OpenSeamGlow = 0.1f;

        /// <summary>The preview light once the chest is open (it was 4).</summary>
        public const float OpenGlowIntensity = 1.6f;

        /// <summary>The preview light's intensity (tests).</summary>
        public float GlowIntensity => _glow != null ? _glow.intensity : 0f;

        /// <summary>The seam's glow relative to the closed chest (tests): 1 closed, OpenSeamGlow open.</summary>
        public float SeamGlow { get; private set; } = 1f;

        private void DimSeams()
        {
            if (SeamGlow < 1f) return;
            SeamGlow = OpenSeamGlow;
            var block = new MaterialPropertyBlock();
            foreach (var r in _seams)
            {
                if (r == null) continue;
                // Every soul part of the renderer: each Chest_Soul submesh of an ordinary one, the mask B part of a
                // merged one (PropMergedLook keeps its iron bands' colour on the same block).
                for (int index = 0; SoulHunter.Gameplay.VFX.PropMergedLook.TryFind(r, "Chest_Soul", out var part, index); index++)
                {
                    Color emission = SoulHunter.Gameplay.VFX.PropMergedLook.CurrentEmission(part, block);
                    Color baseColor = SoulHunter.Gameplay.VFX.PropMergedLook.CurrentBase(part, block);
                    // a dark inside with a faint soul glow
                    SoulHunter.Gameplay.VFX.PropMergedLook.Set(part, block, baseColor * 0.12f, emission * OpenSeamGlow);
                }
            }
        }

        /// <summary>Jumps straight to the open chest (the animation was skipped).</summary>
        public void FinishNow()
        {
            _opening = true;
            _skipped = true;
            _lidT = 1f;
            _dropT = 1f;
            _coinsLeft = 0f;
            DimSeams();
            ApplyPose();
        }

        private void Update()
        {
            if (_needsLayout) Layout();
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (_dropT < 1f) _dropT = Mathf.Min(1f, _dropT + dt / 0.35f);
            if (_opening && _lidT < 1f) _lidT = Mathf.Min(1f, _lidT + dt / LidOpenSeconds);
            ApplyPose();
            // Bug MIS v2 C07: at 4.0 the cyan glow lit the open chest's inside planks into a flat bright slab.
            if (_opening && _glow != null) _glow.intensity = Mathf.Lerp(_glow.intensity, OpenGlowIntensity, dt * 4f);

            // Coin burst out of the open lid.
            if (_coinsLeft > 0f && _lidT > 0.5f && _coinTemplate != null)
            {
                _coinTimer -= dt;
                while (_coinTimer <= 0f && _coinsLeft > 0f)
                {
                    _coinTimer += 0.035f;
                    _coinsLeft -= 1f;
                    SpawnCoin();
                }
            }
            for (int i = _coins.Count - 1; i >= 0; i--)
            {
                var c = _coins[i];
                c.Age += dt;
                c.V += Vector3.down * 9f * dt;
                c.T.position += c.V * dt;
                c.T.Rotate(c.Spin * dt, Space.Self);
                if (c.Age > 1.6f) { Destroy(c.T.gameObject); _coins.RemoveAt(i); continue; }
                _coins[i] = c;
            }
        }

        private void ApplyPose()
        {
            if (_chest != null)
            {
                // Drop in with a small bounce.
                float t = _dropT;
                float bounce = t < 1f ? (1f - t) * (1f - t) * _bounds.size.y * 2.2f - Mathf.Sin(t * Mathf.PI) * 0.05f : 0f;
                var p = _chest.localPosition; p.y = _baseY + bounce; _chest.localPosition = p;
            }
            if (_hinge != null)
            {
                float eased = 1f - (1f - _lidT) * (1f - _lidT) * (1f - _lidT);
                _hinge.rotation = Quaternion.AngleAxis(_lidSign * LidOpenAngle * eased, Vector3.right) * _hingeBase;
            }
        }

        private void SpawnCoin()
        {
            var coin = Instantiate(_coinTemplate, transform).transform;
            StripBehaviours(coin.gameObject);
            coin.gameObject.SetActive(true);
            float s = _bounds.size.x * 0.22f;
            coin.localScale = coin.localScale * (s / Mathf.Max(0.01f, RendererBounds(coin).size.magnitude));
            coin.position = _bounds.center + Vector3.up * _bounds.extents.y * 0.6f;
            var dir = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(1.6f, 2.4f), -Random.Range(-0.1f, 0.9f)); // toward the camera
            _coins.Add(new Coin
            {
                T = coin, V = dir * _bounds.size.magnitude * 1.6f,
                Spin = new Vector3(Random.Range(200f, 600f), Random.Range(-200f, 200f), 0f)
            });
        }

        private void OnDestroy()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (Texture != null) { Texture.Release(); Destroy(Texture); }
        }
    }
}
