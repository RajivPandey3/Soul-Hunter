using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// The segmented body of Gallo Tower's flag dragons (Level 4's Ember and Blaze Serpents). vampire.survivors.wiki/w/Dragon_Shrimp
    /// (raw 2026-10-29): "Flag variant Dragon Shrimps can only deal damage to the player character through their heads, and
    /// likewise, the player character can only deal damage to, freeze, or defang this enemy by targeting its head"; "Players
    /// can safely cross through the body of the flag variants"; "When a flag variant of a Dragon Shrimp freezes, only its head
    /// will get frozen while the rest of its body won't. However, its body will continually shorten towards the frozen head."
    /// The enemy itself is the head (its collider, contact damage and health); the body is drawn only: segments with no
    /// collider, so nothing hits them and they hurt nothing. They follow the head's path, and close up on a head that stands.
    /// Balance method (the page gives no numbers): <see cref="Segments"/> segments <see cref="Spacing"/> units apart, each a
    /// little smaller, in the type's own colour; a standing head pulls its body in at <see cref="ShortenPerSecond"/>.
    /// </summary>
    public class SerpentBody : MonoBehaviour
    {
        public const int Segments = 6;
        public const float Spacing = 0.55f, ShortenPerSecond = 1.5f, SegmentSize = 0.6f, TaperPerSegment = 0.06f;

        /// <summary>The flag dragons' type ids (GalloTowerEnemies).</summary>
        public static readonly string[] SegmentedTypes = { "EmberSerpent", "BlazeSerpent" };

        private static Mesh _mesh;
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        private readonly List<Vector3> _trail = new List<Vector3>(64);
        private Transform[] _segments;
        private EnemyController _enemy;
        private float _spacing;

        public float CurrentSpacing => _spacing;
        public IReadOnlyList<Transform> SegmentTransforms => _segments;

        // Asked on every enemy spawn: the answer per type is kept (reading a name allocates).
        private static readonly Dictionary<SoulHunter.Gameplay.Data.EnemyData, bool> Known = new Dictionary<SoulHunter.Gameplay.Data.EnemyData, bool>();

        public static bool IsSegmented(SoulHunter.Gameplay.Data.EnemyData data)
        {
            if (data == null) return false;
            if (Known.TryGetValue(data, out bool known)) return known;
            bool segmented = false;
            string name = data.name;
            foreach (var id in SegmentedTypes)
                if (name.Contains("_" + id + "_")) { segmented = true; break; }
            Known[data] = segmented;
            return segmented;
        }

        /// <summary>Gives <paramref name="enemy"/> its body if it is a flag dragon; restarts it on every spawn.</summary>
        public static void AttachIfSegmented(EnemyController enemy)
        {
            if (enemy == null || !IsSegmented(enemy.Data)) return;
            var body = enemy.GetComponent<SerpentBody>();
            if (body == null) body = enemy.gameObject.AddComponent<SerpentBody>();
            body._enemy = enemy;
            body.Restart();
        }

        private void Restart()
        {
            if (_segments == null) Build();
            _trail.Clear();
            _trail.Add(transform.position);
            _spacing = Spacing;
            _placed = false; // the spawner moves a pooled body after OnEnable: the first frame starts the trail again
            _hidden = true;
            ShowSegments(true);
            for (int i = 0; i < _segments.Length; i++) _segments[i].position = transform.position;
        }

        private void Build()
        {
            if (_mesh == null)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
                Destroy(sphere);
            }
            Color colour = _enemy != null && _enemy.TypeTint.HasValue ? _enemy.TypeTint.Value : new Color(0.9f, 0.4f, 0.2f, 1f);
            if (!Materials.TryGetValue(colour, out var material) || material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader != null ? shader : Shader.Find("Standard")) { name = "SH10_SerpentBody", enableInstancing = true };
                material.color = colour;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
                Materials[colour] = material;
            }
            // The segments are not children: the head turns and scales, and its body must not swing with it.
            var root = new GameObject(name + "_Body").transform;
            _root = root;
            _segments = new Transform[Segments];
            for (int i = 0; i < Segments; i++)
            {
                var go = new GameObject("Segment" + i, typeof(MeshFilter), typeof(MeshRenderer));
                go.GetComponent<MeshFilter>().sharedMesh = _mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                go.transform.SetParent(root, false);
                go.transform.localScale = Vector3.one * SegmentSize * (1f - TaperPerSegment * i);
                _segments[i] = go.transform;
            }
        }

        private Transform _root;
        private bool _placed;
        private bool _hidden;

        private void ShowSegments(bool show)
        {
            if (_hidden == !show) return;
            _hidden = !show;
            foreach (var segment in _segments) if (segment != null) segment.gameObject.SetActive(show);
        }

        private void LateUpdate()
        {
            if (_segments == null) return;
            float dt = Time.deltaTime; // game time: a pause holds the body too
            if (dt <= 0f) return;
            // A dead head waiting in the death queue has left the screen; its body goes with it at once.
            if (_enemy != null && _enemy.IsDying) { ShowSegments(false); return; }
            Vector3 head = transform.position;
            if (!_placed)
            {
                _placed = true;
                _trail.Clear();
                _trail.Add(head);
                foreach (var segment in _segments) segment.position = head;
                ShowSegments(true);
            }
            Vector3 last = _trail[_trail.Count - 1];
            if ((head - last).sqrMagnitude > 25f) { Restart(); return; } // placed by the spawner after its pool woke it
            bool moving = (head - last).sqrMagnitude > 0.0004f;
            if (moving)
            {
                _trail.Add(head);
                _spacing = Mathf.MoveTowards(_spacing, Spacing, ShortenPerSecond * dt);
                float keep = Spacing * (Segments + 2);
                // Drop trail points the last segment no longer needs.
                float length = 0f;
                for (int i = _trail.Count - 1; i > 0; i--)
                {
                    length += Vector3.Distance(_trail[i], _trail[i - 1]);
                    if (length > keep) { _trail.RemoveRange(0, i - 1); break; }
                }
            }
            else
            {
                // "its body will continually shorten towards the frozen head"
                _spacing = Mathf.MoveTowards(_spacing, 0f, ShortenPerSecond * dt);
            }
            for (int s = 0; s < _segments.Length; s++) _segments[s].position = PointBehind(head, _spacing * (s + 1));
        }

        /// <summary>The point <paramref name="distance"/> back along the head's path (the oldest point if the path is shorter).</summary>
        private Vector3 PointBehind(Vector3 head, float distance)
        {
            Vector3 from = head;
            for (int i = _trail.Count - 1; i >= 0; i--)
            {
                Vector3 to = _trail[i];
                float step = Vector3.Distance(from, to);
                if (step >= distance && step > 0f) return Vector3.Lerp(from, to, distance / step);
                distance -= step;
                from = to;
            }
            return from;
        }

        private void OnDisable()
        {
            if (_segments == null) return;
            ShowSegments(false);
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }
    }
}
