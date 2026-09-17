# Magic Wand behavior inventory

Reference snapshot: stable PC 1.16.107. Individual weapon behavior source: https://vampire-survivors.fandom.com/wiki/Magic_Wand . Community documentation is supporting evidence, not a captured reference-game replay.

Implemented baseline: nearest active enemy; eight levels; sequential projectiles; base damage 10 to 30, amount 1 to 4, cooldown 1.2 to 1.0 seconds, pierce 1 to 2; 0.1-second shot spacing; 60 active missiles; solid obstacle blocking. Level increments: amount at 2/4/6, cooldown at 3, damage at 5/8, pierce at 7.

Player Might, Amount, Cooldown, Area and ProjectileSpeed are applied. Duration does not extend missile lifetime. Project-specific range (15 world units), travel speed (20 units/second) and cleanup lifetime (3 seconds) are retained adaptations; they have not been calibrated against reference screen-space movement.

Waltz of Pearls enables three bounces. The former Magic Wand Gemini duplicate was incorrect and has been removed; Iron Blue Will does not grant it bounces. Full Gemini compatibility for the rest of the roster is a separate open gate.

Open: freeze-related Arcanas, Limit Break, all evolution interactions, exact range/speed/hitbox/knockback calibration, high-speed collision tunnelling, dense-horde frame-time/GC evidence, and reference-game replay comparisons. Full DOT parity is NOT passed.

Tests: VerifyMagicWandLevels, VerifyMagicWandContacts, VerifyMagicWandCapacity, VerifyMagicWandRuntime, plus VerifyProjectileAuthority regression. See corresponding Tools scripts and Logs reports.
