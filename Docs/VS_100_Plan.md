# VS 100% plan (owner, 2026-10-27)

Owner: "VS Wiki 100% match hona chahiye, to pahle match karo then plus apna extra thing"; scope: mechanics + rules **and** full content.

Rule from now on: VS behaviour is the core everywhere. Soul Hunter's own features (dash, Soul Burst, the 10-level story campaign, story cards, the Level 3 merchant, own names, art, audio and UI look) stay as **extras on top**, never in place of a VS rule. Where both cannot run at once, VS is the default and the Soul Hunter way becomes an option or a mode.

Source: four audits made on 2026-10-27 from the wiki, fetched that day, against the code at `b9116484`. They are in `Docs/VS_Audit_20261027/`: `weapons.md`, `player.md`, `enemies-stages.md`, `pickups-meta.md`.
- The weapons audit's web tool returned page summaries, so its quotes are close paraphrases. Every row is re-read from the raw page before it is built.
- Each audit lists what it did not check.

## Audit totals

| Area | Rules checked | Match | Differ | Owner-decision differences | Missing rules | Wiki contradicts itself / unknown |
|---|---|---|---|---|---|---|
| Weapons | 74 | 38 | 15 | 16 (aiming) | - | 5 not checked |
| Player, passives, PowerUps, characters | ~115 | 71 | 21 | - | 20 | 3 |
| Enemies, stages, modes | ~80 | 34 | 25 | 15 | 4 | 7 |
| Pickups, chests, meta, menus | ~120 | ~65 | ~31 | (about half of the 31) | ~19 | 6 |

Missing content:
- **Enemies:** 392 in VS, 286 in Soul Hunter. 271 VS entries have no name match, and 250 of those are on stages Soul Hunter does not map.
- **Weapons and evolutions:** about 25 base-game, and 150 or more DLC.
- **Characters:** 225 in VS (56 in the base game); Soul Hunter has 13.
- **Passives:** the DLC and special ones.
- **Other:** 13 Darkanas, several relics, the Gold Finger, Rerollo and Sorbetto pickups, Gold and Dark chests, the Secrets menu, about 26 stages, and some Options.

## Owner answers needed (the wiki contradicts itself)

1. **Armor cap.** The stats list caps Armor at 50. The Armor page caps only the retaliation bonus at 50. Soul Hunter caps the damage reduction at 50 too.
2. **Little Clover.** The Light_source table gives weight 1 with Luck. The Little_Clover infobox gives rarity 0.5 and no Luck.
3. **Hyper enemy speed on Inlaid Library.** The infobox gives x1.25 in both columns; the Enemies page says Hyper speeds enemies up on every stage.
4. **Inverse per stage.** The Inlaid Library and Dairy Plant infoboxes give health x2 and gold +100%; the Stages page gives x3 and +200%.
5. **Limit Break Area card rarity.** The general table gives 5; the Whip page gives 10.

## Work order

### Phase 1: VS rules on what exists (no new art)

**1a. One stage mapping.** Levels 5-7 use Abyss Foscari, Mt.Moonspell and Lake Foscari for every rule: light sources, stage gold, Hyper values and stage bonuses. Today these rules treat Levels 5-10 as Cappella Magna, while the waves already use the three stages above.

**1b. Each stage's own content, from its wiki page:**
- map events per minute;
- chest-boss minutes, tiers and evolution flags;
- traps only on the trap stages, each with its own events;
- stage items: their list, places and extra copies;
- starting spawns;
- far enemies despawn as the wiki says, while bosses are still brought back;
- hazards on their own stages (Stalker on Dairy Plant; Shooting Stars and Pile Assault on Gallo Tower);
- the Cappella Magna events: Maddener, Drowner, Stalker and Trickster.

**1c. Bosses and chest bosses:**
- stage bosses take their statboxes (speed, damage, XP) and their abilities (crab pincers, Orochimario heads);
- chest bosses take their own bodies, for example the Glowing Bat.

**1d. Weapons:**
- stat and behaviour fixes for Song of Mana, Heaven Sword, Unholy Vespers, Knife, Runetracer, Death Spiral, Santa Water, La Borra, Cherry Bomb, Garlic and Soul Eater;
- Limit Break caps per weapon, and Limit Break unlocked by the Great Gospel relic;
- the 500 gold the first time each evolution or union is made.

**1e. Player:**
- Invulnerability Time as a stat (cap 1 s);
- Arcana bonuses beyond the stat caps;
- Max Health keeps its decimals;
- Metaglio Left does not heal the Max Health it adds;
- the Fever PowerUp;
- the v1.6 rule that fills out extra items when fewer than 3 are available.

**1f. Pickups and modes:**
- the Gold Finger, Rerollo and Sorbetto pickups;
- Hyper enemy health;
- chest gold when everything is maxed;
- the Candybox chance in Big Coin Bag slots.

### Phase 2: VS structure back, Soul Hunter as extras

**Aiming.** Each weapon fires as VS does:
- facing side: Whip;
- movement direction: Knife and Thousand Edge;
- random enemy or random direction: for example Fire Wand, Lightning Ring, Runetracer and Bone;
- 12 directions: Clock Lancet;
- diagonals: Gun.

Nearest-enemy aiming becomes an option, for Soul Hunter's own play style.

**Run structure.**
- VS runs: a stage select, one stage per run, the Reaper at 30:00, and stages unlocked as VS unlocks them.
- The 10-level story campaign stays as Soul Hunter's own mode on top, with its guardian rule, story cards and Shadow Kael.

**Unlocks.**
- Modes, Arcanas, Limit Break and relics unlock the VS way.
- Soul Hunter's "open from the start" becomes an option.

**Merchant.**
- The merchant appears where VS places it (Moongolow, then on every stage once the Glass Vizard is bought).
- Soul Hunter keeps its Level 3 merchant as an extra.

**Crowd.** Map events push the hero as VS events do (Swarm, Flower Wall). Ordinary enemy bodies still do not block the hero.

**Characters.**
- The character data grows to hold every VS character mechanic: several stats at once, a bonus every level, starting Reroll/Skip/Banish, two starting weapons, extra levels, and changes at set levels.
- The 13 Soul Hunter characters get their VS counterparts' stat blocks, prices and unlocks.
- Before this is built, the owner approves the counterpart list.

### Phase 3: missing VS content (needs Soul Hunter's own art, owner-approved)

Order: the base game first, then the DLC.
1. weapons, evolutions and unions;
2. passives;
3. characters;
4. enemies and bosses;
5. stages;
6. Darkanas, relics, Seals and the Collection;
7. Secrets (Soul Hunter's own words, not VS spells);
8. Options.

Every item gets a Soul Hunter name, model or icon and sound, made in Blender, and shown to the owner for approval in batches.

## How each row is done

1. Re-read the raw wiki page.
2. Make the smallest change.
3. Add an EditMode or PlayMode test.
4. Run the full EditMode suite and the touched fixtures on the isolated copy.
5. Check the real save is unchanged.
6. Commit.

The full PlayMode suite runs before every push or build.
