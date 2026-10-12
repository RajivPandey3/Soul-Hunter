using UnityEngine;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// Owner (2026-10-12: "player agar kisi obstacle se takra ke ekdam se atak ja raha hai tum ise slippery banao"). The
    /// hero's velocity was set straight at its input every physics step, so walking into a gravestone or a house stopped
    /// it dead, and the default friction held it against the face. Now the walk slides: the part of the movement that
    /// points into an obstacle is dropped and the rest carries the hero along the face at no less than
    /// <see cref="SlideSpeedShare"/> of its speed, and a hero walking straight at a small obstacle steps round the side
    /// that is open. A long wall with no open side (the Castle corridor) still stops a hero walking straight at it.
    /// The wiki does not describe how the VS player meets obstacles; these numbers are Soul Hunter's own.
    /// </summary>
    public static class ObstacleSlide
    {
        /// <summary>Along an obstacle the hero keeps at least this share of its speed.</summary>
        public const float SlideSpeedShare = 0.7f;
        /// <summary>How far ahead of the hero's body an obstacle is felt (units), on top of this step's travel.</summary>
        public const float Skin = 0.12f;
        /// <summary>How far to each side a straight-on hero looks for the open way round (units).</summary>
        public const float SideProbe = 1.1f;
        /// <summary>How many side probes are tried: a face up to about 3 units to its nearer end is stepped round.</summary>
        public const int SideSteps = 3;
        /// <summary>Under this share of its speed along the face, the hero counts as walking straight at it.</summary>
        public const float StraightOnShare = 0.08f;

        private static PhysicsMaterial _slippery;

        /// <summary>A material without friction, so the physics step does not hold the hero against a face it slides on.</summary>
        public static PhysicsMaterial Slippery
        {
            get
            {
                if (_slippery == null)
                    _slippery = new PhysicsMaterial("HeroSlippery")
                    {
                        dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f,
                        frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum,
                    };
                return _slippery;
            }
        }

        /// <summary>
        /// The horizontal velocity to walk with: <paramref name="wanted"/> when the way is clear, else the slide along
        /// what is in the way.
        /// </summary>
        public static Vector3 Resolve(Vector3 position, Vector3 wanted, float radius, int obstacleMask, float deltaTime)
        {
            wanted.y = 0f;
            float speed = wanted.magnitude;
            if (speed < 0.01f || obstacleMask == 0) return wanted;
            Vector3 direction = wanted / speed;
            float probe = Mathf.Max(0.05f, radius - 0.03f);
            float reach = speed * deltaTime + Skin + 0.03f;
            if (!UnityEngine.Physics.SphereCast(position, probe, direction, out RaycastHit hit, reach, obstacleMask, QueryTriggerInteraction.Ignore))
                return wanted;

            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.0001f) return wanted; // a floor or a roof, not a face in the way
            normal.Normalize();
            float into = Vector3.Dot(wanted, normal);
            if (into >= 0f) return wanted; // already leaving it
            Vector3 along = wanted - normal * into;
            float alongSpeed = along.magnitude;
            Vector3 side = new Vector3(-normal.z, 0f, normal.x); // along the face

            Vector3 slide;
            if (alongSpeed >= speed * StraightOnShare)
            {
                // Glancing: keep going the way the hero already leans, without losing most of its speed.
                slide = along / alongSpeed * Mathf.Max(alongSpeed, speed * SlideSpeedShare);
            }
            else
            {
                // Straight at it: step round the nearer side that opens; a long wall has none and stops the hero.
                float left = OpenBeside(position, -side, direction, probe, reach, obstacleMask);
                float right = OpenBeside(position, side, direction, probe, reach, obstacleMask);
                if (float.IsInfinity(left) && float.IsInfinity(right)) return along;
                // The nearer way round; when both are as near, the side the face's hit point is not on.
                bool goRight = right < left || (Mathf.Approximately(right, left) && Vector3.Dot(side, hit.point - position) <= 0f);
                slide = (goRight ? side : -side) * (speed * SlideSpeedShare);
            }

            // An inner corner: the slide itself runs into a second face. Slide along that one if it does not push back
            // into the first, else stop.
            if (UnityEngine.Physics.SphereCast(position, probe, slide.normalized, out RaycastHit second, slide.magnitude * deltaTime + Skin, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 n2 = second.normal;
                n2.y = 0f;
                if (n2.sqrMagnitude > 0.0001f)
                {
                    n2.Normalize();
                    float into2 = Vector3.Dot(slide, n2);
                    if (into2 < 0f)
                    {
                        slide -= n2 * into2;
                        if (Vector3.Dot(slide, normal) < 0f) return Vector3.zero;
                    }
                }
            }
            return slide;
        }

        /// <summary>
        /// How far to that side (one, two or three <see cref="SideProbe"/>s) the hero could walk on in its direction;
        /// infinity when the way there is blocked or the face is longer than that (a wall).
        /// </summary>
        private static float OpenBeside(Vector3 position, Vector3 side, Vector3 direction, float probe, float reach, int mask)
        {
            for (int step = 1; step <= SideSteps; step++)
            {
                float distance = SideProbe * step;
                // The way to the side must itself be free, then the way ahead from there.
                if (UnityEngine.Physics.SphereCast(position, probe, side, out _, distance, mask, QueryTriggerInteraction.Ignore)) return float.PositiveInfinity;
                if (!UnityEngine.Physics.SphereCast(position + side * distance, probe, direction, out _, reach + 0.5f, mask, QueryTriggerInteraction.Ignore)) return distance;
            }
            return float.PositiveInfinity;
        }
    }
}
