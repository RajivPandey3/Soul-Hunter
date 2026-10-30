using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>A ground pickup that PickupTicker updates once a frame instead of its own Update.</summary>
    public interface ITickedPickup
    {
        int TickerIndex { get; set; }
        void Tick();
    }

    /// <summary>
    /// Heavy-kill performance pass (2026-09-29): one loop updates every coin, chicken and other ground pickup. A strong
    /// hero leaves hundreds of them on the ground, and hundreds of separate Update calls cost over a millisecond a frame.
    /// </summary>
    public sealed class PickupTicker : MonoBehaviour
    {
        private static readonly List<ITickedPickup> Pickups = new List<ITickedPickup>();
        private static PickupTicker _instance;

        /// <summary>Pickups being updated now (tests).</summary>
        public static int Count => Pickups.Count;

        /// <summary>Copies every ticked pickup (coins, bags, chickens, magnets, freezes ...) into <paramref name="into"/>.</summary>
        public static void CopyTo(List<ITickedPickup> into)
        {
            into.Clear();
            into.AddRange(Pickups);
        }

        private static bool IsListed(ITickedPickup pickup)
        {
            int i = pickup.TickerIndex;
            return i >= 0 && i < Pickups.Count && ReferenceEquals(Pickups[i], pickup);
        }

        public static void Add(ITickedPickup pickup)
        {
            // The stored index is only trusted when the list agrees: a pooled copy can carry its template's index
            // (found 2026-09-29: every pooled floor chicken said 1 and none was ticked).
            if (IsListed(pickup)) return;
            pickup.TickerIndex = Pickups.Count;
            Pickups.Add(pickup);
            if (_instance == null && Application.isPlaying)
            {
                var go = new GameObject("PickupTicker") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<PickupTicker>();
            }
        }

        public static void Remove(ITickedPickup pickup)
        {
            int i = pickup.TickerIndex;
            pickup.TickerIndex = -1;
            if (i < 0 || i >= Pickups.Count || !ReferenceEquals(Pickups[i], pickup)) return;
            int last = Pickups.Count - 1;
            Pickups[i] = Pickups[last];
            Pickups[i].TickerIndex = i;
            Pickups.RemoveAt(last);
        }

        private void Update()
        {
            // Backwards: a pickup collected during its Tick is swap-removed, which only moves an already-ticked one.
            for (int i = Pickups.Count - 1; i >= 0; i--)
            {
                if (i >= Pickups.Count) continue;
                var pickup = Pickups[i];
                if (pickup is Object unityObject && unityObject == null)
                {
                    Remove(pickup); // destroyed without OnDisable (never expected)
                    continue;
                }
                try { pickup.Tick(); }
                catch (System.Exception e) { Debug.LogException(e, pickup as Object); } // one pickup's error never stops the rest
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // Domain reload may be off, so pickups from the last play session must not stay listed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Pickups.Clear();
            _instance = null;
        }
    }
}
