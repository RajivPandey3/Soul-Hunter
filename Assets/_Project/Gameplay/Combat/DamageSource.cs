namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Which weapon is dealing the hit being applied, for the VS Results screen's per-weapon damage
    /// (vampire.survivors.wiki/w/Results). Hits are applied synchronously, so a weapon, projectile or zone opens a scope
    /// around its <c>TakeDamage</c> call and <see cref="HealthController"/> credits the damage an enemy takes to it.
    /// A hit outside any scope (enemy abilities, stage hazards) is credited to nothing.
    /// </summary>
    public static class DamageSource
    {
        /// <summary>The weapon dealing the current hit, or null.</summary>
        public static AutoAttackWeapon Weapon { get; private set; }
        /// <summary>Name of a damage source that is not a held weapon (Soul Burst, a rail cart), or null.</summary>
        public static string Name { get; private set; }

        public static bool Any => Weapon != null || !string.IsNullOrEmpty(Name);

        /// <summary>Opens a scope; dispose it (a <c>using</c> block) right after the hit.</summary>
        public static Scope From(AutoAttackWeapon weapon, string name = null) => new Scope(weapon, name);

        public readonly struct Scope : System.IDisposable
        {
            private readonly AutoAttackWeapon _previousWeapon;
            private readonly string _previousName;

            public Scope(AutoAttackWeapon weapon, string name)
            {
                _previousWeapon = Weapon;
                _previousName = Name;
                Weapon = weapon;
                Name = weapon != null ? null : name;
            }

            public void Dispose()
            {
                Weapon = _previousWeapon;
                Name = _previousName;
            }
        }
    }
}
