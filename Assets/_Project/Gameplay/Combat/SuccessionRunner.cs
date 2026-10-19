using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>One shot of a volley fired one after another: its index, the volley's count, and what the volley fixed.</summary>
    public delegate void SuccessionShot(int index, int count, Vector3 arg);

    /// <summary>
    /// CMB-13 (2026-10-19): fires the later shots of every weapon's volley (AutoAttackWeapon.FireInSuccession) from one
    /// LateUpdate, in place of one coroutine, iterator and WaitForSeconds per extra shot on every attack. Timing is the
    /// coroutine's: each shot waits its interval of game time from the frame the last one left (a pause holds it, as
    /// WaitForSeconds did), at most one shot of a volley per frame, after every Update. A volley ends when its weapon is
    /// switched off or destroyed. Created on first use, one per loaded game.
    /// </summary>
    public sealed class SuccessionRunner : MonoBehaviour
    {
        private struct Volley
        {
            public AutoAttackWeapon Weapon;
            public SuccessionShot Fire;
            public int Count;
            public int Next;
            public float Interval;
            public float NextAt;
            public int LastFrame;
            public Vector3 Arg;
        }

        private static readonly List<Volley> Volleys = new List<Volley>();
        private static SuccessionRunner _instance;

        /// <summary>Volleys still firing (tests).</summary>
        public static int Pending => Volleys.Count;

        internal static void Queue(AutoAttackWeapon weapon, SuccessionShot fire, int count, float interval, Vector3 arg)
        {
            if (_instance == null)
            {
                var go = new GameObject("SuccessionRunner") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SuccessionRunner>();
            }
            Volleys.Add(new Volley
            {
                Weapon = weapon, Fire = fire, Count = count, Next = 1, Interval = interval,
                NextAt = Time.time + interval, LastFrame = Time.frameCount, Arg = arg,
            });
        }

        private void LateUpdate()
        {
            float now = Time.time;
            int frame = Time.frameCount;
            // Backwards: a finished volley is swap-removed with the last, which is already done this frame (or was
            // queued this frame and waits for a later one anyway).
            for (int i = Volleys.Count - 1; i >= 0; i--)
            {
                if (i >= Volleys.Count) continue;
                var volley = Volleys[i];
                if (volley.Weapon == null || !volley.Weapon.isActiveAndEnabled) { RemoveAt(i); continue; }
                if (volley.LastFrame == frame || now < volley.NextAt) continue;
                int index = volley.Next++;
                volley.LastFrame = frame;
                volley.NextAt = now + volley.Interval;
                bool done = volley.Next >= volley.Count;
                Volleys[i] = volley;
                try { volley.Fire(index, volley.Count, volley.Arg); }
                catch (System.Exception e) { Debug.LogException(e, volley.Weapon); done = true; }
                // A shot can only add volleys at the end, so this volley is still at i.
                if (done) RemoveAt(i);
            }
        }

        private static void RemoveAt(int i)
        {
            int last = Volleys.Count - 1;
            Volleys[i] = Volleys[last];
            Volleys.RemoveAt(last);
        }

        private void OnDestroy()
        {
            if (_instance == this) { _instance = null; Volleys.Clear(); }
        }

        // Domain reload may be off, so volleys of the last play session must not stay listed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Volleys.Clear();
            _instance = null;
        }
    }
}
