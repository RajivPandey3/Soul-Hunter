using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Switches dead horde bodies off a few a frame (2026-10-29). A Lightning Ring strike that killed 116 enemies in one
    /// physics step spent 4.56 ms in their SetActive(false) (`run-20261029-physjolt`, a 30 ms frame); about 28 µs each,
    /// spread over the collider, the skinned renderer, EnemyController's OnDisable and the rest (death bench).
    ///
    /// The VS wiki and poncle's posts say nothing on how many deaths a frame VS handles (`Docs/VS_Audit_20261027/
    /// performance-public-sources.md`), so this is the balance method: what a player sees stays in the death's frame.
    /// The kill, its gem, drops, sound, puff, blood and sinking body all happen at once (HealthController.OnDied), and the
    /// body leaves the screen at once (it leaves the live list, and the GPU horde drawer draws only that list). Only its
    /// switch-off waits: a body that died in an earlier frame is switched off here, at most <see cref="PerFrame"/> a frame.
    /// Until then nothing hits it (DamageableLookup), its contact damage is off and nothing steps, aims at or pushes it.
    /// </summary>
    public static class EnemyDeathQueue
    {
        /// <summary>Off: every body is switched off in its death's frame, as before (tests, A/B).</summary>
        public static bool Enabled = true;
        /// <summary>Bodies switched off per frame (40 is about 1.1 ms on the reference laptop).</summary>
        public static int PerFrame = 40;

        private struct Entry { public EnemyController Enemy; public int Frame; }
        private static readonly Queue<Entry> Pending = new Queue<Entry>(256);

        /// <summary>Dead bodies still waiting to be switched off.</summary>
        public static int Count => Pending.Count;

        /// <summary>Takes the body's switch-off if it can wait; false means the caller switches it off now.</summary>
        public static bool TryDefer(EnemyController enemy)
        {
            if (!Enabled || !Application.isPlaying || enemy == null || !enemy.CanFinishDyingLater()) return false;
            enemy.BeginDying();
            Pending.Enqueue(new Entry { Enemy = enemy, Frame = Time.frameCount });
            EnemyRunner.Ensure();
            return true;
        }

        /// <summary>Switches off the bodies of earlier frames, at most <see cref="PerFrame"/> (EnemyRunner.Update).</summary>
        internal static void Drain()
        {
            int frame = Time.frameCount;
            for (int done = 0; done < PerFrame && Pending.Count > 0; )
            {
                var next = Pending.Peek();
                if (next.Frame >= frame) break; // this frame's deaths wait for the next frame
                Pending.Dequeue();
                if (SwitchOff(next.Enemy)) done++;
            }
        }

        /// <summary>Switches every waiting body off now (stage start and clear, tests).</summary>
        public static void Flush()
        {
            while (Pending.Count > 0) SwitchOff(Pending.Dequeue().Enemy);
        }

        private static bool SwitchOff(EnemyController enemy)
        {
            if (enemy == null || !enemy.IsDying) return false; // destroyed, or already switched off elsewhere
            enemy.gameObject.SetActive(false);
            return true;
        }

        // Domain reload may be off: the last play session's bodies must not stay queued.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Pending.Clear(); Enabled = true; PerFrame = 40; }
    }
}
