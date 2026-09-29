using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>Gameplay feedback effects played by VFXPoolManager.Play.</summary>
    public enum VfxKind { HitSpark, EnemyDeath, GemPickup, GoldPickup, Heal, ChestOpen, LevelUp, Explosion, Slash, Blood }

    /// <summary>
    /// Learning Comment: Saare gameplay VFX prefabs ek jagah (Resources/VfxLibrary.asset). VfxBuilder (editor) inhe
    /// particle systems se banata hai; VFXPoolManager inhe pool karke chalata hai.
    /// </summary>
    [CreateAssetMenu(menuName = "Soul Hunter/VFX Library")]
    public sealed class VfxLibrary : ScriptableObject
    {
        public const string ResourcePath = "VfxLibrary";

        public GameObject HitSpark;
        public GameObject EnemyDeath;
        public GameObject GemPickup;
        public GameObject GoldPickup;
        public GameObject Heal;
        public GameObject ChestOpen;
        public GameObject LevelUp;
        public GameObject Explosion;
        public GameObject Slash;
        /// <summary>Blood on an enemy's death (VS has blood, switched off by its "Disable Blood" option).</summary>
        public GameObject Blood;

        public GameObject Get(VfxKind kind) => kind switch
        {
            VfxKind.HitSpark => HitSpark,
            VfxKind.EnemyDeath => EnemyDeath,
            VfxKind.GemPickup => GemPickup,
            VfxKind.GoldPickup => GoldPickup,
            VfxKind.Heal => Heal,
            VfxKind.ChestOpen => ChestOpen,
            VfxKind.LevelUp => LevelUp,
            VfxKind.Explosion => Explosion,
            VfxKind.Slash => Slash,
            VfxKind.Blood => Blood,
            _ => null,
        };
    }
}
