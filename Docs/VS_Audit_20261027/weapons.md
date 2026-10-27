# VS audit: weapons, evolutions, unions, Limit Break (2026-10-27)

Source: raw wiki pages `https://vampire.survivors.wiki/w/<Page>?action=raw`, fetched 2026-10-27 through WebFetch. WebFetch returns a summary, not the raw text, so the "quotes" below are close paraphrases of what the tool gave back. SH paths are relative to `Assets/_Project/Gameplay/Weapons/`, except where a full path is given. Read-only audit: no project files were changed.

Aiming: owner decision 2026-09-24 (everything aims at the nearest enemy) is being reconsidered, so every weapon whose VS aim differs is listed as a DIFF and marked [AIM].

| Item | Rule | Wiki (page + short quote) | Soul Hunter now (file:line) | Status | What to change |
|---|---|---|---|---|---|
| Whip | L1 stats | Whip: dmg 10, CD 1.35, interval 0.1, KB 1, crit 20%/2x (Slash only), rarity 100, max 8 | WhipWeapon.cs:27-29,82; Whip_Weapon.prefab `_cooldown 1.35 _damage 10`; Gameplay/Combat/UpgradeRarity.cs:35 | MATCH | - |
| Whip | Level table | "L2 +1 projectile; L3 +5; L4 Area +10% +5; L5 +5; L6 Area +10% +5; L7 +5; L8 +5" | WhipWeapon.cs:73-79 | MATCH | - |
| Whip | Aim | "slash ... starts from the torso on the side they face"; extra whips "alternate toward and away from facing, each one progressively higher" | WhipWeapon.cs:88 AimDirection (nearest); alternates sides, no height offset | DIFF [AIM] | Use the facing side; offset each extra lash upward |
| Whip | Pierce | "Pierce: Area of Effect" | WhipWeapon.cs:55 buffer of 50 colliders per lash | DIFF (minor) | Grow the buffer like Garlic (CMB-07) |
| Magic Wand | L1 + table + aim | Magic_Wand: 10 dmg, 1.2 s, interval 0.1, pierce 1, KB 1, rarity 100; L2/4/6 +1, L3 CD -0.2, L5/L8 +10, L7 pierce +1; "fired at the closest enemy" | MagicWandWeapon.cs:16-42 | MATCH | - |
| Knife | L1 stats | Knife: 6.5 dmg, 1.0 s, interval 0.1, pierce 1, KB 0.5, crit 30%/3x (Slash), rarity 100, max 8 | KnifeWeapon.cs:9-19,49 | MATCH | - |
| Knife | Level table (interval) | "L4 +1 projectile, interval reduced by 0.02 s; L6 +1, interval -0.02; L8 +1 pierce, interval -0.02" | KnifeWeapon.cs:49 `VsInterval = 0.1f` is a constant; no interval step | DIFF | Interval 0.1 → 0.08 (L4) → 0.06 (L6) → 0.04 (L8) |
| Knife | Aim | "fly in the direction the player is moving or last moved" | KnifeWeapon.cs:58-62 AimDirection (nearest); also a random ±10° spread (KnifeShot) not on the wiki | DIFF [AIM] | Movement direction; drop the random spread or record it as SH's own |
| Axe | L1 + table | Axe: 20 dmg, 4.0 s, interval 0.2, pierce 3, KB 1, crit 30%/2x Slash, rarity 100; L2/L5 +1, L3/L6/L8 +20, L4/L7 pierce +2; "Ignores Duration" | AxeWeapon.cs:11-68; Axe_Weapon.prefab | MATCH | - |
| Axe | Aim | "first axe is thrown directly above the character", later ones "towards the direction the player is facing in progressively wider arcs"; "Speed controls how high axes travel" | AxeWeapon.cs:90-150 flat ground throw toward the nearest enemy, gravity off | DIFF [AIM] | Up-screen throw plus fan to the facing side |
| Cross | L1 + table + aim | Cross: 5 dmg, 2.0 s, interval 0.1, AoE pierce, KB 1, rarity 80; L2/5/8 +10, L3/6 Area +10% Speed +25%, L4/7 +1; "Aims at nearest enemy, has boomerang effect" | CrossWeapon.cs:12,30-81,130-142; Cross_Weapon.prefab | MATCH | - |
| King Bible | L1 + table + cycle | King_Bible: 10 dmg, CD 3, Duration 3, hitbox delay 1.7, KB 1, rarity 80; L2/5/8 +1, L3/6 Area +25% Speed +30%, L4/7 +0.5 s +10; "full cycle is about 6 seconds" | BibleWeapon.cs:12-101,157 | MATCH | - |
| King Bible | Direction | "orbits the player, rotating clockwise" | not checked | NOT CHECKED | Check the orbit direction |
| Fire Wand | L1 + table | Fire_Wand: 20 dmg, 3.0 s, 3 shots, Speed 75%, interval 0.02, pierce 1, KB 1, rarity 80; +10 every level; Speed +20% at L3/5/7 | FireWandWeapon.cs:19-27; base speed is SH's own `ProjectileSpeed = 5f` (:16) | MATCH (numbers) | The VS base Speed of 75% is not expressed; record the mapping |
| Fire Wand | Aim | "shoots an arc of fireballs in the direction of a random enemy" | FireWandWeapon.cs:70-75 nearest enemy, 8° fan | DIFF [AIM] | Random enemy |
| Garlic | L1 + table | Garlic: 5 dmg, 1.3 s, KB 0, rarity 70; L2 Area +40% +2; L3/7 CD -0.1 +1; L4/6 Area +20% +1; L5 CD -0.1 +2; L8 Area +20% +2 | GarlicWeapon.cs:12,117-129 | MATCH | - |
| Garlic | Hit rule | "An enemy takes damage instantly when it first enters the aura. It cannot be hit again until the Cooldown ends" | GarlicWeapon.cs:132-160 pulses the whole radius every cooldown, so a new enemy waits for the next pulse | DIFF | Keep a cooldown per enemy; hit on entry |
| Garlic | Debuffs | "+0.3 knockback ... -0.1 freeze resistance" | GarlicWeapon.cs:164-175 | MATCH | - |
| Santa Water | L1 + table | Santa_Water: 10 dmg, 4.5 s, 1, Duration 2.0, interval 0.3, delay 0.5, KB N/A, rarity 100; L2/4/6 +1 Area +20%; L3 +0.5 s +10; L5 +0.25 s +10; L7 +0.25 s +5; L8 Area +20% +5 | SantaWaterWeapon.cs:15-45,82 | MATCH | - |
| Santa Water | Targeting | "Until total Amount reaches four, the first bottle ... targets the closest enemy. After that, bottles fall in a roughly circular, clockwise pattern" | SantaWaterWeapon.cs (Attack) first on the nearest enemy, the rest random within 1.5 units of it | DIFF | Clockwise ring placement for bottles after the first, and for all bottles at Amount 4+ |
| Runetracer | L1 + table | Runetracer: 10 dmg, 3.0 s, 1, Duration 2.25, interval 0.2, delay 0.5, KB 1, rarity 80; L2/5 +5 Speed +20%; L3/6 +0.3 s (real 0.25) +5; L4/7 +1; L8 +0.5 s | RunetracerWeapon.cs BaseDuration adds 0.3 at L3 and L6 | DIFF (minor) | L3/L6 should add 0.25 ("the first two Duration increases actually add 0.25 seconds") |
| Runetracer | Aim | "fires its runes in random directions" | RunetracerWeapon.cs AimDirection(RandomFlatDirection()) = nearest, 15° fan | DIFF [AIM] | Random direction |
| Lightning Ring | L1 + table | Lightning_Ring: 15 dmg, 4.5 s, 2, interval 0.05, AoE, KB 1, rarity 80; L2/4/6/8 +1; L3 Area +100% +10; L5/L7 Area +100% +20 | LightningRingWeapon.cs:20-36 | MATCH | - |
| Lightning Ring | Targeting | "strikes random enemies from the sky" | LightningRingWeapon.cs:75-85 nearest first | DIFF [AIM] | Random enemies |
| Pentagram | L1 + table + floor | Pentagram: CD 90, duration 1 s, keep chance 10%, rarity 60; L2/4 -10 s; L6/8 -5 s; L3/5/7 25/45/65%; 15 s minimum | PentagramWeapon.cs (CooldownForLevel, KeepChance, VsMinimumCooldown) | MATCH | - |
| Peachone | L1 + table | Peachone: 10 dmg, Speed 80%, 4, Dur 4.0, CD 1.0, interval 0.025, KB 2, rarity 50; +1 every level; L2/5/8 Area +40%; L3/6 +10; L4/7 CD -0.25 | PeachoneWeapon.cs (BombCount, LevelArea, CooldownForLevel) | MATCH | - |
| Peachone | Behaviour + union | "bombards a zone that circles clockwise"; Vandalier needs Ebony Wings at max | PeachoneWeapon.cs orbit; Union_TwilightFlock.asset (A/B max level 8) | MATCH | - |
| Ebony Wings (Dusk Raven) | Stats + behaviour | per the SH code comment: Peachone's numbers, "Area orbits counterclockwise" | DuskRavenWeapon.cs:6-26 | MATCH (code comment; page not re-fetched) | - |
| Phiera Der Tuphello (Gun) | L1 + table | Phiera: 5 dmg, 1.4 s, interval 0.2, pierce 1, KB 0.1, rarity 50; L2/4 +1; L3/6/8 pierce +2; L5 +5; L7 +5 Speed +50% | GunWeapon.cs (BaseDamage, BulletsPerArm, Pierce, LevelSpeedMultiplier) | MATCH | - |
| Phiera Der Tuphello (Gun) | Aim | "fires ... in the four ordinal (diagonal) directions"; "Amount adds one bullet to each of the four streams" | GunWeapon.cs one arm turned toward the nearest enemy, plus a ±5° random wobble | DIFF [AIM] | Fixed diagonals; no wobble |
| Eight The Sparrow (Azure Repeater) | Stats + aim | per the SH comment: the same numbers as Phiera | AzureRepeaterWeapon.cs:6-22 (subclass of GunWeapon) | MATCH (stats) / DIFF [AIM] | As Gun |
| Song of Mana | Level table | Song_of_Mana: 10 dmg, 2.0 s, Dur 0.5, interval 0.05, delay 1.0, AoE, rarity 50; L2 Area +100%; L3/5/7 +0.5 s and CD +0.8 ("each cooldown increase actually adds 0.75"); L4/6/8 Area +25% +10 | SongOfManaWeapon.cs `VsCooldownStep = 0.8f` | DIFF (minor) | Cooldown step 0.75 |
| Song of Mana | Behaviour | "Attacks vertically, passes through enemies"; a vertical column centred on the character that follows it; 1 s hitbox delay | SongOfManaWeapon.cs:6-9, Attack(): one flying shot at the nearest enemy, pierce 5, 1 s base lifetime, no hitbox delay; holds fire with no enemy | DIFF [AIM] (behaviour) | Rebuild as a vertical column following the hero: AoE, delay 1.0, Duration 0.5 |
| Clock Lancet | L1 + table | Clock_Lancet: CD 2.0, Dur 2.0, rarity 70, max 7; L2/4/5/7 +1 s; L3/6 CD -0.5 | ClockLancetWeapon.cs (FreezeDuration, CooldownForLevel, MaxLevel 7) | MATCH | - |
| Clock Lancet | Aim / chance | "chance to freeze"; "12 fixed directions, one at a time ... clockwise" | ClockLancetWeapon.cs:48 aims at the nearest enemy; `VsFreezeChance = 1f` | DIFF [AIM]; chance UNKNOWN (the wiki gives no number) | Clockwise 12-direction sweep |
| Laurel | L1 + table | Laurel: CD 10, rarity 60, max 7; L2/3/5/6 CD -0.5 invuln +0.2; L4/7 +1 charge | LaurelWeapon.cs (TimingUpgrades, MaxCharges) | MATCH | - |
| Bone | L1 + table | Bone: 5 dmg, 3.0 s, Area 120%, Speed 75%, Dur 2.0, KB 1, rarity 1; L2 Area +20% +0.2 s; L3/5 +1 +20; L4 Area +20% Speed +50%; L6 +0.2 s; L7 +20; L8 +0.2 s Speed +50% | BoneWeapon.cs (BaseDamage, BonesPerAttack, BaseDuration, LevelArea/SpeedMultiplier) | MATCH (levels) | The base Area of 120% and Speed of 75% are not expressed; check the prefab |
| Bone | Aim / bounce | "a small spinning bone in a random direction"; "bounces from terrain and enemies", and off screen edges | BoneWeapon.cs aims at the nearest enemy with a 15° fan; bounces not verified | DIFF [AIM] | Random direction; check the bounces |
| Cherry Bomb | L1 + table | Cherry_Bomb: 10 dmg, 3.0 s, 1, Dur 2.0, interval 0.3, chance 40%, rarity 1; L2/7 Area +25% Speed +30%; L3 50%; L4 +1; L5/8 Area +25% +5; L6 60% | CherryBombWeapon.cs (BaseDamage, BombCount, LevelArea, LevelSpeed, ExplodeChance) | MATCH | - |
| Cherry Bomb | Behaviour | "bouncing off enemies, walls, and edges of the screen ... then decelerates and eventually stops" | CherryBombWeapon.cs:101 only Arcana bounces; no deceleration (AGENTS: "not built") | DIFF | Bounces off enemies, walls and edges, plus deceleration |
| Bloody Tear | Stats | Bloody_Tear: 40 dmg, Area 130%, 2, 1.35 s, interval 0.1, crit 10%/2x, heal 8 on a crit hit | BloodyTearWeapon.cs:10-79 | MATCH (Area 1.3 not verified in the prefab) | Check Area 1.3 |
| Holy Wand | Stats | 30 dmg, 0.5 s, 4, interval 0.1, pierce 2 (owner fix 2026-10-19) | HolyWandWeapon.cs:19-28 | MATCH (page not re-fetched) | - |
| Thousand Edge | Stats | Thousand_Edge: 16.5 dmg, Speed 150%, 6, pierce 3, 0.35 s, interval 0.05, KB 0.5, crit 30%/3x | ThousandEdgeWeapon.cs:8-23,80 | MATCH | Speed 1.5x over Knife not verified |
| Thousand Edge | Aim | as Knife (movement direction) | nearest enemy | DIFF [AIM] | As Knife |
| Death Spiral | Stats | Death_Spiral: 60 dmg, Area 1.2, Speed 0.8, 9, pierce 1000, 4.0 s, interval 0.05 | DeathSpiralWeapon.cs:19-65 | DIFF (minor) | No Area 1.2 or Speed 0.8 found; scythes appear to leave together, not 0.05 s apart (check) |
| Death Spiral | Aim | "outward from the player in fixed directions" | DeathSpiralWeapon.cs:44 first scythe at the nearest enemy | DIFF [AIM] | Fixed directions |
| Heaven Sword | Stats | Heaven_Sword: 77 dmg, Area 1.2, Speed 2 ("+50%" over a maxed Cross), 1, 3.3 s, interval 0.5, KB 6, crit 10%/2.5x | Prefabs/Weapons/Generated/HeavenSword.prefab `_speed 16` (Cross 12); CrossWeapon.cs:142 interval const 0.1 | DIFF | Speed 16/12 = 1.33x where the wiki says 2x (24); Area 1.2 missing; interval 0.5 |
| Unholy Vespers | Stats | Unholy_Vespers: 30 dmg, 4, Area 1.75, Speed 1.5, Dur 3.0, CD 3.0, delay 1.7, KB 4 | UnholyVespers.prefab `_bibleCount 4 _radius 3.5 _rotationSpeed 240 _damage 30 _knockback 4` | DIFF | Radius 3.5/3 = 1.17x (wiki 1.75x); rotation 240/180 = 1.33x (wiki 1.5x) |
| Soul Eater | Stats | Soul_Eater: 20 dmg, Area 300%, CD 1.0, Dur 1.3, KB 0, +1 dmg per 60 healed (cap +60) | GarlicWeapon.cs:13,43-49 | MATCH | - |
| Soul Eater | Healing | "Killing enemies can drop Little Hearts, which heal 1 × Recovery" | GarlicWeapon.cs:35,189-191 life steal per enemy hit (SH's own) | DIFF | Little Heart drops on kill |
| Soul Eater | Hit rule | as Garlic (on entry, then cooldown) | pulses | DIFF | As Garlic |
| Hellfire (Hellmaw Comet) | Stats | Hellfire: 100 dmg, 3.0 s, 2, Speed 1, Dur 0.1, interval 0.2, AoE | HellmawCometWeapon.cs:18-26 | MATCH | - |
| Hellfire (Hellmaw Comet) | Aim | "meteors are aimed randomly" | HellmawCometWeapon.cs:49 AimDirection(Random) = nearest | DIFF [AIM] | Random |
| La Borra (Drowning Chalice) | Stats | La_Borra: 40 dmg, CD 4.0, 4, Area 200%, Dur 4.0, interval 0.3, delay 0.5, KB none | DrowningChaliceWeapon.cs:16-30,72 | MATCH | - |
| La Borra (Drowning Chalice) | Growth | "Duration slows growth, so puddles reach full size as they expire"; scales with "stat bonuses and Move Speed" | DrowningChaliceWeapon.cs:29-30 grows only with Speed bonuses, at most 2x | DIFF | Growth over the puddle's Duration, scaled by stats and Move Speed |
| NO FUTURE (Severed Fate) | Stats | NO_FUTURE: 30 dmg, Speed 2.8, 1, Dur 3.0, CD 1.0, delay 0.5; retaliation every 0.5 s, +10% per Armor | SeveredFateWeapon.cs:21-28 | MATCH | - |
| NO FUTURE (Severed Fate) | Aim | "ricocheting cyan laser in random directions" | nearest enemy | DIFF [AIM] | Random |
| Thunder Loop (Stormbound Halo) | Stats | Thunder_Loop: 65 dmg, 4.5 s, 6, Area 4, interval 0; each spot struck again | StormboundHaloWeapon.cs:22-26 (repeat 0.5 s, SH's number) | MATCH | - |
| Thunder Loop (Stormbound Halo) | Targeting | "Strikes hit random enemies ... each one must target a different enemy" | nearest enemy | DIFF [AIM] | Random |
| Gorgeous Moon (Eclipse Sigil) | Stats | page not re-fetched | EclipseSigilWeapon.cs:26-29 | NOT RE-CHECKED | - |
| Mannajja (Requiem of Souls) | Stats | Mannajja: 40 dmg, Area 6, Dur 2.0, CD 4.5, interval 0.1, delay 1.0, slow 5% per hit to 20% | RequiemOfSoulsWeapon.cs:22-29 | MATCH | - |
| Mannajja (Requiem of Souls) | Shape | "five concentric rings, forming a vertical column around the player" | circular wave, radius 6 × Area | DIFF (minor) | Vertical column shape |
| Infinite Corridor (Sundered Hour) | Stats + rule | Infinite_Corridor: CD 1.0, Dur 6.0; Clock Lancet + max Silver Ring and Gold Ring; "12 clockwise directions"; at 12 o'clock "halves the health of every enemy on screen" | SunderedHourWeapon.cs:15-131; Evo_SunderedHour.asset (rings at level 9) | MATCH (rules) / DIFF [AIM] | Clockwise sweep in place of the nearest enemy |
| Crimson Shroud | Stats | Crimson_Shroud: Laurel + max Metaglio L/R; CD 8, interval 0.1, 3 charges, cap 10, Area 2; Amount adds explosions | CrimsonShroudWeapon.cs:20 | MATCH (code comment; not re-verified line by line) | - |
| Vandalier (Twilight Flock) | Stats | Vandalier: 28 dmg, Area 2.2, Speed 1, 20, Dur 4.0, CD 1.0, interval 0.025, KB 2; two zones in opposite directions | TwilightFlockWeapon.cs:26-42 | MATCH | - |
| Phieraggi (Revenant Wheel) | Stats | Phieraggi: 15 (+1 per Revival, max +10), 4 (+1 per Revival), pierce 7, CD 1.4, interval 0.1, delay 0.2, KB 0.05; needs Eight The Sparrow at max + Tirajisú | RevenantWheelWeapon.cs:32-49; Union_RevenantWheel.asset | MATCH | - |
| Evolution recipes | Base max + passive | Weapons page: Whip+Hollow Heart, Wand+Empty Tome, Knife+Bracer, Axe+Candelabrador, Cross+Clover, Bible+Spellbinder, Fire Wand+Spinach, Garlic+Pummarola, Santa Water+Attractorb, Runetracer+Armor, Lightning+Duplicator, Pentagram+Crown, Song+Skull O'Maniac, Clock Lancet+rings, Laurel+Metaglios | 14 Evo_*.asset (base max level 8, or 7 for Clock Lancet) | MATCH (enum ids not decoded one by one) | Decode the RequiredPassive ints for a full check |
| Rarity | All base weapons | infobox rarity | Gameplay/Combat/UpgradeRarity.cs:34-52 | MATCH (Whip 100, Wand 100, Knife 100, Axe 100, Cross 80, Bible 80, Fire 80, Garlic 70, Santa 100, Rune 80, Lightning 80, Penta 60, Peach 50, Gun 50, Song 50, Clock 70, Laurel 60, Bone 1, Cherry 1) | - |
| Limit Break | Unlock | Limit_Break: "It is unlocked by obtaining the Great Gospel" (a toggle) | a run mode, open from the start (AGENTS P3-05) | DIFF | Gate behind a Great Gospel counterpart (relic) |
| Limit Break | Rows and caps | general table: Might +0.3/0.5/1% (r10), Area +1/2/2.5% (r5; the Whip page says r10), Speed +5% (r10), Duration +0.1 s (r1), Amount +1 (r1, cap 5-30), Pierce +1 (r5, 10), Crit +1/2.5% (r5), Chance +0.5/5% | Gameplay/Combat/LimitBreak.cs:40-110 | DIFF (partly) | Area rarity is 10 for every weapon (the general table says 5; the wiki conflicts with itself); Area cap is a single 1000% while the wiki caps vary (400/500/1000%) per weapon; Amount cap is a single value while the wiki gives 5-30 per weapon |
| Limit Break | Evolve reset | "Evolving a Limit Broken weapon resets its extra stats ... starts from level 1" | not checked | NOT CHECKED | - |
| Limit Break | Stage items | "Collecting stage items pauses Limit Break leveling" | not checked | NOT CHECKED | - |
| Limit Break | Chests | "Treasure Chests are unaffected and still give Big Coin Bags" | not checked | NOT CHECKED | - |

## Missing content (VS weapons not in Soul Hunter)

Base game (from the Weapons page; stats from each page where fetched):
- **Gatti Amari**: 10 dmg, CD 5.0, 1, Dur 5.0, rarity 50, max 8; cats claw enemies, start scuffles and eat pickups. → **Vicious Hunger** (Stone Mask; SH has it as Miser's Idol).
- **Shadow Pinion**: 10 dmg, CD 6.0, 1, Dur 1.0, rarity 50, max 8; drops drills behind the hero while moving and fires them forward on stopping. → **Valkyrie Turner** (Wings).
- **Vento Sacro**: 2 dmg (6 at max, +40 flat from nonstop walking), CD 2.0, 4, Dur 0.4, rarity 50, max 8; slashes ahead, 5% chance of a 2x crit. Union with Bloody Tear → **Fuwalafuwaloo**.
- **Carréllo**: 10 dmg, CD 5.5, 2, rarity 1, max 8; a bouncing cart that explodes. → **Carrozza!** (Chaos Lazulia + Bianca Ramba Lv80).
- **Celestial Dusting**: 5 dmg, CD 6.0 (faster while moving), 1, Dur 0.5, rarity 1; bouncing flowers, unlimited pierce. → **Profusione D'Amore**.
- **La Robba**: 10 dmg, CD 4.5, 3, Dur 2.0, rarity 1; bouncing furniture from the top of the screen; no evolution.
- **Greatest Jubilee**: 10 dmg, CD 3.0, 1, Dur 0.1, rarity 20, max 9; fireworks, 1% chance of 4 light sources; no evolution.
- **Bracelet**: 10 dmg, CD 1.4, 3, Dur 0.4, rarity 40, max 6; three shots at a random enemy. → **Bi-Bracelet** → **Tri-Bracelet** (no passive).
- **Candybox**: item selector (page not fetched). → Super Candybox II Turbo.
- **Victory Sword**: 5 dmg, CD 1.85, 2, rarity 30, max 12; combo on the nearest enemy plus retaliation, crits from L8 (Queen Sigma). → **Sole Solution** (Torrona's Box = SH Cursed Coffer).
- **Flames of Misspell**: 10 dmg, 12 shots, CD 4.0, Dur 0.4, rarity 30, max 8; fire cones in the last move direction. → **Ashes of Muspell** (Torrona's Box).
- Not fetched individually: **Pako Battiliar** → Mazo Familiar (Hollow Heart); **Ammo Appalate** → Gunastrophe (Bracer); **Unearthly Bolt** → Spirit Disturbance (Tirajisú); **Glass Fandango** → Celestial Voulge (Wings); **Penshin Fatcha** → Miracle of Multiplication; **Tonno Subito, Tonnado, Tonn'omoto, Tonn'oddeeo, Tonne**; **Unsurpassed**; **Santa Javelin** → Seraphic Cry (Clover); **Gaze of Gaea** → Embrace of Gaea (Parm Aegis); **Magi-Stone** → Kyra-Stones (Karoma's Mana); **Phas3r** → Photonstorm (Empty Tome); **Chaos Rune** → Wicked Ruler (Spellbinder); **Arma Dio**.
- Evolutions of weapons SH already has: **Anima of Mortaccio** (Bone; Chaos Malachite + Mortaccio Lv80) and **Yatta Daikarin** (Cherry Bomb; Chaos Rosalia + Yatta Cavallo Lv80).
- Group names from the Weapons page's "Belongs To" column: **Gold Finger**, **Super Candybox II Turbo**.

DLC weapons (whole families; none are in SH):
- **Legacy of the Moonspell:** Silver Wind → Festive Winds → Argent Flow; Four Seasons → Godai Shuffle; Summon Night → Echo Night; Mirage Robe → J'Odore; Night Sword → Muramasa; Mille Bolle Blu → Boo Roo Boolle; 108 Bocce; LunarMight/Sight/Bight → LunarFlight.
- **Tides of the Foscari:** SpellString/Stream/Strike → SpellStrom; Eskizzibur → Legionnaire; Flash Arrow → Millionaire; Prismatic Missile → Luminaire; Shadow Servant → Ophion; Party Popper.
- **"bm" DLC:** Scarlet Needle → Cardinal Rain; Ashella → Bloodlust; Incineration → Dust to Dust; Screams from the Void → Forbidden Siren; Blacken Firmament → Damnation; 108 Responsive Prayers; FireFall/Ball/Wall → FireStall; Descent Into Misery.
- **Emergency Meeting:** 8 weapons with Mini-crewmate evolutions.
- **Operation Guns:** 11 weapons with Prototype evolutions.
- **Ode to Castlevania:** about 80 weapons.
- **Emerald Diorama:** 17 weapons.
- **"ac" DLC:** Infernolatro, Gros Michel, Fibonacci Spritz, Celestial Booster.
- **DLC unions on base evolutions SH has:** Death Spiral → Legacy of Death: Soul River (Dark Rift); Unholy Vespers → Ninth Circle (Icebrand); Mannajja → Dies Irae (Arrow of Goth); NO FUTURE → Carnage Heart (Troll Bomb); La Borra → Hydro Pump Climax (Hydro Storm).

## Not checked
- The Weapons page past character 100,000 (later DLC evolution recipes).
- Individual pages for Holy Wand, Gorgeous Moon, Ebony Wings, Eight The Sparrow and Candybox (code comments trusted).
- Prefab-level Area and Speed for Bloody Tear, Thousand Edge, Death Spiral and Bone.
- King Bible's orbit direction and Bone's bounces.
- The Evo_* RequiredPassive enum ids, one by one.
- The Limit Break reset, stage-item pause and chest rules.
- VS per-weapon "pool" limits: I found nothing in SH that models them.
- Crit and knockback rows for evolutions other than those listed.
