using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// A straight rail with its cart (GameContentCatalog.RailCart, PLACEHOLDER art from Unity primitives built by
    /// StageHazardBuilder). The rail runs along the track's local X; the cart waits at one end.
    /// </summary>
    public class RailCartTrack : MonoBehaviour
    {
        public const float Length = 24f;
        /// <summary>How far from the centre the cart stops at either end (half its body inside the rail).</summary>
        public const float HalfTravel = Length * 0.5f - 1.2f;

        public RailCart Cart => GetComponentInChildren<RailCart>(true);

        public Vector3 EndA => transform.TransformPoint(new Vector3(-HalfTravel, 0f, 0f));
        public Vector3 EndB => transform.TransformPoint(new Vector3(HalfTravel, 0f, 0f));
    }
}
