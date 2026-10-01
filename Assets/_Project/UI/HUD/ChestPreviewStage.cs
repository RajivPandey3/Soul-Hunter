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
            Texture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "ChestPreview", antiAliasing = 2 };

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
            _glow.color = arcana ? new Color(0.75f, 0.45f, 1f) : new Color(0.45f, 0.95f, 1f);
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
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(behaviour);
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
            _hingeBase = hinge.rotation;

            // Lift the front edge (-Z) up and back.
            Vector3 front = new Vector3(0f, 0f, -_bounds.size.z);
            _lidSign = (Quaternion.AngleAxis(60f, Vector3.right) * front).y > 0f ? 1f : -1f;
        }

        private static GameObject CoinVisual(GameObject coinPrefab)
        {
            if (coinPrefab == null) return null;
            var gold = coinPrefab.transform.Find("Visual/Tier_Gold") ?? coinPrefab.transform.Find("Tier_Gold");
            return gold != null ? gold.gameObject : null;
        }

        /// <summary>Opens the lid and starts the coin burst (<paramref name="coins"/> coins over about a second).</summary>
        public void Open(int coins)
        {
            _opening = true;
            _coinsLeft = coins;
            _glow.intensity = 1.2f;
        }

        /// <summary>Jumps straight to the open chest (the animation was skipped).</summary>
        public void FinishNow()
        {
            _opening = true;
            _skipped = true;
            _lidT = 1f;
            _dropT = 1f;
            _coinsLeft = 0f;
            ApplyPose();
        }

        private void Update()
        {
            if (_needsLayout) Layout();
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (_dropT < 1f) _dropT = Mathf.Min(1f, _dropT + dt / 0.35f);
            if (_opening && _lidT < 1f) _lidT = Mathf.Min(1f, _lidT + dt / LidOpenSeconds);
            ApplyPose();
            if (_opening && _glow != null) _glow.intensity = Mathf.Lerp(_glow.intensity, 4f, dt * 4f);

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
