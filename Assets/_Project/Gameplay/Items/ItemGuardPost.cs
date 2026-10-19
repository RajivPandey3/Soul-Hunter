using UnityEngine;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Sits on a guarded stage item (both Metaglios and the two Rings) and sends its guardian (ItemGuardians) when the
    /// hero comes close. VS wiki (Passive_items, "Stage items"): the guard "spawns when any player gets close to the
    /// item. The player does not have to defeat the guard to pick up the item ... If the stage item was attracted using
    /// Mad Groove (VIII), the guard will not appear until the item is interacted with." So an item that Gathering Call
    /// drew away from its place sends its guardian only when it is taken. The guardian appears where the item was laid.
    /// </summary>
    public sealed class ItemGuardPost : MonoBehaviour
    {
        /// <summary>Further than this from where it was laid, the item counts as drawn away (Gathering Call).</summary>
        public const float MovedDistance = 1f;

        public ItemGuardians.Guard Guard { get; private set; }
        public int Level { get; private set; }
        public bool Spawned { get; private set; }
        /// <summary>The guardian's body once it has been sent (tests).</summary>
        public GameObject Body { get; private set; }
        public Vector3 Home => _home;

        private Vector3 _home;

        public static ItemGuardPost Attach(GameObject item, ItemGuardians.Guard guard, int level)
        {
            if (item == null) return null;
            var post = item.GetComponent<ItemGuardPost>();
            if (post == null) post = item.AddComponent<ItemGuardPost>();
            post.Guard = guard;
            post.Level = level;
            post._home = item.transform.position;
            post.Spawned = false;
            post.Body = null;
            return post;
        }

        private void Update()
        {
            if (Spawned) return;
            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 moved = transform.position - _home;
            moved.y = 0f;
            if (moved.sqrMagnitude > MovedDistance * MovedDistance) return; // drawn away: only taking it wakes the guardian
            Vector3 to = player.transform.position - _home;
            to.y = 0f;
            if (to.sqrMagnitude <= ItemGuardians.WakeDistance * ItemGuardians.WakeDistance) Spawn();
        }

        /// <summary>The item was taken: a guardian that has not come yet comes now.</summary>
        public void OnItemTaken() => Spawn();

        /// <summary>Sends the guardian now (also called by tests). Returns its body, or null when it could not be sent.</summary>
        public GameObject Spawn()
        {
            if (Spawned) return Body;
            var spawner = FindFirstObjectByType<SoulHunter.Gameplay.AI.EnemySpawner>();
            var prefab = ItemGuardians.Prefab(Level, Guard.Id);
            if (spawner == null || prefab == null) return null;
            // Like a stage boss, a guardian is unique (one per item, with its Soul Egg), so the 500 cap does not hold it back.
            var body = spawner.SpawnHazardEnemy(prefab, _home, ignoreHardCap: true);
            if (body == null) return null; // tried again on the next frame while the hero stays close
            Spawned = true;
            Body = body;
            ItemGuardian.Begin(body);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.BossWarningSound, 0.7f);
            return body;
        }
    }
}
