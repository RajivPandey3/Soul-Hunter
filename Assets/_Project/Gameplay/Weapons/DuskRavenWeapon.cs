using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Dusk Raven: Soul Hunter's second bird (own name, 2026-10-12; not yet owner-approved), built on VS Ebony Wings
    /// (vampire.survivors.wiki/w/Ebony_Wings, raw, fetched 2026-10-12). The infobox and level table are Peachone's own:
    /// 10 damage, 1.0 s cooldown, 4 projectiles, 4.0 s duration, 0.025 s interval, knockback 2, Speed 80%; "Level 2:
    /// Fires 1 more projectile. Base Area up by 40%" ... "Level 8", with the same footnote ("Each Cooldown reduction
    /// level up actually reduces Cooldown by 0.25 seconds"). The one difference: "Area orbits counterclockwise around the
    /// player". So this is the Peachone script circling the other way, starting on the far side of the hero so the two
    /// birds do not begin on top of each other (Soul Hunter's choice; the wiki gives no starting place).
    /// Its union with Peachone is Twilight Flock (TwilightFlockWeapon). Arcanas: the page lists the same three as
    /// Peachone's (Gemini, Twilight Requiem, Pale Diamond Incursion), which it gets by being a PeachoneWeapon.
    /// </summary>
    public class DuskRavenWeapon : PeachoneWeapon
    {
        /// <summary>Counterclockwise seen from the game camera (a rising angle), at Peachone's pace.</summary>
        protected override float OrbitDegreesPerSecond => 50f;
        protected override string ResultsName => "Dusk Raven";

        protected override void Awake()
        {
            base.Awake();
            SetOrbit(CircleRadius, 180f);
        }
    }
}
