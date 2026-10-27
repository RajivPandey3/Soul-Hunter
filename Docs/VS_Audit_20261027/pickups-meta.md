# VS audit: pickups, light sources, chests, economy, merchant, Arcanas, relics, collection, unlocks, secrets, results, options, main menu

Date: 2026-10-27. Read-only audit. Wiki pages fetched raw (`?action=raw`) into
`C:\Users\Lenovo\AppData\Local\Temp\claude\D--Unity-Projects-Soul-Hunter\6fdfafb8-2413-4f46-b8cb-631090820c24\scratchpad\vs-audit\wiki\` (Arcana pages in `wiki\arc\`, relic pages in `wiki\rel\`).
Code paths are relative to `D:\Unity Projects\Soul-Hunter\Assets\_Project\`. Soul Hunter (SH) names: Soul Toll = Rosary, Hellfire Breath = Nduja Fritta Tanto, Luck Charm = Little Clover, Gilded Charm = Gilded Clover, Soul Magnet = Vacuum, Time Freeze = Orologion, Soul Egg = Golden Egg, Soul Lantern = light source.

Status key: MATCH, DIFF (rule differs; "owner" = recorded owner decision, "balance" = recorded balance-method choice), MISSING, WIKI-CONFLICT (the wiki contradicts itself; owner must choose), UNKNOWN (not proven either way).

## Table

| Item | Rule | Wiki (page + short quote) | Soul Hunter now (file:line) | Status | What to change |
|---|---|---|---|---|---|
| Gold Coin | value / weight / level | Gold_Coin_(pickup): "rarity = 50 / level = 0 / luck = No", "Gives 1 Coin", x Greed | Gameplay/Items/LightSource.cs:80 (50, 0), :99-101 no Luck | MATCH | - |
| Coin Bag | value / weight | Coin_Bag: "rarity = 10 / level = 0 / luck = No", 10 coins | LightSource.cs:80 | MATCH | - |
| Rich Coin Bag | value / weight / level | Rich_Coin_Bag: "rarity = 1 / level = 5 / luck = Yes", 100 coins | LightSource.cs:81 | MATCH | - |
| Floor Chicken | heal / weight | Floor_Chicken: "rarity = 12 / level = 0 / luck = Yes", "Heals 30 HP" | LightSource.cs:80; Gameplay/Items/HealthPickup.cs:12,76 (30 x (1+Recovery)) | MATCH | - |
| Floor Chicken | brief invulnerability | Pickups trivia: "Picking up Floor Chicken ... provide a brief period of invulnerability" | HealthPickup.cs:93 (240 ms) | MATCH | - |
| Rosary (Soul Toll) | weight / level / effect | Rosary: "rarity = 1 / level = 8 / luck = Yes"; "instantly kills all enemies on the screen"; "some enemies ... are resistant or immune" | LightSource.cs:82; Gameplay/Items/ScreenWipePickup.cs:40-43 (skips stage bosses and instant-kill-resistant) | MATCH | - |
| Rosary vs Stalker/Drowner | kills them, chest | Rosary: "can attack and defeat the otherwise intangible Stalkers and Drowners ... causing them to drop Treasure Chests" | ScreenWipePickup.cs:48 | MATCH | - |
| Orologion (Time Freeze) | weight / level / 10 s / new enemies frozen | Orologion: "rarity = 2 / level = 4"; "frozen for 10 seconds"; "new enemies will also become frozen" | LightSource.cs:81; Gameplay/Items/TimeFreezePickup.cs:15,23 | MATCH | - |
| Orologion | Gold Fever decay halved | Orologion: "slows down Gold Fever's duration decay by half" | Gameplay/Items/GoldFever.cs:43 | MATCH | Stale doc comment GoldFever.cs:9 says "Not adopted: Orologion halving the decay"; fix the comment. |
| Vacuum (Soul Magnet) | weight / level / gems only | Vacuum: "rarity = 2 / level = 12"; "All Experience Gems are pulled"; "Ignores all pickups" | LightSource.cs:81; Gameplay/Items/MagnetPickup.cs:18-26 (gems only) | MATCH | - |
| Nduja (Hellfire Breath) | weight / level | Nduja_Fritta_Tanto: "rarity = 1 / level = 0 / luck = Yes" | LightSource.cs:82 | MATCH | - |
| Nduja | weapon stats | Nduja infobox: "damage = 30 / amount = 12 / cooldown = 0.5 / interval = 0.04 / duration = 1.0 second / area = 200% / speed = 300%"; 10 s | Gameplay/Weapons/FireBreath.cs:19-26 | MATCH (numbers) | Speed 300% is mapped to 9 units/s, "provisional" (FireBreath.cs:25); record the mapping. |
| Nduja | direction | Nduja: "breathe fire in the direction they are facing, similar to Knife" | FireBreath.cs:94 `AimDirection(HolderFacing)` (nearest enemy, facing fallback); 30° random cone FireBreath.cs:27,105 | DIFF (owner: nearest-enemy aiming) | Cone 30° is SH's own number (wiki silent); OK under owner aiming rule, list it in the recorded exceptions. |
| Nduja | Duration not capped by egg cap | Nduja tips: "It is not limited by the 400% Duration cap" | not checked | UNKNOWN | Check FireBreath reads raw Duration. |
| Little Clover (Luck Charm) | weight / Luck | Light_source table: "Little Clover 1 / 0"; Light_source: Luck increases "pickups other than Gold Coin and Coin Bag"; BUT Little_Clover infobox: "rarity = 0.5 / luck = No" | LightSource.cs:82 (weight 1, Luck-weighted) | WIKI-CONFLICT | Owner/balance: pick 1 + Luck (table) or 0.5, no Luck (infobox). |
| Little Clover | effect | Little_Clover: "Gives +10% Luck"; "can collect an infinite amount" | AGENTS: +10% Luck for the run | MATCH | - |
| Gilded Clover (Gilded Charm) | weight / level / fever | Gilded_Clover: "rarity = 1 / level = 30 / luck = Yes"; 10 s; "75% chance it drops a Gold Coin that has a value of 0.5"; extensions "0.5, 5 and 10 seconds"; fever coin "0.01"; "capped at 10 seconds" | LightSource.cs:82; GoldFever.cs:14-18 | MATCH | - |
| Light source | 10 HP, Luck spawn formula, cap, oldest replaced | Light_source: "Light sources have 10 health"; "destructibleSpawnChance = mapDestructibleChance · totalLuck"; "a new one will spawn in a location closest to the player, while the oldest ... despawned"; at cap "spawn chance is not influenced by Luck"; one attempt per second | LightSource.cs:15; Gameplay/Items/LightSourceSpawner.cs:64-66, 98-106 | MATCH | - |
| Light source | per-stage chance/max | Mad_Forest "dchance = 10% / max 50% / dmax 10"; Inlaid 7.5/50/10; Dairy 20/60/10; Gallo 7.5/50/10; Cappella 20/80/20 | LightSourceSpawner.cs:44-57 (L1-L4 as listed, L5-L10 Cappella) | MATCH for L1-L4 / DIFF for L5-L10 | Waves on L5-L7 use Abyss Foscari, Mt.Moonspell, Lake Foscari (AGENTS wave 3), whose rows are 7.5%/50%/18, 7.5/50/12, 7.5/50/12 (fetched). Use one stage mapping everywhere. |
| Light source | fixed spawn spots | Light_source: "In some stages, such as Inlaid Library, Dairy Plant and Gallo Tower, the light sources have predetermined possible spawn locations ... other maps ... oval shape around the character" | LightSourceSpawner (off-screen ring on every level) | DIFF (balance, recorded) | Optional: fixed spots on L2-L4. |
| Light source | Fire Wand unlock | Light_source: "Destroying 20 light sources unlocks the Fire Wand" | Save/SaveData/ItemUnlocks.cs:141 | MATCH | - |
| Rerollo | light-source pickup, +1 Reroll | Rerollo: "Pick up to get +1 Reroll", "rarity = 1 / level = 0" (Light_source table: weight "unknown") | not in LightSource.cs:74 enum | MISSING | Add own-named pickup (+1 Reroll), weight 1. |
| Gold Finger | light-source pickup | Gold_Finger: "rarity = 0.02 / level = 30 / luck = No"; invulnerability, -100% Cooldown, min/max spawns 500 for 10 s (+4 s invuln.); 5 reward tiers by kills | none | MISSING (known) | Build (wiki: Astral Stair-only until Trisection; SH needs owner decision on availability). |
| Sorbetto | pickup via Out of Bounds | Sorbetto: "Temporarily emits freezing cones", "rarity = 1"; Out_of_Bounds: "Sorbettos could also be found" | ArcanaEffects.cs:210 doubles Orologion only | MISSING | Add freezing-cone pickup, only with Out of Bounds (XII). |
| Big Coin Bag | 25 gold, maxed level-up and maxed chest slots | Big_Coin_Bag: "Gives 25 Coins"; chests: "replaced with Big Coin Bags (25)" | Core/Game/ChestLogicController.cs:59; MaxedRewards | MATCH | - |
| Big Coin Bag | Candybox chance | Treasure_Chest: "each Coin Bag in a Treasure Chest will have a 1% chance to award ... a Candybox ... guaranteed ... from Treasure Chests that award five items" | none (no Candybox weapon) | MISSING | Needs Candybox (and Super Candybox II Turbo). |
| Little Heart, Crystallized/Cursed/Scorched Soul, Pie, Cheese, Corn, Survarocchi, Friendship Amulet | unlisted / character / co-op pickups | PickupNav "Unlisted"; Little_Heart "recover 1 HP", souls "minor bonus to MaxHealth ..." (rarity 0) | none | MISSING (low priority; tied to VS characters/co-op) | Decide per item; most need VS characters SH lacks. |
| Treasure Chest | not attracted; Luck affects quality only | Treasure_Chest infobox: "Doesn't get attracted"; "Only the quality of the chest is affected by Luck" | AGENTS P1-13; ChestLogicController.cs:91-93 | MATCH | - |
| Treasure Chest | three separate rolls 5/3/1 then fallback | Treasure_Chest: "3% chance for a 5-item chest, and if that roll fails then a 10% ... then a 50% ... level 1 fallback"; "chanceWithLuck = levelChance · totalLuck", "separate random number" | ChestLogicController.cs:83-95 | MATCH | - |
| Treasure Chest | gold per size | Treasure_Chest: "1-item 100-200 / 3-item 300-600 / 5-item 500-1000 at base" | ChestLogicController.cs:63-67 | MATCH | - |
| Treasure Chest | gold when everything maxed | Treasure_Chest: all items "replaced with Big Coin Bags (25)", gold "normally around 150, but this number grows based on how many items" | ChestLogicController.cs:57 (GoldWhenEmpty 100) + per-size range + 25/slot | DIFF (minor) | Wiki is vague ("around 150"); keep, but note GoldWhenEmpty 100 is not a wiki value. |
| Treasure Chest | only owned items | Treasure_Chest: "a random weapon or passive item the player currently has" | AGENTS B04 | MATCH | - |
| Treasure Chest | one evolution per chest; evolve flag per boss | Treasure_Chest: "Generally, only one weapon can be evolved per chest"; Template:Treasure reward1 = Evolution when evo | ChestLogicController.cs:233-236; ChestTiers | MATCH | - |
| Treasure Chest | tier rows per stage | Template:Treasure configs per boss; each VS stage has its own rows | AGENTS P1-14: Mad Forest rows on all 10 levels | DIFF (balance, recorded) | Optional: each level's source-stage rows. |
| Beginner's luck 1-1-3-1-1-5 | first six chests | Treasure_Chest: "first six chests ... 1-1-3-1-1-5 ... cannot be skipped" | ChestLogicController.cs:99; ChestOpeningScreen.cs:19 (UnskippableChests 6) | MATCH | - |
| Chest skipping | 1/3 after 7th; 5 after 50 five-item or 500 total or fireworks; gold-only auto-skip | Treasure_Chest: "once the player has seen the final fireworks, collected 50 five-item chests, or collected a total of 500 chests"; "chests that contain only Big Coin Bags will have their animation automatically skipped" | ChestOpeningScreen.cs:19-30 | MATCH (fireworks path absent; no SH counterpart) | - |
| Quick Treasures option | skip all chest menus | Treasure_Chest: "Edge of the Earth (VIII) ... unlock the option to toggle 'Quick Treasures'" | none | MISSING (needs Darkana) | Add with Darkanas. |
| Chest types | bronze / silver / Arcana | Treasure_Chest "Bronze ... incapable of ... evolutions"; "Silver ... capable"; Arcana chest "choose one out of 4 or 6"; "two additional items ... Game Killer" | ChestLogicController.cs:35-37,159-179; ArcanaEffects.cs:43 | MATCH | - |
| Chest types | gold chest | Treasure_Chest: "Golden chests ... weapons the player does not currently have ... appear as stage items" | none | MISSING | Needs Call of a Mad Moon / Zi'Appunta; low priority. |
| Chest types | dark chest | Treasure_Chest: "Dark chests ... 50% ... new passive item"; only from Moonlight Bolero / Call of a Mad Moon | none | MISSING (Darkana-tied) | Build with Darkanas. |
| Arcana chest fallbacks | bronze at limit, silver when all in use | Treasure_Chest Arcana: "turn into bronze chests"; "act like Silver chests" | ChestLogicController.cs:141-176 | MATCH | - |
| Merchant | where it appears | Merchant: "They initially only appear in Moongolow. Then later in all other stages after the purchase of the Glass Vizard" | Core/Game/WanderingMerchant.cs; AGENTS: Level 3 only | DIFF (owner, Level 3 decision) | Owner: keep, or add Glass Vizard relic to spread it to every level. |
| Merchant | once per run; Endless every cycle; never leaves until touched | Merchant notes: "accessed once per run ... In Endless ... spawning every cycle"; "does not disappear after a certain time" | AGENTS Level 3 merchant | MATCH | - |
| Merchant | weapon wares 1,000 | Merchant: Bone, Cherry Bomb, Carréllo, Celestial Dusting, La Robba (+DLC) "1,000"; "cannot be bought if the max weapon limit is already reached" | WanderingMerchant.cs:49; sells Bone + Cherry Bomb | DIFF (content) | Carréllo, Celestial Dusting, La Robba missing (weapons not in SH). |
| Merchant | Golden Egg 10,000 repeatable | Merchant: "Golden Egg ... 10,000 ... Can be purchased repeatedly" | WanderingMerchant.cs:50 | MATCH | - |
| Merchant | Inverse wares | Merchant: Skip 100, Banish 500, Reroll 1,000 "until the player has 20"; Pick a Card 20,000 | WanderingMerchant.cs:45,53-56 | MATCH | - |
| Merchant | Inverse Arcana per cycle | Arcanas: "With Endless mode enabled, the Merchant will respawn with another for sale at the start of each ... cycle" | WanderingMerchant.cs:27-30 | MATCH | - |
| Merchant | Endless +1 Revival 10,000 | Merchant page lists no Revival ware; SH cites Stages page "+1 Revival instead of Golden Eggs" | WanderingMerchant.cs:59-60,82 | DIFF (balance price) | Re-check Stages page; Merchant page itself does not list it. |
| Merchant | Glass Vizard ware | Merchant: "Glass Vizard ... 10,000 ... Sold in Moongolow only" | none | MISSING | Only if owner moves merchant off Level 3-only. |
| Eggman / Healer | bulk eggs, egg removal | Golden_Egg: Eggman at "5,000 Golden Eggs ... 1,000,000"; Healer "remove 100 Golden Eggs" | none | MISSING (low priority) | Optional. |
| Golden Egg (Soul Egg) | per-stat amounts | Golden_Egg: "+1 Max Health; +0.1 Recovery, Armor, Amount, Revival, Reroll; +1% ...; -0.5% Cooldown; +0.3 Magnet; +0.2 Skip, Banish" | Gameplay/Player/SoulEggs.cs:28-34 (Magnet 0.01 of base 1 = VS 0.3 of 30) | MATCH | - |
| Golden Egg | caps | Golden_Egg: "+50 Armor, +900% Might, +400% Speed, +400% Duration, +900% Area, -90% Cooldown, +10 Amount" | SoulEggs.cs:25 | MATCH | - |
| Golden Egg | fractional flat stats | Golden_Egg: "+0.1 ... Armor, Amount, Revival" (no floor rule given) | SoulEggs.cs:88-106 floors Armor/Amount/Revival/Max Health to whole points | UNKNOWN | Wiki silent on rounding; confirm VS keeps fractions (Recovery is not floored). |
| Golden Egg | sources | Golden_Egg: Merchant, Atlanteans drop it, Reaper drops five | Merchant + ItemGuardians drop | MATCH (Reaper n/a by owner decision) | - |
| Arcanas | count / max 3 / chest minutes | Arcanas: "up to three Arcanas"; "At 11:00 and 21:00 ... Arcana Chest"; "If the player has three or more Arcanas, the Arcana bosses will instead drop regular chests"; disabled Arcanas = no bosses | AGENTS Run modes / P2-01 | MATCH | - |
| Arcanas | start pick / random | Arcanas: "choose one of any unlocked Arcanas or Darkanas ... option to select a random one" | Core/Game/ArcanaDraft.cs; ArcanaChoiceScreen | MATCH | - |
| Arcanas | chest draft 4 / 6+free reroll at 23 / rerolls no repeats / skip | Arcanas: "four or more ... Rerolls ... previous options will not be offered again"; "23 or more ... six ... one free Reroll"; Patch 0.11.0 "option to skip" | ArcanaDraft.cs | MATCH | 23+ can never be reached with 22 cards (no Darkanas). |
| Arcanas | unlock conditions | Arcanas: Randomazzo gives VI; others "reaching level 50 with certain characters, or ... 31:00 mark"; Game Killer: "Defeat the final enemy in Cappella Magna" | AGENTS: every card unlocked from start | DIFF (balance, recorded) | Owner: SH-own conditions (e.g. level 50 with each SH hero, 31:00 on L1-L4). |
| 0 Game Killer | effect | Game_Killer: "Halts XP gain. Experience Gems turn into exploding projectiles. All Treasure Chests contain at least 3 items." | ArcanaEffects.cs:41-45 | MATCH | - |
| I Gemini | effect | Gemini: "Listed weapons come with a counterpart" | GeminiCounterpart (AGENTS) | MATCH | - |
| II Twilight Requiem | effect | "explosions when they expire ... affected by Curse" | ArcanaEffects.cs:83-84 | MATCH (balance read of its own examples) | - |
| III Tragic Princess | effect | "cooldown ... reduces when moving" | ArcanaEffects.cs:111 (rate 0.5 balance) | MATCH | - |
| IV Awake | effect | "+3 Revivals. Consuming a Revival gives +10% Max Health, +1 Armor, and +5% Might, Area, Duration, and Speed" | AGENTS Arcanas | MATCH | - |
| V Chaos in the Dark Night | effect | "-50% and +50% over 10 seconds ... +1% projectile Speed every level" | ArcanaEffects.cs:132-134 | MATCH | - |
| VI Sarabande | effect | "Healing is doubled. Recovering HP damages nearby enemies for the same amount." | ArcanaEffects.cs:146-148 | MATCH | - |
| VII Iron Blue Will | effect | "up to 3 bounces and might pass through enemies and walls" | AGENTS Arcanas | MATCH | - |
| VIII Mad Groove | effect | "Every 2 minutes attracts all standard stage items, pickups, and light sources" | ArcanaEffects.cs:157 (120 s) | MATCH | - |
| IX Divine Bloodline | effect | "Armor also affects listed weapons' damage and reflects ... missing Health ... +0.5 Max Health" | ArcanaEffects.cs:167-169 | MATCH | - |
| X Beginning | effect | "+1 Amount ... main weapon and its evolution gain +3" | ArcanaEffects.cs:191 | MATCH | - |
| XI Waltz of Pearls | effect | "up to 3 bounces" | AGENTS Arcanas | MATCH | - |
| XII Out of Bounds | effect | "Freezing enemies generates explosions. Orologions are easier to find. Sorbettos could also be found." | ArcanaEffects.cs:210 | DIFF | Sorbetto part missing (no Sorbetto pickup). |
| XIII Wicked Season | effect | "doubled at fixed intervals ... +1% ... every 2 levels" | ArcanaEffects.cs:227 (10 s each, balance) | MATCH | - |
| XIV Jail of Crystal | effect | "chance to freeze enemies" | ArcanaEffects.cs:238 (25% x Luck, 2 s, balance) | MATCH | - |
| XV Disco of Gold | effect | "coin bags ... triggers Gold Fever. Obtaining gold restores as many HP." | AGENTS Arcanas | MATCH | - |
| XVI Slash | effect | "Enables critical hits for listed weapons. Doubles overall critical damage." | AutoAttackWeapon.SlashArcanaActive (AGENTS) | MATCH | - |
| XVII Lost & Found Painting | effect | "Duration ... -50% and +50% over 10 seconds ... +1% every level" | ArcanaEffects.cs:132-134 | MATCH | - |
| XVIII Boogaloo | effect | "Area ... -25% and +25% over 10 seconds ... +1% every level" | ArcanaEffects.cs:132-134 | MATCH | - |
| XIX Heart of Fire | effect | "projectiles explode on impact. Light sources explode. Character explodes when damaged"; Nduja weight x4 | ArcanaEffects.cs:269-270 | MATCH | - |
| XX Silent Old Sanctuary | effect | "+3 Reroll, Skip, and Banish ... +20% Might and -8% Cooldown for each active weapon slot left empty" | ArcanaEffects.cs:311-313 | MATCH | - |
| XXI Blood Astronomia | effect | "special damaging zones affected by Amount and Magnet ... within Magnet range take damage" | ArcanaEffects.cs:340-342 | MATCH (shapes balance) | - |
| Arcana effects | depth of check | each page's Effects/Tips sections | infobox lines only compared in this pass | UNKNOWN (partial) | The 2026-10-09 build quoted each page; a line-by-line re-check of Tips was NOT done here. |
| Darkanas (13) | whole system | Arcanas: "Obtaining the Darkasso in Room 1665 unlocks an additional set of 'Darkanas'" | none | MISSING | Owner: "later" (AGENTS P2-01). |
| Relics | general | Relics: "permanent items that unlock new mechanics ... Once collected, they are permanently added to the collection ... locations are indicated by green arrows" | Save/SaveData/Relics.cs:68-116 (11 relics); MapGuides off by default | DIFF | Green arrows should be on for relics (VS default). |
| Grim Grimoire | evolution list from pause | Grim_Grimoire: "located to the far west ... Inlaid Library"; "Unlocks the 'Open Grimoire' menu" | Relics.cs:76 (Reaper's Ledger, L2 west) gives +1 Reroll; list open from start | DIFF (owner opened features) | - |
| Ars Gouda | Bestiary | Ars_Gouda: "far south ... Dairy Plant"; "unlocks the Bestiary" | Relics.cs:81 (Beast Codex, L3 south) + Bestiary also opens on reaching Level 3 (CollectionBook.cs:173-174) | DIFF (minor) | Extra Level-3 route is SH's own. |
| Milky Way Map | pause map | Milky_Way_Map: "far south ... Dairy Plant"; "Unlocks the map in the pause menu" | not built; pause map open from start | DIFF / MISSING | Add relic or record decision. |
| Magic Banger | music selection | Magic_Banger: "north-east ... Green Acres" | Relics.cs:106 (Bard's Echo, L7) gives +5% Greed; music open | DIFF (owner) | - |
| Sorceress' Tears | Hurry | "far south ... Gallo Tower" | Relics.cs:89 (Hourglass Tear, L4 south) +3% Move Speed | DIFF (owner) | - |
| Glass Vizard | merchant everywhere | "Sold by the merchant in Moongolow" | none | MISSING (owner Level 3 merchant) | - |
| Mindbender | favourites, appearance, max weapons | Mindbender: "Fill 50 entries in the Collection"; "change character appearance ..., maximum weapon loadout, and favorite characters" | CollectionBook.cs:48 (50 entries -> favourites) | DIFF | Max-weapon-loadout setting and appearance missing. |
| Yellow Sign | hidden stage items | Yellow_Sign: "Unlocks the Silver Ring, Gold Ring, Metaglio Left, and Metaglio Right"; "Due east of Holy Forbidden" | ItemUnlocks.cs:126-127 Metaglios from start | DIFF (owner opened) | - |
| Forbidden Scrolls of Morbane | Secrets menu | "unlocks the secret menu ... cast spells" | none | MISSING | See Secrets. |
| Great Gospel | Limit Break | "Dropped by the final enemy in Cappella Magna" | Relics.cs:97 (Oathbound Psalm, L5) +3% Might | DIFF (owner) | - |
| Gracia's Mirror / Seventh Trumpet | Inverse / Endless | "Given after entering Eudaimonia Machine" | Relics.cs:110,114 (L8, L9) +3% Growth / +1 Skip | DIFF (owner) | - |
| Randomazzo | Arcanas | "far north ... Gallo Tower"; awards Sarabande | Relics.cs:93 (Fate Deck, L4 north) +5% Luck | DIFF (owner) | - |
| Apoplexy / Wax Fetish / Antidote | Charm / Preserve / Defang | Achievements (per Relics.cs comments) | Relics.cs:71,85,101 unlock those PowerUps | MATCH (placement balance) | - |
| Other EXTRA / DLC relics | Atlas Gate, Chaos Malachite/Rosalia/Lazulia/Altemanna, Trisection, Brave Story, Masquerade, Green Eyes Vizard, Darkasso, maps, Gates, Disks, Aspects ... | RelicNav | none | MISSING | Mostly tied to VS stages/DLC. |
| Collection | what counts / start entries | Collection: "items ... used by the player at least once"; Gold_Coin: Gold Coin, Friendship Amulet, Experience Gem found "before the player starts their first round" | CollectionBook.cs:34-35 (Soul Gem, Soul Coin; no Friendship Amulet) | MATCH | - |
| Collection | size | Collection: "512 items"; Version 1.0 "132 items", EXTRA "63" | SH 105 entries (AGENTS) | DIFF (content-limited) | Grows as items are added. |
| Collection | Seals | Collection: "Seal powerup ... disable in their runs" | Seals left out (AGENTS PowerUp shop) | MISSING | Add Seals + disable UI. |
| Collection unlocks | Mindbender 50, Banish 60-100 | Achievements: "Mindbender | Fill 50 entries"; "Banish 1-5 | Fill 60..100 entries" | CollectionBook.cs:26-48 | MATCH | - |
| Queen Sigma | complete Collection 1.0 | Achievements: "Complete the Collection 1.0" | none | MISSING | VS character (other auditor's area). |
| Achievements | item unlocks | Achievements v1.0 rows (Wings L5, Crown L10, Hollow Heart 1 min, Peachone 10 min, Pentagram 20 min, Bracer Bible 4, Candelabrador Santa Water 4, Duplicator Magic Wand 7, Spellbinder Runetracer 7, Empty Tome 6 weapons, Fire Wand 20 lights, Garlic 5 chickens, Clover/Attractorb/Clock Lancet/Cross by pickup, Lightning Ring 5000 kills, Ebony Wings Peachone 7, Torrona's Box 6 evolutions, Omni Torrona 9) | ItemUnlocks.cs:115-189 | MATCH | Character-tied rows mapped to SH heroes (balance). |
| Achievements | evolution reward 500 gold | Achievements: "Bloody Tear | Evolve the Whip. | 500 gold coins" (every evolution/union row) | Relics.cs:62-66 uses 500 only as relic fallback; no first-evolution gold found (grep) | MISSING | Pay 500 x? gold the first time each evolution/union is made (VS: plain 500). |
| Achievements | Candybox for all evolutions | Achievements: "Candybox | Discover every standard evolution and union" | none | MISSING | Needs Candybox. |
| Achievements | Reroll/Skip ranks | Achievements: "Reroll 1-5 | Reach Level 80 with Mortaccio..."; "Skip 1-5 | Survive 15/30 minutes in Molise ..." | AGENTS VS locked PowerUps (SH heroes / L6-L10) | MATCH (mapped) | - |
| Achievements | Hyper per stage via boss | Achievements: "Hyper Mad Forest | Defeat the giant Blue Venus" etc. | Hyper open from start (owner) | DIFF (owner) | - |
| Achievements | total | Achievements: "total number of achievements is 489" | SH Unlocks list (UnlockTasks.cs) | DIFF (content) | - |
| Secrets menu | hints + spells | Secrets: "unlocked after obtaining the Forbidden Scrolls of Morbane. It shows hints ... and ... cast spells"; "23 base game secrets"; temp unlock "clicking ... the Yellow Sign ... seven times" | none | MISSING | Build with SH-own spells and hints (no-copy rule: never VS spell words). |
| Main-menu code | "aintnobodygottimeforthat" style | Arcanas trivia: typed "while on the main menu" unlocks 31:00 Arcanas | none | MISSING | Own code if wanted. |
| Results | all four areas, DONE, unlock toggle, quit shows results | Results: top-left stage/mode/gold bonus/time/gold/level/kills; weapon level/damage/time/DPS, most in yellow, SI to E; character + Golden Eggs; items (yellow max); PowerUps; pickups; Arcanas; DONE; green unlock button; v0.2.2 quit shows page | Core/Game/RunResults.cs:11-14,44-79,119; UI/HUD/RunResultsScreen.cs | MATCH | - |
| Results | Arcana special damage rows | Results Patch 0.9.0: "shows damage from special Arcanas" | not checked | UNKNOWN | Check Soul Barrage, Final Echo, etc. credit a DamageSource row. |
| Results | mobile ad button | Results: "watch a video advertisement" | none | N/A (no ads) | - |
| Options | sections | Options: "six sections ... Favorites, Display, Sound, Gameplay, Co-op, and Account" | UI/HUD/OptionsMenuUI.cs: Favorites, Display, Sound, Gameplay, Controls, Save Data | DIFF (no Co-op; Account named Save Data; Controls extra) | - |
| Options | Favorites copies | Options Favorites: Language, Sounds, Music, Damage Numbers, Flashing VFX, Classic Sound Effects | Sounds, Music, Damage Numbers, Flashing VFX | DIFF | Language and Classic SFX missing (no content). |
| Options | Display | V-Sync, Borders, Flashing VFX, Disable blood, Weapons ScreenShake, Pixel Font, Show Defanged Enemies; PC: Resolution, Screen Refresh Rate (Exclusive only), Window Mode | V-Sync, Flashing VFX, Disable Blood, Screen Shake, Show Defanged Enemies, Resolution, Refresh Rate, Window Mode | DIFF | Borders and Pixel Font missing. |
| Options | FPS Cap | Options v1.6.0: "Added new display settings (V-Sync toggle, FPS Cap, Refresh Rate)" (not in current list) | fixed 60 cap (DisplaySettingsApplier) | DIFF (wiki unclear) | - |
| Options | mobile Orientation | Options: "Orientation: Portrait (default) / Portrait Inverse / Landscape Left / Landscape Right; Apply Orientation" | auto both orientations | DIFF (owner, 2026-10-10) | - |
| Options | Gameplay | Damage Numbers, Show stage progression, Visually Invert Stages, Touch screen control stick, Joystick Type Legacy/Default | all present (+ Pause When Unfocused extra) | MATCH | - |
| Options | Account | Language, Delete Save "after clicking on the Account section button 6 times", Twitch, DATA RECOVERY; mobile Restore Backup / Restore Purchases; About | Delete Save after six tab presses + Data Recovery | MATCH (Twitch / mobile restore / About missing: N/A) | - |
| Pause menu | options directly without Grimoire/Map | Options: "If the player does not have the Grim Grimoire nor the Milky Way Map, the game options would be immediately accessed upon clicking the pause button" | pause menu always shows (Evolutions + Map open from start) | DIFF (follows owner opening those) | - |
| Main menu | buttons | Main_menu: PRESS TO START; Start; Quick Start ("Unlocking the five main stages"); Online; Adventures; Credits; Bestiary (Ars Gouda); Collection; Power Up; Unlocks; Secrets (Forbidden Scrolls); top: Account, Quit, Options; DLC Store (mobile) | StartPrompt, Start, Quick Start (Level 5 cleared), Credits, Bestiary, Collection, Power Up, Unlocks, Options, Quit (UI/Menus/MainMenuLayout.cs, AGENTS) | DIFF | Secrets missing; Online/Adventures/Account/DLC store N/A. |
| Main menu | Space = Enter | Main_menu Patch 0.2.4: "Spacebar can be used just like Enter" | UiSubmitKeys (AGENTS) | MATCH | - |

## Missing content (VS items with no Soul Hunter counterpart)

**Pickups:** Gold Finger; Rerollo; Sorbetto; Little Heart; Crystallized Soul; Cursed Soul; Scorched Soul; Pie; Cheese; Corn; Survarocchi (pickup); Friendship Amulet (co-op); DLC pickups (Rapid Fire, Barrier, Grenade, Wall Chicken, Heart Refresh, Karma Coin, Mirror of Truth, Neutron Bomb); evolved pickups (Weird Souls Purifier, Starry Heavens, Lavatrix Machina - need weapons SH lacks).

**Chests:** Gold chest, Dark chest, Candybox / Super Candybox II Turbo from Big Coin Bag slots, Quick Treasures option, Yellow Sign silver-chest rule.

**Merchant / economy:** Glass Vizard (merchant on every level), merchant weapons Carréllo, Celestial Dusting, La Robba (+DLC Mille Bolle Blu, Party Popper, Hats), Candybox ware (Queen Sigma), Eggman, Healer, Master Librarian, Giocare, Big Troubler, Glaucus (Darkana merchant), first-evolution 500 gold achievement rewards.

**Arcanas:** all 22 present. Unlock conditions not built (all open).

**Darkanas (13, all missing):** Stake to Your Heart (0), Sapphire Mist (I), Hidden Anathema (III), Pale Diamond Incursion (V), Moonlight Bolero (VI), Edge of the Earth (VIII), Hail from the Future (X), Crystal Cries (XII), Call of a Mad Moon (XIII), Jaws of a Golden Sky (XIV, beta), Victorian Horror (XVIII), Heir of Fate (XIX), Wandering the Jet Black (XXI); plus the Darkasso relic.

**Relics:** Milky Way Map, Glass Vizard, Mindbender as a relic with appearance + max-weapon setting, Yellow Sign, Forbidden Scrolls of Morbane, Eudaimonia Machine; EXTRA: Antipyretic, Atlas Gate, Chaos Malachite/Rosalia/Lazulia/Altemanna, Trisection, Brave Story, Masquerade, Roast Chicken with a Clock in the Middle, Green Eyes Vizard, Astral Stair Map, Survarocchi, Enemahs, Darkasso; DLC maps, Gates, Disks, Ebony and Crimson Stones; the 12 Aspects. (Apoplexy, Wax Fetish, Antidote, Grim Grimoire, Ars Gouda, Magic Banger, Sorceress' Tears, Great Gospel, Gracia's Mirror, Seventh Trumpet, Randomazzo have SH stand-ins.)

**Collection / unlocks:** Seals; Queen Sigma; Candybox-for-all-evolutions; 500-gold evolution rewards; Hyper-by-boss unlocks; secret characters; total 489 achievements vs SH's own list.

**Secrets:** the whole Secrets menu, its 23 base-game hints and spells, the Yellow-Sign seven-click unlock, main-menu typed codes.

**Options:** Language, Classic Sound Effects / Blast Processed, Borders, Pixel Font, FPS Cap, mobile Orientation dropdown (owner chose auto), Co-op section (10 options), Twitch, Restore Backup / Restore Purchases, About.

**Main menu:** Secrets, Online, Adventures, Poncle Account, DLC Store.

## Not checked in this pass

- Arcana Effects/Tips sections line by line (only each card's infobox description was compared; numbers were compared to `ArcanaEffects.cs` constants).
- Every individual relic page beyond the 13 base relics' infobox (EXTRA/DLC relics listed only from RelicNav).
- Achievements EXTRA and DLC sections (only Version 1.0 rows parsed).
- Results: Arcana special-damage rows; Golden Eggs line rendering (code mentions eggs, not seen).
- Soul Egg fractional rounding (wiki silent).
- Bestiary page itself (only Ars_Gouda and Main_menu quotes).
- Coin Greed multiplication in `Pickup` code (assumed from AGENTS).
