using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Heavy-kill performance pass (2026-09-29): "is the player within <c>radius</c> of this point?" without a physics
    /// query. Every coin, chicken and other pickup on the ground asked this with Physics.OverlapSphereNonAlloc every frame;
    /// with hundreds lying around (a strong hero kills thousands) that was about 2 ms a frame in the profile. This gives the
    /// same answer as an overlap sphere on the Player layer: the distance from the point to the nearest player collider on
    /// that layer (Collider.ClosestPoint) is at most the radius. A far point is rejected by one distance check.
    /// </summary>
    public static class PlayerReach
    {
        private static Transform _player;
        private static readonly List<Collider> _colliders = new List<Collider>();
        private static float _extent;

        /// <summary>The player collider within <paramref name="radius"/> of <paramref name="point"/>, or null.</summary>
        public static Collider Within(Vector3 point, float radius)
        {
            var controller = SoulHunter.Gameplay.Player.PlayerController.Instance;
            if (controller == null) return null;
            if (_player != controller.transform) Cache(controller.transform);

            // Cheap reject: the whole player body is within _extent of its position.
            float reach = radius + _extent;
            if ((point - _player.position).sqrMagnitude > reach * reach) return null;

            for (int i = 0; i < _colliders.Count; i++)
            {
                var collider = _colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                if ((collider.ClosestPoint(point) - point).sqrMagnitude <= radius * radius) return collider;
            }
            return null;
        }

        private static void Cache(Transform player)
        {
            _player = player;
            _colliders.Clear();
            _extent = 0f;
            int layer = LayerMask.NameToLayer("Player");
            foreach (var collider in player.GetComponentsInChildren<Collider>(true))
            {
                if (layer >= 0 && collider.gameObject.layer != layer) continue;
                // Mesh colliders that are not convex have no ClosestPoint; the player's body is a capsule.
                if (collider is MeshCollider mesh && !mesh.convex) continue;
                _colliders.Add(collider);
                var bounds = collider.bounds;
                _extent = Mathf.Max(_extent, (bounds.center - player.position).magnitude + bounds.extents.magnitude);
            }
            _extent += 0.5f; // the body may move and animate a little between frames
        }
    }
}
