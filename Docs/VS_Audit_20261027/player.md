# VS audit: player stats, passives, PowerUps, level-up, XP, characters, controls

Sources: raw wiki pages fetched 2026-10-27 with `?action=raw` (saved in `scratchpad/vs-audit/wiki-player/`). The PowerUps table comes from the rendered page (`api.php?action=parse`), because the raw page builds it with a Lua module. Code was read at `b9116484`, read-only.

Paths are under `Assets/_Project/` unless written otherwise.

## Stats

| Item | Rule | Wiki (page + short quote) | Soul Hunter now (file:line) | Status | What to change |
|---|---|---|---|---|---|
| Max Health base and formula | 100, character bonus added, then PowerUp and items multiply | Max_Health: "Max Health = ( 100 + character bonus ) * PowerUp multiplier"; items "stack multiplicatively" | `CharacterData.BaseMaxHealth` holds the total (`Gameplay/Player/PlayerController.cs:355`), then the PowerUp ×1.1^rank (`:376-377`), Hollow Heart ×1.2 (`Gameplay/Combat/WeaponManager.cs:296`) | MATCH | The only difference is rounding: VS keeps decimals (93.17), Soul Hunter rounds max health to a whole number (`HealthController.cs:389`). |
| Recovery | Base 0, additive; each 0.1 adds 10% to pickup healing | Recovery: "each 0.1 translates to an additive 10% boost" | `PlayerStats.Regen` (`PlayerStats.cs:55`); `HealthPickup.HealWithRecovery` (AGENTS heal pass) | MATCH | none |
| Armor reduction | −1 damage per point, at least 1 damage | Armor_(stat): "damage taken from an enemy will always be at least 1" | `HealthController.cs:197` `Mathf.Max(1, dmg - Armor)` | MATCH | none |
| Armor cap | Player_stats max 50; Armor page: no cap on Armor, only the retaliation bonus capped at 50 | Armor_(stat): "There is no upper limit to how much Armor ... but the retaliatory bonus damage is capped at +500% (50 Armor)"; Player_stats max "50 (With the exception of Toastie)" | `PlayerStats.MaxArmor = 50` caps the damage reduction too (`PlayerStats.cs:17,37`) | DIFF (the wiki contradicts itself) | Owner or balance method: either keep the 50 cap (Player_stats) or cap only the retaliation bonus (Armor page). |
| Armor retaliation | +10% retaliatory damage per Armor, additive, ×Might | Armor_(item): "increases Retaliatory Damage by 10% additively" | Only Crimson Shroud uses it (AGENTS); Thorns stat missing | MATCH for what exists | Add Thorns if a retaliation source is ever added. |
| Move Speed | 100% base; character, PowerUp and items add | Move_Speed: "stack additively" | `PlayerStats.MoveSpeedMultiplier` additive (`PlayerStats.cs:188,271`) | MATCH | none |
| Might cap | 1000% after character, PowerUp and items; Soul Eater not capped | Might: "upper limit ... is 1000%"; Player_stats "(Does not affect Soul Eater ...)" | `MaxMight=10` (`PlayerStats.cs:16,23`); RawMight for Soul Eater | MATCH | none |
| Area / Speed / Duration caps | 1000% / 500% / 500%; "Bonuses from Arcanas are unaffected by the limit" | Area / Speed / Duration pages | `Area => Min(RawArea + ArcanaAreaBonus, MaxArea) * Factor` (`PlayerStats.cs:30,54,75`): additive Arcana bonuses go inside the cap | DIFF (small) | Add `Arcana*Bonus` after `Mathf.Min`, as the wiki says. |
| Amount cap | 10 | Amount: "upper limit ... is 10" | `MaxAmount=10` (`PlayerStats.cs:17,50`) | MATCH | none |
| Cooldown floor | 10% total; Tragic Princess "is not limited by the -90% Cooldown cap" | Cooldown | `Cooldown => Max(0.1, _cooldown - ArcanaCooldownBonus)` (`PlayerStats.cs:25`): the Arcana bonus also stops at the floor | DIFF (small) | Let the Arcana (III/XX) reduction go past the 10% floor. Silent Old Sanctuary keeps its own −40% limit. |
| Luck | 100%, additive, no cap | Luck | `_luck` additive (`PlayerStats.cs:38,274`) | MATCH | none |
| Growth | 100%, additive; +100% at levels 20 and 40, taken off at 21 and 41 | Growth; Level_up | `PlayerExperience.GrowthBonusChange` (`PlayerExperience.cs:46,129-130`) | MATCH | none |
| Greed / Curse | Additive, no cap | Greed, Curse: "stack additively" | `PlayerStats.cs:40-43` | MATCH | none |
| Curse effects | Health on new spawns at once; the others at the next minute; 500 on screen | Curse: "All other effects are applied at the start of the next minute"; "capped at 500" | `EnemySpawner.MinuteCurse`, `HardCap` 500 (AGENTS row 11) | MATCH (not re-read in code) | none |
| Magnet | 30 × character × PowerUp × items; the Golden Egg adds to the base | Magnet: "Magnet = (30 + Eggs bonus) * Character bonus * PowerUp multiplier" | `PlayerStats.cs:61-62,194,283-285` | MATCH | none (the 4-unit radius is Soul Hunter's mapping) |
| Revival | +1 each, revive at 50% Max Health, Revive button | Revival: "revive once upon death with 50% of their Max Health" | `HealthController.RevivalHealth` (`HealthController.cs:328`); `PlayerController.cs:248-286` | MATCH | none |
| Reroll | Starts at 0; the shown items cannot come back in the new set | Reroll: "items that were discarded by a reroll cannot appear in the new set" | `LevelUpManager.cs:64,334-338` | MATCH | none |
| Skip | Gives 20% of the experience to the next level | Skip, Tips: "they provide 20% of the experience to the next level" | `SkipExperienceShare=0.2f` (`LevelUpManager.cs:124,353`) | MATCH (the only source is a tip) | none |
| Banish | Removed for the run; a banished item cannot be raised by chests | Banish: "can no longer be offered upon level ups or upgraded by Treasure Chests" | `_banishedTypes` filter (`LevelUpManager.cs:542`), used by chests through `GetRandomValidUpgradesForChest` (`ChestLogicController.cs:255`) | MATCH | none |
| Preserve | 10% per rank to keep a Reroll, Skip or Banish | Preserve | `Preserved()` (`LevelUpManager.cs:118`) | MATCH | none |
| Invulnerability Time | 240 ms added to every immunity; the stat is capped at 1 s; Parm Aegis +50 ms per level | Invulnerability_Time: "capped at 1 second" | Fixed `VsInvulnerabilityTime = 0.24f` (`HealthController.cs:87`); no stat, no cap, no Parm Aegis | MISSING (the stat) | Make it a stat (base 0.24, cap 1.0) if Parm Aegis is added; until then it matches. |
| Hidden stats Fever, Thorns, Seal | Stats in the list | Player_stats: "Fever ... Affects the duration of Gold Fever"; "Thorns ... Retaliatory" | None | MISSING | Fever: a 5-rank PowerUp, 50,000 gold. Thorns and Seal: only with their items. |
| Stats panel order | Max Health … Banish; Reroll, Skip and Banish shown only once bought | Characters: the list order | `CharacterStatSheet` (`Gameplay/Player/CharacterStatSheet.cs:67`) | MATCH | none |

## Passive items

| Item | Rule | Wiki (page + short quote) | Soul Hunter now (file:line) | Status | What to change |
|---|---|---|---|---|---|
| Passive slots | 6, plus stage items past 6 | Passive_items: "6 slots for passive items" | `_maxPassiveSlots = 6` (`WeaponManager.cs:35`); stage items past six (AGENTS) | MATCH | none |
| Spinach | +10% Might, 5 levels, rarity 100 | Spinach: "rarity = 100", "max-level = 5", "+10%" | `WeaponManager.cs:301`; `WD_Spinach` 5/100 | MATCH | none |
| Armor | +1 Armor, 5 levels, rarity 100 | Armor_(item) | `:307`; `WD_Armor` 5/100 | MATCH | none |
| Hollow Heart | ×1.2 Max Health, no heal, 5 levels, rarity 90 | Hollow_Heart: "multiplies Max Health by 1.2" | `:296` (healByIncrease false); `WD_MaxHealth` 5/90 | MATCH | none |
| Pummarola | +0.2 Recovery, 5 levels, rarity 90 | Pummarola | `:308`; 5/90 | MATCH | none |
| Empty Tome | −8% Cooldown, 5 levels, rarity 50 | Empty_Tome | `:300`; 5/50 | MATCH | none |
| Candelabrador | +10% Area, 5 levels, rarity 100 | Candelabrador | `:305`; 5/100 | MATCH | none |
| Bracer | +10% Speed, 5 levels, rarity 100 | Bracer | `:302`; 5/100 | MATCH | none |
| Spellbinder | +10% Duration, 5 levels, rarity 100 | Spellbinder | `:306`; 5/100 | MATCH | none |
| Duplicator | +1 Amount, 2 levels, rarity 50 | Duplicator | `:307`; 2/50 | MATCH | none |
| Wings | +10% Move Speed, 5 levels, rarity 50 | Wings | `:294`; 5/50 | MATCH | none |
| Attractorb | Magnet ×1.5, ×1.33, ×1.25, ×1.2, ×1.33 (3.98×), rarity 100 | Attractorb: "3.980025x Magnet" | `AttractorbStep` (`WeaponManager.cs:364-375`); 5/100 | MATCH | none |
| Clover | +10% Luck, 5 levels, rarity 100 | Clover | `:312`; 5/100 | MATCH | none |
| Crown | +8% Growth, 5 levels, rarity 70 | Crown | `:313`; 5/70 | MATCH | none |
| Stone Mask (Miser's Idol) | +10% Greed, 5 levels, rarity 100 | Stone_Mask | `:326`; `WD_MisersIdol` 5/100 | MATCH | none |
| Skull O'Maniac | +10% Curse, 5 levels, rarity 40 | Skull_O'Maniac | `:314`; 5/40 | MATCH | none |
| Tirajisú | +1 Revival, 2 levels, rarity 40; leaves the inventory when the last revival is used | Tirajisú: "rarity = 40", "max-level = 2"; tips | `:315`; `ReleaseSpentTirajisu` (`:341-353`) | MATCH | none |
| Torrona's Box (Cursed Coffer) | 9 levels; +4% Omni at level 1, +3% at levels 2-8, +100% Curse at 9; rarity 40 | Torrona's_Box | `WeaponManager.cs:329-337`; 9/40 | MATCH | none |
| Silver Ring (Ring of Long Hours) | 9 levels; nothing at level 1; +5% Duration and Area at levels 2-9; stage item only; rarity 10 | Silver_Ring | `:340-344`; `IsStageItemOnly` (`UpgradeRarity.cs:18-22`) | MATCH | none |
| Gold Ring (Ring of Dark Hours) | +5% Curse at levels 2-9; stage item only | Gold_Ring | `:345` | MATCH | none |
| Metaglio Left | Levels 2-9: +0.1 Recovery, ×1.05 Max Health | Metaglio_Left | `:318-322`: ×1.05 **heals the gain** (default `healByIncrease = true`, `HealthController.cs:383`) | DIFF? (the wiki says nothing on healing) | Wiki silent: balance method. Hollow Heart's "does not heal" is the nearest rule, so pass `false`. |
| Metaglio Right | +5% Curse at levels 2-9 | Metaglio_Right | `:323` | MATCH | none |
| Passive unlocks | Hollow Heart: survive 1 min; Pummarola: 5 min with Gennaro; Crown: level 10, and so on | each page | `Save/SaveData/ItemUnlocks.cs:122-189` (Soul Hunter characters stand in for VS ones) | MATCH (mapped) | none |
| Level-up weights | P = rarity / pool weight; one shared pool | Level_up "Random selection of items" | `UpgradeRarity.cs:30-81`; `PickWeightedIndex` (`LevelUpManager.cs:637`) | MATCH | none |

## PowerUps

| Item | Rule | Wiki (page + short quote) | Soul Hunter now (file:line) | Status | What to change |
|---|---|---|---|---|---|
| 23 PowerUps, Might … Preserve | ranks, bonus per rank and initial cost (Might 5/5%/200 … Preserve 5/10%/500) | PowerUps table (rendered) | `Save/SaveData/PowerUps.cs:44-69`: every row checked, all equal | MATCH | none |
| Max Health and Magnet stacking | ×1.1 and ×1.25, multiplied | Max_Health / Magnet: "Multiplicative" | `PowerUps.cs:174,177` | MATCH | none |
| Price formula | `InitialPrice` when nothing is bought, else `Initial×(1+Bought) + floor(20×1.1^TotalBought)` | PowerUps "Cost" | `PowerUps.cs:101-109` | MATCH | none |
| Refund | Every rank's price comes back; fees are additive | PowerUps: "lose all of their PowerUp ranks and get their gold back" | `RefundValue` (`:147-158`) | MATCH | VS loses "a negligible amount" to rounding; Soul Hunter refunds exactly (fine). |
| On/off for maxed PowerUps | All but Reroll, Skip, Banish and Seals | PowerUps | `CanToggle` (`:123-124`) | MATCH | none |
| Seal I/II/III/All | 10 ranks each, 10,000 gold, used in the Collection | PowerUps table | Not built (`PowerUps.cs:39` "left out") | MISSING | Needs the Collection banish menu: owner decision. |
| Fever | 5 ranks, +5% Gold Fever duration, 50,000 gold | PowerUps table, row "-" | Not built | MISSING | Add it: Soul Hunter has Gold Fever, so add `PowerUpId.Fever` to the shop. |
| Reroll rank unlocks | Level 80 with Mortaccio, Cavallo, Ramba, O'Sole and Ambrojoe | Reroll | Level 80 with Corvan, Aria, Draven, Gideon, Kaelen (`PowerUps.cs` `RerollCharacters`) | DIFF (owner-approved mapping) | none |
| Skip rank unlocks | Survive 15 or 30 min on Il Molise, Green Acres, Bone Zone, Moongolow, Boss Rash | Skip | Levels 6-10 at 15/30/30/15/15 min (`SkipSurvivals`) | DIFF (owner-approved mapping) | none |
| Banish rank unlocks | 60/70/80/90/100 Collection entries | Banish | Same (`BanishCollectionEntries`) | MATCH | none |
| Omni / Charm / Defang / Preserve unlocks | Torrona's Box at max level; Apoplexy, Antidote, Wax Fetish relics | Omni, Charm, Defang, Preserve | Cursed Coffer at level 9; Soul Hunter relics | MATCH (mapped) | none |
| Defang exclusions | "wave bosses, enemies from wave events, and special enemies" cannot be defanged | Defang | Only ordinary wave spawns (AGENTS PowerUp section) | MATCH | none |
| Charm | Flat, added after Curse | Charm: "added last after other bonuses" | `PlayerStats.Charm`; spawner (AGENTS) | MATCH | none |

## Level-up and experience

| Item | Rule | Wiki (page + short quote) | Soul Hunter now (file:line) | Status | What to change |
|---|---|---|---|---|---|
| XP curve | 5 to level 2, +10 per level to 20, +13 to 40, +16 after; +600 at 20 and +2400 at 40 | Level_up "Experience requirement" | `GemsToNextLevel` (`PlayerExperience.cs:34-43`) ×10 (Soul Hunter's 10-XP gem) | MATCH (scaled ×10) | none |
| Several level-ups from one gem | Allowed | Level_up | `while` loop (`PlayerExperience.cs:120`) | MATCH | none |
| Growth applied | XP × Growth | Experience_Gem: "multiplied by Growth" | `PlayerExperience.cs:115`; it also rounds and gives at least 1 | MATCH | none |
| Gem colours and 400 cap | Blue up to 2, green up to 9; past 400 gems one red gem | Experience_Gem | Thresholds 20/90 (owner ×10); `MaxGemsOnGround` 400 (AGENTS) | MATCH | none |
| 3 or 4 choices | Fourth choice = 1 − 1/Luck | Level_up "Fourth option" | `ChoiceCountForLuck` (`LevelUpManager.cs:48-52`) | MATCH | none |
| Owned-item checks | `1 + 0.3x − 1/Luck`, twice, only while the inventory is not full, a repeat redrawn from the whole pool | Level_up "Receiving owned items" | `OwnedChance` (`:575-579`), `DrawChoices` (`:591-628`) | MATCH | none |
| Maxed level-ups | Gold or Floor Chicken; "always" options after 100 Big Coin Bags; "Always Floor Chicken" gives gold at full health | Level_up | `MaxedRewards`, `AlwaysAfterBags` (`LevelUpManager.cs:480`) | MATCH (the full-health rule was not re-read in code) | Check that `MaxedRewards.ApplyAlways` gives gold at full health. |
| No Skip or Reroll when maxed | "Skips and Rerolls are unavailable when the player has maxed out their inventory" | Level_up | `CanReroll` / `CanSkip` (`:320-321`) | MATCH | none |
| Limit Break cards | Offered once nothing is left to level | Level_up | `LimitBreak` | MATCH (not re-audited) | none |
| Invulnerability after a pick | Patch 0.2.12 "brief moment of invulnerability" | Level_up history | 240 ms (AGENTS) | MATCH | none |
| Extra items below level 3 | v1.6.0: "fill out extra items when less than 3 are available and you are less than level 3" | Level_up history | Not found in `LevelUpManager` | MISSING? | The wiki does not say what fills the gap (balance method). Low priority. |
| Banish keeps the screen open | 15 Jul 2026 public beta, "[QOL] Banish: after banishing an item, you can keep using the LevelUp menu" | Level_up history | Soul Hunter deals new cards after a banish (`:361-380`) | MATCH? (beta, not checked in play) | none |
| Max weapons setting | 1 to 6 weapons, "Unlocked through Mindbender" | Characters, Character setup | Not built | MISSING | Add it with the Character setup screen. |
| Random LevelUp toggle | Can be switched during a run | Level_up history | Blind Fate (AGENTS P3-05) | MATCH | none |
| Evolution hints | Patch 0.3.2; passives too since 1.4.200 | Level_up | Built (AGENTS P3-01) | MATCH | none |

## Characters

Soul Hunter's 13 characters are its own (owner rule). None copies a VS stat block, so each row compares a Soul Hunter character with the VS character that has the same starting weapon or the same bonus.

| Item | Rule | Wiki (page + short quote) | Soul Hunter now (file:line) | Status | What to change |
|---|---|---|---|---|---|
| Starting roster | Only Antonio at the start; the others unlocked, then bought | Characters: "the only character that is available from the start is Antonio" | Only Kael (`Data/Characters/Char_Kael.asset` Unlock 0) | MATCH | none |
| Price rise | +10% of base per character bought, additive (Antonio free) | Characters: "increased by 10% additively per characters purchased" | `CharacterUnlocks.PriceFor` (`Gameplay/Player/CharacterUnlocks.cs:77-84`), rounded down | MATCH | none |
| Base prices | Imelda 10, Pasqualina 100, most 500, coffin characters 1000, Gallo/Divano 2000, Zi'Assunta 5000 | character pages, `cost` | 300 to 3000 (`GoldPrice` in each `Char_*.asset`) | DIFF | Owner: use the VS price of each character's VS counterpart, or record these as balance values. |
| Unlock conditions | VS conditions (Arca: Fire Wand level 4; Clerici: recover 1000 HP; coffins …) | character pages | Soul Hunter conditions (AGENTS "Character unlocks": provisional, not VS data) | DIFF | Map each to its VS counterpart's condition, or keep them as owner decisions. |
| Coffin unlocks | Pugnala, Giovanna, Poppea, Concetta and Zi'Assunta come from stage coffins | their pages | Soul Caskets on Levels 1-7 free seven characters (AGENTS) | MATCH (mapped) | none |
| Corvan, Antonio-like | Antonio: Whip, **120 Max Health, +1 Armor**, +10% Might every 10 levels to 50 | Antonio_Belpaese: "starts with +20 Max Health and +1 Armor" | `Char_Corvan.asset`: Whip, 100 HP, 0 Armor, Might bonus to 50 | DIFF | Set BaseMaxHealth 120 and StartingArmor 1, if Corvan is meant to be Antonio. |
| Kael (default hero) | VS default hero Antonio: 120 HP, +1 Armor | Antonio_Belpaese | Whip, 100 HP, no bonus (`Char_Kael.asset`) | DIFF (story hero) | Owner decision. |
| Valerius, Divano-like | Divano: Laurel, Recovery 1, Luck +30%, Greed −50%, Banish +10, "Starts with 1 extra level", +1 Armor every 5 levels to 25 | Divano_Thelma | Whip, 150 HP, Might 1.2, Move 0.9; Armor bonus to 25 (`Char_Valerius.asset`) | DIFF (only the level bonus matches) | Owner decision. |
| Draven (Knife, Amount +2) | Gennaro: Knife, 120 HP, Amount +1 | Gennaro_Belpaese | 100 HP, Amount +2, Move 1.3, Might 0.9 | DIFF | Owner decision. |
| Gideon (Axe, Area +50%) | Lama: Axe, 110 HP, +10% Might, Move and Curse, +5% each every 10 levels to +20%; Porta: Area +30% | Lama_Ladonna, Porta_Ladonna | 200 HP, Move 0.8, Area 1.5 | DIFF | Owner decision. |
| Isolde (Santa Water, −15% Cooldown) | Clerici: Santa Water, 150 HP, 0.5 Recovery, +400% Area falling 100% per level | Suor_Clerici | 90 HP, Cooldown 0.85 | DIFF | Owner decision. |
| Kaelen (Cross, 3× crits) | Krochi: Cross, Move +30%, 1 Revival, +1 Revival at level 33 | Krochi_Freetto | 110 HP, Might 1.3, Move 1.15, crit 3× | DIFF | Owner decision. |
| Lumina (Bible, Recovery 1) | Dommario: Bible, Duration and Speed +40%, Move −40% | Dommario | 110 HP, Recovery 1 | DIFF | Owner decision. |
| Elaria and Seraphina (Magic Wand) | Imelda: Magic Wand, +10% Growth every 5 levels to +30% | Imelda_Belpaese | Elaria 70 HP, Might 1.2; Seraphina 80 HP, Move 1.1, +3 bounces | DIFF | Owner decision. |
| Aria (Song of Mana, Growth +20%) | Poppea: Song of Mana, Move +20%, +1% Duration every level | Poppea_Pecorina | 95 HP, Move 1.05, Might 0.8, Growth 1.2 | DIFF | Owner decision. |
| Rurik (Bone, faster at low health) | Mortaccio: Bone, +1 Amount every 20 levels to +3, changes form at 80 | Mortaccio | 130 HP, Move 1.2, Might 1.1, low-health speed | DIFF | Owner decision. |
| Sylas (Runetracer, 10 s shield) | Pasqualina: Runetracer, Speed +10%, +10% every 5 levels to +30% | Pasqualina_Belpaese | 100 HP, Move 1.1, 10 s shield | DIFF | Owner decision. |
| Level-bonus kinds | VS: one stat or several at once (Lama, Zi'Assunta), every level (+1% Pugnala, Giovanna, Poppea, Concetta), every 20 levels (Amount), at one level (Krochi's revival at 33), bonuses that fade (Porta, Clerici, Ambrojoe), "Starts with 1 extra level" (Christine, Gallo, Divano) | pages above | `LevelBonusStat` has one stat at every Nth level, with no Speed, Duration, Amount, Revival or Curse (`Data/Config/CharacterData.cs:16,53-64`) | MISSING (mechanics) | Extend `CharacterData` with several stats, per-level gains, fading start bonuses, a starting level and a one-off level bonus. |
| Starting Reroll/Skip/Banish | Gallo +5/+5, Divano Banish +10, Sigma +108 | pages | No `CharacterData` field | MISSING | Add the fields. |
| Two starting weapons | Pugnala: "Phiera Der Tuphello; Eight The Sparrow" | Pugnala_Provola | One `StartingWeapon` | MISSING | Add a second-weapon field (Gun + Azure Repeater exist). |
| Form change (morph) | Mortaccio, Cavallo, Ramba and O'Sole: +1 Amount and +2 Armor at level 80 | Armor_(stat), Amount | None | MISSING | none until owner decision |
| Secret characters and Random | Exdash, Toastie, Smith IV, Random, … | Characters "Secret" | None | MISSING | Owner decision. |
| Character setup | Favourite, Max weapons, Power creep (Survarots / Golden Eggs), Skin | Characters | Favourites locked behind Mindbender (AGENTS); no Max weapons, Power creep or Skin | PARTIAL / MISSING | Owner decision. |
| Stage completion panel and sort | A tick per stage cleared; sort by it | Characters | Built (AGENTS P3-03) | MATCH | none |

## Controls

| Item | Rule | Wiki | Soul Hunter now | Status | What to change |
|---|---|---|---|---|---|
| Controls page | not on the wiki | `w/Controls` does not exist (empty raw page) | Options remapping, controller, touch stick (AGENTS Accessibility) | NOT CHECKED (no wiki page) | Audit against the Options page instead. |
| Space confirms in menus | Options patch 0.2.4: "Spacebar can be used just like Enter" | from AGENTS (not fetched again) | `UiSubmitKeys` | MATCH | none |

## Missing content

**Passives (VS, including DLC) with no Soul Hunter item.** Every base-game level-up passive is in Soul Hunter, plus the stage-only rings and Metaglios. Missing:
- Academy Badge (Tides of the Foscari)
- Karoma's Mana (Charm passive)
- Parm Aegis (Invulnerability Time)
- Pearl Magatama
- Velvet Dodecahedron
- Weapon Power-Up
- Mini Crewmate / Engineer / Ghost / Guardian / Horse / Impostor / Scientist / Shapeshifter (Emergency Meeting)
- Outer Saboteur
- the Ode to Castlevania familiars (Faerie, Imp, Ukoback, Sacred familiars, Innocent Devils)

**Stats and PowerUps:** Fever (PowerUp and stat), Seal I/II/III/All, Thorns, Invulnerability Time as a real stat with its 1 s cap.

**Characters.** The wiki counts 225 characters: 56 in the base game (34 standard and 22 secret) and 169 in DLCs. Soul Hunter has 13 of its own.
- Base standard with no Soul Hunter counterpart:
  - Antonio-likes, partly matched: Antonio (Corvan), Imelda, Pasqualina, Gennaro, Arca, Porta, Lama, Poe, Clerici, Dommario, Krochi, Christine, Pugnala, Giovanna, Poppea, Concetta;
  - Mortaccio, Yatta Cavallo, Bianca Ramba, O'Sole Meeo, Sir Ambrojoe, Iguana Gallo Valletto, Divano Thelma, Zi'Assunta, Queen Sigma;
  - Bat Robbert, Zi'Appunta, Big Troubler, She-Moon Eeta, Para Kooleo, Santa Ladonna, Gazebo, Chula-Reh, Space Dude.
- Secret: Exdash, Toastie, Smith IV, Random, Boon Marrabbio, Avatar Infernas, Minnah, Leda, Cosmo, Peppino, Big Trouser, missingN, Gains Boros, Gyorunton, Mask of the Red Death, Bats Bats Bats, Rose De Infernas, Torino, Scorej-Oni, Gyoruntin, Secretino Dagsson, Space Dette.
- DLC: every Moonspell, Bloodmoon, Foscari, Emergency Meeting, Operation Guns, Ode to Castlevania, Emerald Diorama and Ante Chamber character.

**Character mechanics Soul Hunter lacks:**
- start at level 2 ("1 extra level");
- start bonuses that fade per level (Porta's cooldown, Clerici's area, Ambrojoe's amount);
- per-level percentage gains with no cap;
- bonuses to several stats at once;
- Amount every 20 levels;
- a revival at a set level;
- the level-80 form change;
- starting Reroll, Skip and Banish;
- two starting weapons;
- negative stat blocks (the secret characters);
- the Random character;
- skins;
- the Max weapons and Power creep setup;
- co-op and party.

## Not checked (budget)

- Each stat's minor interactions with DLC weapons. They do not apply.
- Arcana stat effects (another audit area).
- The Golden Egg / Soul Egg amounts per stat. They are in SoulEggs.cs, not re-read.
- `MaxedRewards` "Always Floor Chicken gives gold at full health".
- The v1.6.0 rule that fills the level-up screen below level 3.
- The Character setup and Options/Controls screens against the Options page. The Controls page does not exist.
- DLC character pages: only the base game, the coffin characters and three secret ones were fetched.
