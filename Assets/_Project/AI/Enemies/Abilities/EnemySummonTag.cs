using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>Marks an enemy summoned by EnemyAbilities, so the summoner can count its living summons (and cap them).</summary>
    [DisallowMultipleComponent]
    public class EnemySummonTag : MonoBehaviour
    {
        public EnemyAbilities Owner { get; set; }
        internal object Runner { get; set; }

        private void OnDisable()
        {
            // Killed or returned to the pool: no longer counts. The tag stays on the pooled object with no owner, so a
            // reused enemy is an ordinary one again (and a same-frame reuse can take the tag over).
            if (Owner != null) Owner.OnSummonGone(this, Runner);
            Owner = null;
            Runner = null;
        }
    }
}
