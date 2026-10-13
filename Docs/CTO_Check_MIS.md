# Soul Hunter: CTO Check MIS (Start button se game end tak)

Tareekh: 2026-10-13. Yeh list batati hai ki ek CTO ke roop mein main poore game mein kya-kya jaanchunga, kaise jaanchunga, aur pass hone ki rekha kya hai. Yeh abhi sirf **jaanch ki list** hai; jaanch chalane par har row ka nateeja isi file mein bhara jayega.

## Kaise padhen

- **Kaise:** T = automatic test, B = bot ka khel (build mein, screenshots aur log ke saath), M = haath se khelna, P = profiler ya benchmark, D = phone ya doosri machine par.
- **Aaj ki sthiti:**
  - `OK (test)`: automatic test pass hai, aankh se nahi dekha.
  - `OK (dekha)`: build ke screenshot ya log mein dekha.
  - `MASLA`: gadbad mili hai, tippani mein likhi hai.
  - `BAAKI`: abhi jaancha nahi.
- **Gambhirta:** S1 = game ruk jaye ya save bigde, S2 = khel bigde, S3 = dikhne ya sunne mein kharab, S4 = chhota sudhaar.

## Saar (aaj jo pata hai)

| Hissa | Rows | OK | MASLA | BAAKI |
|---|---|---|---|---|
| A. Shuruaat aur loading | 6 | 2 | 0 | 4 |
| B. Main menu aur uski screens | 10 | 8 | 0 | 2 |
| C. Character aur mode chunna | 7 | 5 | 0 | 2 |
| D. Run ki shuruaat | 5 | 4 | 0 | 1 |
| E. Khel ka mool chakra | 14 | 10 | 2 | 2 |
| F. Run ke andar UI | 12 | 10 | 1 | 1 |
| G. Events, hazards, Omens | 8 | 6 | 1 | 1 |
| H. Boss aur agla level | 6 | 3 | 1 | 2 |
| I. Har level ki apni cheezein | 10 | 6 | 2 | 2 |
| J. Run ka ant | 6 | 4 | 0 | 2 |
| K. Sound | 10 | 3 | 0 | 7 |
| L. VFX aur dikhavat | 11 | 4 | 3 | 4 |
| M. Lag, memory aur performance | 21 | 5 | 3 | 13 |
| N. Save aur data | 8 | 6 | 0 | 2 |
| O. Controls aur devices | 7 | 3 | 0 | 4 |
| P. Phone | 8 | 4 | 0 | 4 |
| Q. Build aur sthirta | 7 | 3 | 0 | 4 |
| R. Text, naam, kanooni | 6 | 3 | 1 | 2 |
| S. Kinare ke maamle (ek saath do cheezein) | 12 | 6 | 0 | 6 |
| T. Camera, takkar aur bhautiki | 7 | 4 | 0 | 3 |
| U. Run modes, Arcanas, items: har ek alag se | 12 | 10 | 0 | 2 |
| V. Santulan aur arthvyavastha | 8 | 1 | 1 | 6 |
| W. Pahunch aur aaram (accessibility) | 6 | 2 | 0 | 4 |
| X. Naye khiladi ka pehla anubhav | 5 | 3 | 0 | 2 |
| Y. Code aur project ki sehat | 10 | 1 | 3 | 6 |
| Z. Release ki taiyaari | 10 | 0 | 0 | 10 |
| **Kul** | **232** | **116** | **18** | **98** |

Sabse bade khaali hisse: **Release ki taiyaari** (10 mein 10 baaki), **Lag, memory aur performance** (21 mein 13 baaki) aur **Sound** (aaj tak kisi ne game suna nahi hai, sirf tests kehte hain ki clip baj rahi hai).

---

## A. Shuruaat aur loading

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| A1 | Icon par click se main menu tak ka samay | B, P | 10 second se kam (reference laptop) | S3 | BAAKI | Kabhi naapa nahi |
| A2 | Splash aur loading screen sahi dikhti hai, kuch kata hua nahi | M | Har window size par poori tasveer | S3 | BAAKI | Loading sirf ek tasveer hai |
| A3 | Bootstrap se Loading se MainMenu ka kram | T | Har baar yahi kram | S1 | OK (test) | `BootstrapPlayModeTests` |
| A4 | Pehli baar chalane par (koi save nahi) game khulta hai | T | Naya save banta hai, koi error nahi | S1 | OK (test) | `SaveServiceTests` |
| A5 | Pehle launch par shader ya cache ka jhatka | P | 250 ms se bada koi frame nahi | S3 | BAAKI | Ek baar 152 ms dekha tha |
| A6 | Window chhoti-badi karne, alt-tab par game theek rehta hai | M | Na crash, na kaali screen | S2 | BAAKI | Asli alt-tab kabhi nahi jaancha |

## B. Main menu aur uski screens

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| B1 | Nau buttons frame ke andar, ek size, barabar gap | T | Koi overlap nahi, sab screen par | S3 | OK (test) | Layout par aapki manzoori baaki |
| B2 | Start prompt aur Quick Start (Level 5 ke baad khulta hai) | T | Band ho to saaf likha ho kyun | S2 | OK (test) | |
| B3 | Power Up dukaan: kharid, daam, Refund, ON/OFF | T | Daam wiki ke formula se, gold sahi kate | S2 | OK (test) | |
| B4 | Options: har switch sach mein asar karta hai | T | Sound, shake, numbers, blood sab badlen | S2 | OK (test) | |
| B5 | Window Mode aur Resolution build mein lagte hain | M | Chuna hua mode turant lage | S2 | BAAKI | Editor mein nahi dikhta, build mein kabhi nahi jaancha |
| B6 | Collection: 105 entries, na mili ho to "?" | T | Ginti sahi, koi entry do baar nahi | S3 | OK (test) | |
| B7 | Bestiary, Unlocks, Credits khulte aur band hote hain | T | Back se wahi button chuna rahe | S3 | OK (test) | |
| B8 | Save Data tab: Recovery aur Delete (do baar poochhna) | T | Galti se delete na ho sake | S1 | OK (test) | |
| B9 | Har screen par likhaayi 4.5:1 contrast se padhi jaye | T, M | Koi halka ya kata hua text nahi | S3 | OK (test) | Aankh se sab screens nahi dekhi |
| B10 | Bahut chaudi (21:9) aur chhoti window mein menu | M | Kuch screen ke bahar nahi | S3 | BAAKI | |

## C. Character aur mode chunna

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| C1 | 13 character cards ek saath dikhte hain | T | Sab grid ke andar | S3 | OK (test) | |
| C2 | Band character: silhouette aur shart; khula: daam; Buy | T | Galat character se run shuru na ho | S2 | OK (test) | |
| C3 | Stat panel asli numbers dikhata hai (PowerUp, Soul Egg samet) | T | Run ke andar wahi numbers milen | S2 | OK (test) | |
| C4 | Chhe run modes ke switch save hote aur run mein lagte hain | T | Har mode ka asar run mein dikhe | S2 | OK (test) | |
| C5 | Sort, filter, favourite | T | Koi character gayab na ho | S4 | OK (test) | |
| C6 | Music card: track, speed, Keep | M | Chuna hua gaana hi baje | S3 | BAAKI | Suna nahi gaya |
| C7 | Portrait tasveerein saaf aur roshan | M | Chehra pehchana jaye | S3 | BAAKI | Info box ka portrait bahut gehra tha |

## D. Run ki shuruaat

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| D1 | Confirm se pehla frame: zameen har kinare tak | T | Kahin khaali background nahi | S2 | OK (test) | |
| D2 | Hero apne model aur shuruaati weapon ke saath | T | Sab 13 character | S2 | OK (test) | |
| D3 | Story card Level 1 par, bina game roke | T | Menu khule to card chhup jaye | S3 | OK (test) | |
| D4 | Pehle 10 second mein enemy screen ke bahar se aate hain | T | Koi enemy screen ke andar na ubhre | S2 | OK (test) | |
| D5 | Pehle 30 second ka ehsaas: na bahut khaali, na bahut bheed | M | Aapki raay | S3 | BAAKI | |

## E. Khel ka mool chakra

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| E1 | Chalna, mudna, dash; obstacle par fisalna | T | Hero atke nahi, deewar ke paar na jaye | S2 | OK (test) | Phone par haath se nahi dekha |
| E2 | Har weapon (base aur evolution) enemy ko chot deta hai | T | 0 error, sab weapon | S1 | OK (test) | |
| E3 | Koi weapon hero ko khud chot nahi deta | T | Hero ki health na ghate | S1 | OK (test) | |
| E4 | Duplicator har weapon par asar dikhata hai | T | 2 zyada shot, ek ke upar ek nahi | S2 | OK (test) | |
| E5 | Enemy ka contact damage aur 240 ms ki suraksha | T | Ek hit ke baad turant doosra na lage | S2 | OK (test) | |
| E6 | Koi cheez hero ko har frame damage na de | B | Health achanak 1-1 karke na gire | S1 | MASLA | Stage 9 ka ghera: theek kiya, commit baaki |
| E7 | XP gem, coin, chicken uthaye jate hain; Magnet kaam karta hai | T | Kuch zameen par atka na rahe | S2 | OK (test) | |
| E8 | Level-up screen: game rukta hai, card chunne par chalta hai | T | Kabhi atka hua pause nahi | S1 | OK (test) | |
| E9 | Chest screen: OPEN, DONE, skip ke niyam | T | Har window size par button dikhe | S1 | OK (test) | |
| E10 | Revive screen: 50% health, phir suraksha | T | Game atke nahi | S1 | OK (test) | |
| E11 | Chhupe items ke guardians: nyaaypoorn ya nahi | B, M | Hero ko sambhalne ka mauka mile | S2 | MASLA | 50 damage: do hit mein maut (Stage 4, 8, 9 ke bot run) |
| E12 | Passive items sach mein stat badalte hain | T | Stats panel mein fark dikhe | S2 | OK (test) | |
| E13 | Difficulty ka dhalaan Level 1 se 10 | B, M | Koi level bina chot ya turant maut wala nahi | S2 | BAAKI | Bot: Level 2, 3, 5, 6 par ek bhi chot nahi |
| E14 | 30 minute lagatar khelne par kuch nahi tootta | B | 0 error, koi atka pause nahi | S1 | BAAKI | Naye code par poora 30 minute nahi khela |

## F. Run ke andar UI

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| F1 | Kills, Gold, Time, Level saaf padhe jayen | T | Safed text, gehri plate | S3 | OK (dekha) | |
| F2 | Item slots: 6 weapon, 6 passive, sahi icon | T | Evolution par sunehri kinara | S3 | OK (dekha) | |
| F3 | HP bar hero ke neeche, chhoti | T | Hero ke saath chale | S3 | OK (dekha) | |
| F4 | XP bar aur "LV" | T | Level-up par sahi ank | S3 | OK (test) | |
| F5 | Damage numbers padhe jayen, dher na lage | T, B | Hero ko na dhaken | S3 | OK (test) | |
| F6 | Notice (unlock, guardian ki soochna) kisi cheez par na chadhe | T | Stage ke naam aur slots se alag | S3 | OK (test) | Guardian wali soochna aankh se nahi dekhi |
| F7 | Pause menu: Resume, Evolutions, Map, Options, Quit | T | Esc dobara dabane par bhi theek | S2 | OK (test) | |
| F8 | Pause ka Map: items, casket, relic, merchant sahi jagah | T | Nishaan asli jagah se milen | S3 | OK (test) | Marker ka art placeholder |
| F9 | Armoury Cache ki weapon list | T, B | Card mein icon, naam, varnan | S2 | MASLA | Test pass, par build ke screenshot mein dibba nahi dikha |
| F10 | Arcana chunne ki screen (shuruaat, chest, merchant) | T | Reroll, Skip, Random ke niyam | S2 | OK (test) | |
| F11 | Merchant ki dukaan (Level 3) | T | Gold sahi kate, slot ke niyam | S2 | OK (test) | Screenshot nahi liya |
| F12 | Har in-game screen controller se chal sake | M | Har button focus mein aaye | S2 | BAAKI | Asli controller par kabhi nahi |

## G. Events, hazards, Omens

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| G1 | Map events (swarm, wall, ring) ghadi par aate hain | T | Screen ke bahar se shuru | S2 | OK (test) | |
| G2 | Chest elite 16 baar, sahi minute par | T | Har ek chest giraye | S2 | OK (test) | |
| G3 | Soul Lantern: banna, tootna, drop | T | Seema se zyada na banen | S3 | OK (test) | |
| G4 | Rune Plate, rail cart, Soul Meteor, Pile Assault | T | Har ek apna kaam kare | S3 | OK (test) | |
| G5 | Death's Shadow: sirf instant kill se mare | T | Aam weapon se na mare | S2 | OK (test) | |
| G6 | 27 Omens: har ek chalta hai, koi error nahi | T, B | Sab 27 | S2 | OK (dekha) | Aakhiri 7 aaj bane; 6 screenshot mein dikhe |
| G7 | Thorn Circle sach mein hero ko rokta hai | M | Hero bahar na nikal sake jab tak ek na mare | S2 | MASLA | Test sirf collider dekhta hai; chalkar nahi jaancha |
| G8 | Omen ka pahiya (roulette) UI | M | Chuna hua event saaf dikhe | S3 | BAAKI | |

## H. Boss aur agla level

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| H1 | Boss 25:00 par aata hai, door chhootne par wapas aata hai | T | Har level | S2 | OK (test) | |
| H2 | Boss marne par chest aur soul gem | T | Dono giren | S2 | OK (test) | |
| H3 | Naya niyam: boss 30:00 se pehle mara to agla level | T, B | Level 3 se 4 build mein hua | S1 | OK (dekha) | Aaj bana |
| H4 | 5 minute mein boss maarna mumkin hai, har level par | B, M | Aam loadout se 3 minute ke andar | S1 | MASLA | Level 5 ka boss (90,000 health) 5 minute mein nahi mara |
| H5 | Boss na mare to Results par saaf kaaran | T | "Level n+1 stays closed" dikhe | S2 | BAAKI | Test likha hai, aankh se nahi dekha |
| H6 | Level badalne ka pal: purane enemy, items, hazards saaf | T, B | Naye level par purana kuch na bache | S2 | BAAKI | Naye hazards (paani, kaante) ke saath nahi jaancha |

## I. Har level ki apni cheezein

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| I1 | Level 1: 16 enemy type minute ke hisaab se | T | Wiki ki table | S2 | OK (test) | Aapne khela: "thik hai" |
| I2 | Level 2: andhera aankh ko na thakaye | M | 30 minute aaram se | S3 | BAAKI | Kahani "andhera" kehti hai, aaram ka niyam alag: aapka faisla |
| I3 | Level 3: Merchant khuli jagah par, dukaan chalti hai | T | Kabhi obstacle par nahi | S2 | OK (dekha) | |
| I4 | Level 4: galiyara, deewarein, enemy donon siron se | T, B | Hero deewar ke paar na jaye | S2 | MASLA | Bot deewar se chipakkar 1:23 par mara |
| I5 | Level 5: dheema bhaari horde | T | Taye raftaar | S3 | OK (test) | |
| I6 | Level 6: 4 second ki dheemi chaal ka koi sanket | M | Khiladi ko pata chale kyun dheema hua | S3 | BAAKI | Screen par koi nishaan nahi |
| I7 | Level 7: bhoot sirf Holy ya Magic se maren | T | Normal weapon paar ho jaye | S2 | OK (test) | Har 15 s ka 3 damage bina chetavni |
| I8 | Level 8: sirf elite; floor ke jod | T, B | Tiles ke beech rekha na dikhe | S3 | MASLA | Tiles ke jod screenshot mein saaf dikhte hain |
| I9 | Level 9: safe zone dikhta hai, bahar 10 damage/second | T, B | Ring dikhe, hero turant na mare | S1 | OK (dekha) | Aaj theek kiya, commit baaki |
| I10 | Level 10: koi samay-seema nahi, Shadow Kael loadout ki nakal | T | Uski maut par hi jeet | S1 | OK (test) | Raaste chamakte cyan hain (aankh ko chubhte) |

## J. Run ka ant

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| J1 | Maut par Results, DONE se main menu | T | Doosra run bhi chale | S1 | OK (test) | |
| J2 | Pause se Quit par bhi Results | T | Score chhoote nahi | S2 | OK (test) | |
| J3 | Results ke ank: damage, DPS, samay, pickups | T | Asli hits se milen | S2 | OK (dekha) | |
| J4 | Jeet ki screen: kahani ki aakhiri pankti | T | GDD ke shabd | S2 | OK (test) | Khel mein kabhi nahi dekhi |
| J5 | Poora run Level 1 se 10 tak, ek baar | B, M | Bina error ke ant tak | S1 | BAAKI | Kabhi ek saans mein poora nahi khela (5 ghante) |
| J6 | Ant ke baad time scale, sound, pause sab reset | M | Agla run saaf shuru ho | S1 | BAAKI | Tests mein hai, haath se nahi |

## K. Sound

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| K1 | Har effect ki clip lagi hai aur khaamosh nahi | T | Sab clips | S2 | OK (test) | |
| K2 | Menu aur har level ka apna music | T | Level badalne par gaana badle | S3 | OK (test) | |
| K3 | Pehli baar bajne par jhatka nahi | T, P | Koi clip disk se der se na aaye | S3 | OK (test) | |
| K4 | Awaazon ka santulan (music, weapon, hit, UI) | M | Koi awaaz doosri ko na dabaye | S3 | BAAKI | **Kisi ne aaj tak suna nahi** |
| K5 | Bheed mein awaaz phatti ya kaan-phodu nahi | M | 300 enemy par bhi saaf | S2 | BAAKI | |
| K6 | Music ka loop bina "click" ke | M | Jod sunai na de | S3 | BAAKI | |
| K7 | Har weapon ki awaaz us weapon se milti hai | M | Aapki raay | S3 | BAAKI | Awaazein ank dekhkar chuni gayi thi, sunkar nahi |
| K8 | Pause, level-up, chest par music ka vyavhaar | M | Na achanak band, na dohra | S3 | BAAKI | |
| K9 | Sounds aur Music slider 0 par poori khaamoshi | M | Kuch na baje | S3 | BAAKI | |
| K10 | Phone ke speaker aur headphone par | D | Dono par saaf | S3 | BAAKI | |

## L. VFX aur dikhavat

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| L1 | Har weapon ka apna effect (koi placeholder gola nahi) | T | Sab weapon | S3 | OK (test) | |
| L2 | Koi effect hero ko na dhake | B | Hero hamesha dikhe | S2 | OK (dekha) | Kaala dhuaan aaj hataya |
| L3 | Coin asli sone jaisa dikhe | B, M | Motai, kinara, chamak | S3 | MASLA | Sapaat peela tukda dikhta hai; kaam shuru nahi hua |
| L4 | Har enemy type alag pehchana jaye | M | Rang ke alawa bhi fark | S3 | MASLA | Ek hi model alag rang mein (placeholder) |
| L5 | Level 6: enemy aur zameen alag dikhen | B | Saaf contrast | S3 | MASLA | Dono halke neele |
| L6 | Khoon ka spray: 300 particle, zameen par kuch nahi | T | Disable Blood par band | S3 | OK (test) | |
| L7 | Marta shareer doobta hai, phir gayab | T | 0.85 second mein gayab | S4 | OK (test) | |
| L8 | Pause mein sirf chest aur level-up ke effect chalen | T | Baaki sab ruke | S3 | BAAKI | Naye effects ke saath dobara nahi dekha |
| L9 | Zameen ka rang aankh ke liye aaramdeh, har level | M | Na bahut gehra, na chamak | S3 | BAAKI | Level 10 ke raaste chubhte hain |
| L10 | Naye placeholder art (guardian, cache, paani, relic, casket) | M | Aapki manzoori | S3 | BAAKI | Sab bina manzoori ke |
| L11 | Hero ka animation: chalna, vaar, mudna | M | Smooth, bina jhatke | S3 | BAAKI | Haath se nahi dekha |

## M. Lag, memory aur performance

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| M1 | Aam khel ka FPS, har level | B | Median 60, kabhi 30 se neeche nahi | S2 | OK (dekha) | 23 bot runs: median 60 |
| M2 | 300 enemy par FPS | P | Average 60, 1% low 30 se upar | S2 | OK (dekha) | Purana benchmark: 76-88 |
| M3 | Pentagram chalne par jhatka | B | 100 ms se bada frame nahi | S2 | MASLA | 650-800 ms har 60 second; ilaaj likha, naapa nahi |
| M4 | Bahut tez killing (level 100+ hero) | P | 30 FPS se neeche nahi | S2 | MASLA | Level 7 der se: median 43, sabse kam 27 |
| M5 | Chest kholne, level-up par jhatka | B | 250 ms se kam | S3 | MASLA | Chest par 333 ms dekha |
| M6 | Poore 30 minute mein FPS girta nahi jaata | B | Shuru aur ant mein barabar | S2 | OK (dekha) | 10 Oct ka run; naye code par dobara baaki |
| M7 | Managed memory (heap): 30 minute mein badhti nahi jaati | P | Shuru aur ant ka fark 20 MB se kam | S2 | BAAKI | 6 minute ke runs mein 8 se 18 MB tak badhi; 30 minute par naapa nahi |
| M8 | 8 GB RAM wali machine | D | Wahi rekha | S2 | BAAKI | Sirf 16 GB par naapa |
| M9 | Asli 1920x1080 fullscreen | P | Wahi rekha | S2 | BAAKI | Window mein naapa |
| M10 | Naye Omens ka bojh (paani, kaante, takeover) | P | Koi naya jhatka nahi | S3 | BAAKI | |
| M11 | Loading ka samay level badalne par | B | 3 second se kam | S3 | BAAKI | |
| M12 | Garbage collection ke jhatke | P | GC se 16 ms se bada koi frame nahi | S2 | OK (dekha) | Aaj ke har jhatke ke log mein GC ki ginti 0 thi: jhatke GC ke nahi the |
| M13 | Har frame ka kachra (allocation) | P | Aam khel mein 1 KB per frame se kam | S3 | BAAKI | Kabhi naapa nahi; yahi GC ko bulata hai |
| M14 | GC kitni baar chalta hai (gen 0, 1, 2) | B, P | Gen 2 poore level mein gine-chune baar | S3 | BAAKI | Bot ginti likhta hai, par kisi ne 30 minute ka jod nahi dekha |
| M15 | Zinda objects ki ginti badhti nahi jaati (pool ka leak) | B | 30 minute mein sthir (7,000-9,000) | S1 | OK (dekha) | 29 Sep ko 30,000 tak badhti thi, theek kiya; aaj ke runs mein 4,400-8,300 |
| M16 | Pool wapas lete hain: enemy, projectile, pickup, VFX, damage number | T, P | Koi cheez pool ke bahar na banti rahe | S2 | BAAKI | Naye Omens (kaante ke khambhe, paani, weapon cards) ke saath nahi jaancha |
| M17 | Run khatam karke menu par lautne par memory wapas milti hai | P | Menu par memory pehle launch jaisi | S2 | BAAKI | Kabhi naapa nahi |
| M18 | Paanch run lagatar: har run ke baad memory | P | Run-dar-run badhat nahi | S1 | BAAKI | Kabhi naapa nahi |
| M19 | Level badalne par purane level ka maal (floor, enemy pool, textures) chhootta hai | P | Level 10 par memory Level 1 se dugni se kam | S2 | BAAKI | Das level ke enemy pool ek run mein jamaa hote jaate hain: sandeh hai |
| M20 | Textures aur audio ki memory (native) | P | Reference laptop ke 8 GB mein aaram se | S2 | BAAKI | 30 HD icons 512 px bina compression ke; music stream hota hai |
| M21 | Phone par memory aur "low memory" ki chetavni | D | App band na ho | S1 | BAAKI | |

## N. Save aur data

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| N1 | Save atomic hai; kharab file par pichhla save lautta hai | T | Kabhi shoonya se shuru na ho | S1 | OK (test) | |
| N2 | Purana save naye build mein khulta hai | T | Gold, unlocks bache rahen | S1 | OK (test) | |
| N3 | Tests aur bot asli save ko nahi chhoote | T | Hash na badle | S1 | OK (test) | Aaj ke har run mein jaancha |
| N4 | Gold, unlocks, Collection, Bestiary sahi jud te hain | T | Do baar na ginen | S2 | OK (test) | |
| N5 | Run ke beech band karne par kya bachta hai | M | Kamaya gold na jaye | S2 | BAAKI | 2 second ki der se save hota hai |
| N6 | Bijli jaane ya zabardasti band karne par | M | Save na bigde | S1 | BAAKI | Asli haalat mein kabhi nahi |
| N7 | Delete Save sach mein sab mitata hai, backup rakhta hai | T | `.deleted.bak` bane | S2 | OK (test) | |
| N8 | Options (awaaz, controls) save hote hain | T | Agle launch par wahi | S3 | OK (test) | |

## O. Controls aur devices

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| O1 | Keyboard: chalna, dash, ultimate, pause | T | Sab kaam karen | S1 | OK (test) | |
| O2 | Keys badalna aur Reset | T | Agle launch par bhi rahe | S3 | OK (test) | |
| O3 | Controller ke buttons | T | Start se pause | S2 | OK (test) | Nakli controller se |
| O4 | Asli controller (Xbox, PlayStation) | D | Har screen chale | S2 | BAAKI | |
| O5 | Mouse se har button | M | Hover aur click sahi | S3 | BAAKI | |
| O6 | Focus khone par pause (alt-tab) | M | Wapas aane par theek | S2 | BAAKI | |
| O7 | Do monitor, alag refresh rate | D | Na phate, na dheema | S3 | BAAKI | |

## P. Phone

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| P1 | Seedha aur aada, dono mein har screen | T, D | Kuch kata nahi, 48 dp se chhota button nahi | S2 | OK (dekha) | 12 Oct |
| P2 | Joystick ungli ke neeche aata hai | D | Har jagah | S2 | OK (dekha) | |
| P3 | Phone par 60 FPS (shuruaati khel) | D | 58-60 | S2 | OK (dekha) | |
| P4 | Phone par 300 enemy | D | 30 se neeche nahi | S2 | BAAKI | |
| P5 | Garmi aur battery, 30 minute | D | Phone bahut garam na ho | S2 | BAAKI | |
| P6 | Call ya notification aane par | D | Game ruke, wapas chale | S2 | BAAKI | |
| P7 | Naye screens (weapon list, guardian ki soochna) phone par | D | Padhe jayen | S3 | BAAKI | Aaj bane |
| P8 | App icon aur naam | D | Sahi dikhe | S4 | OK (dekha) | |

## Q. Build aur sthirta

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| Q1 | Windows build banta hai, 0 compile error | B | Har baar | S1 | OK (dekha) | Aaj do baar |
| Q2 | Android build banta hai | B | Har baar | S1 | BAAKI | Aaj ke code par nahi banaya |
| Q3 | Poore test suite | T | Sab pass, 0 exception | S1 | OK (test) | 927 + 416 pass; naye code par poora run chal raha tha |
| Q4 | Player log mein koi error ya warning ki baadh | B | 0 error | S2 | OK (dekha) | Aaj ke 23 runs mein 0 error |
| Q5 | Build ka size | B | Tay seema ke andar | S4 | BAAKI | 142 MB tha; seema tay nahi |
| Q6 | Saaf machine par install aur pehla launch | D | Bina kisi extra install ke chale | S1 | BAAKI | |
| Q7 | Do ghante lagatar chalane par crash nahi | B | 0 crash | S1 | BAAKI | |

## R. Text, naam, kanooni

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| R1 | Game mein dikhne wala koi naam VS ka nahi | T, M | Sab apne naam | S1 | MASLA | Kai items ab bhi VS ke naam se dikhte hain (jaise Santa Water, Peachone, Hollow Heart); aapka 9 Oct ka faisla: naam nahi badlenge |
| R2 | Kahani ki panktiyan GDD se shabd-ba-shabd | T | Ek akshar ka fark nahi | S2 | OK (test) | |
| R3 | Spelling aur ek jaisa likhna (Power Up, Level) | M | Har screen par ek jaisa | S4 | BAAKI | |
| R4 | Audio aur font ke credits | T | Har file ka naam Credits mein | S1 | OK (test) | |
| R5 | Koi VS ki file, tasveer ya data project mein nahi | M | Shoonya | S1 | OK (test) | Aapki install ki hui VS ki files kholi nahi gayin |
| R6 | Naye naamon par aapki manzoori | M | Har naam | S3 | BAAKI | Guardians, 7 Omens, Cursed Coffer, Armoury Cache |

## S. Kinare ke maamle (ek saath do cheezein)

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| S1 | Level-up ke dauran chest, ya chest ke dauran level-up | T | Dono screen ek ke baad ek, koi atka pause nahi | S1 | OK (test) | `GameTime` ginti se pause sambhalta hai |
| S2 | Level-up ya chest khuli ho aur hero mar jaye | M | Revive ya Results sahi aaye | S1 | BAAKI | |
| S3 | Pause ke dauran level badalne ka pal (30:00) | T, M | Ghadi ruki rahe, level pause ke baad badle | S1 | BAAKI | |
| S4 | Boss ki ladai ke beech 30:00 (naya niyam) | T, B | Run khatam, koi adhoora transition nahi | S1 | OK (dekha) | Level 5 ke bot run mein hua |
| S5 | Weapon list ya Arcana screen khuli ho aur doosri screen aaye | T | Ek samay par ek hi chunaav | S2 | OK (test) | Doosra chunaav khula ho to cache pehla weapon de deta hai |
| S6 | Omen chal raha ho aur level badal jaye | B | Paani, kaante, khambhe sab hat jayen | S2 | BAAKI | |
| S7 | Do Omens ek saath (Rootbound ke upar Nightfall) | M | Koi enemy "adha-adha" na bane | S2 | BAAKI | Doosra takeover pehle ko badal deta hai: jaancha nahi |
| S8 | Hurry mode mein ghadi dugni: sab events sahi samay par | T | Boss, chest elite, 30:00 | S2 | OK (test) | Stage 9 ka ghera apni ghadi par chalta hai: Hurry mein dheere sikudega |
| S9 | Time Freeze ke dauran naye enemy, fliers, khade enemy | T | Freeze ke baad sab apna kaam dobara karen | S2 | OK (test) | |
| S10 | Bahut tez level-up ki ladi (ek saath 30 level) | B | Har screen apna level dikhaye, FPS na gire | S2 | OK (dekha) | 1 Oct |
| S11 | Dash deewar, obstacle, kaanton ke khambhe ke paar nahi jaata | T, M | Hero kabhi andar na phanse | S1 | BAAKI | Kaanton ke khambhon ke saath dash nahi jaancha |
| S12 | Hero obstacle aur badhte paani ke beech phans jaye | M | Nikalne ka raasta hamesha ho | S2 | BAAKI | |

## T. Camera, takkar aur bhautiki

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| T1 | Camera hero ke saath smooth chalta hai, kaanpta nahi | T, M | Koi jhatka nahi | S2 | OK (test) | |
| T2 | Screen Shake band karne par sach mein band | T | Shoonya hilna | S3 | OK (test) | |
| T3 | Tez projectile enemy ke paar bina chot ke nahi nikalta | M | Har tez weapon | S2 | BAAKI | |
| T4 | Enemy ek doosre par dher nahi lagate | T | Bheed chaudi rahe | S2 | OK (test) | God mode mein 300 enemy hero par ek gola ban gaye (screenshot) |
| T5 | Hero aur enemy zameen par khade dikhte hain, hawa mein nahi | M | Pair zameen par | S3 | BAAKI | Hero y = 1 par, floor y = 0 par: kabhi aankh se nahi jaancha |
| T6 | Parchhaiyan sahi (hero, boss, obstacle) | T | Horde ki nahi, baaki ki haan | S4 | OK (test) | |
| T7 | Bahut door chalne par (1,000+ unit) sab theek | B | Floor, spawn, ghere, items | S2 | BAAKI | Kabhi nahi jaancha |

## U. Run modes, Arcanas, items: har ek alag se

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| U1 | Hyper, Hurry, Inverse, Endless: har ek ka ank | T | Wiki ke ank | S2 | OK (test) | Bot kabhi mode chalu karke nahi khela |
| U2 | Endless: Level 10 ke baad Level 1, har chakkar kathin | T | Chakkar ki ginti badhe | S2 | OK (test) | Khel mein kabhi nahi dekha |
| U3 | Arcana mode: shuruaat ka chunaav, 11:00 aur 21:00 ke chest | T | 3 Arcana tak | S2 | OK (test) | |
| U4 | 22 Arcana: har ek ka asar | T | Har card | S2 | OK (test) | 8 card real run mein, baaki sirf niyam ke test |
| U5 | Limit Break ke cards | T | Har weapon ki apni seema | S3 | OK (test) | |
| U6 | Blind Fate (apne aap level-up) | T | Koi screen nahi, item hero ke upar dikhe | S3 | OK (test) | |
| U7 | 14 evolutions aur 2 unions: recipe se bante hain | T | Base weapon ki jagah len | S2 | OK (test) | |
| U8 | 13 character: har ek ki khaas kshamta | T | Varnan jo kehta hai wahi ho | S2 | OK (test) | |
| U9 | 11 relic aur 7 Soul Casket: milte hain, save hote hain | T | Var hamesha rahe | S2 | OK (test) | Build mein kabhi nahi dekhe |
| U10 | Item unlock ki 20+ sharten poori ki ja sakti hain | T, M | Koi shart asambhav nahi | S2 | BAAKI | Har shart ko khelkar nahi parkha |
| U11 | Modes ko milakar (Hyper + Inverse + Endless) | M | Ank jud te hain, koi overflow nahi | S2 | BAAKI | |
| U12 | Soul Egg: 19 stat, har ek ki seema | T | Agle run se lage | S3 | OK (test) | |

## V. Santulan aur arthvyavastha

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| V1 | Ek run mein kitna gold milta hai banaam dukaan ke daam | B | Pehla PowerUp 1-2 run mein | S2 | BAAKI | Level-130 ke bot run mein 16 second mein 1,680 gold tha: sandeh |
| V2 | Level ki raftaar: 30 minute mein hero ka level | B | VS ke aas-paas (lagbhag 50-60) | S2 | OK (dekha) | Level 1 par 30 minute mein level 50 |
| V3 | Har level par maut ka jokhim | B, M | Na shoonya, na pakki maut | S2 | BAAKI | E13 ke saath |
| V4 | Boss ki health banaam hero ki taakat, har level | B | 1 se 3 minute | S1 | MASLA | H4 ke saath |
| V5 | Healing kahan se milti hai aur kitni | B | Har 2-3 minute mein ek mauka | S2 | BAAKI | Sirf lantern, stage item, Recovery |
| V6 | Curse wale items (Coffer level 9, Skull) khel ko tod te nahi | B | Chunaav ho, jaal nahi | S2 | BAAKI | Coffer ne boss ki health dugni ki aur run khatam hua |
| V7 | Character ke daam aur unlock ki raftaar | M | Har 1-2 ghante mein kuch naya | S3 | BAAKI | |
| V8 | Bahut majboot loadout (Pentagram, Soul Toll) khel ko bekaar nahi karte | B | Level phir bhi chunauti de | S3 | BAAKI | |

## W. Pahunch aur aaram (accessibility)

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| W1 | Rang-andhta: koi jaankari sirf rang se nahi | T, M | Shabd ya aakaar bhi ho | S3 | OK (test) | UI mein; khel ke andar gem ke tier sirf rang se |
| W2 | Tez chamak (photosensitivity): Flashing VFX band | T | Koi poori screen ki chamak nahi | S2 | OK (test) | Pentagram ki chamak 0.3 s ki hai: band hone par jaanchni hai |
| W3 | Likhaayi ka size, sabse chhoti window par | M | 12 se chhoti nahi | S3 | BAAKI | |
| W4 | Ek haath se ya sirf keyboard se khelna | M | Poora game | S3 | BAAKI | |
| W5 | Lamba khelne par aankh ki thakaan (zameen, effects) | M | 30 minute aaram se | S3 | BAAKI | Aapki raay chahiye |
| W6 | Awaaz ke bina bhi khela ja sake (har sanket dikhta bhi ho) | M | Boss, chetavni screen par bhi | S3 | BAAKI | Level 6, 7, 10 ke khatre ka koi nishaan nahi |

## X. Naye khiladi ka pehla anubhav

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| X1 | Bina bataye samajh aaye ki kya karna hai | M | Pehle 1 minute mein | S2 | BAAKI | Koi tutorial nahi (VS mein bhi nahi) |
| X2 | Controls kahin dikhte hain | M | Options mein mil jayen | S3 | OK (test) | |
| X3 | Naya niyam (boss maaro) khiladi ko samay par pata chale | M | 25:00 se pehle bhi sanket | S2 | BAAKI | Abhi sirf boss aane par soochna |
| X4 | Band cheez par likha ho ki kaise khulegi | T | Har band button, card, PowerUp | S3 | OK (test) | |
| X5 | Pehli maut ke baad wapas khelne ka raasta saaf | T | DONE se menu, phir Start | S2 | OK (test) | |

## Y. Code aur project ki sehat

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| Y1 | Compile warnings | B | Naye warning shoonya | S4 | BAAKI | Kabhi gine nahi |
| Y2 | Scene aur prefab mein toote hue reference | T, M | Shoonya | S2 | MASLA | `Main_Gameplay` mein 6 khaali union reference (code unhe chhod deta hai) |
| Y3 | Aapki bina commit ki files | M | Aap tay karen: rakhni ya hatani | S2 | MASLA | Scene, HUD prefab, 2 material, 1 meta, chest v3: hafton se bina commit |
| Y4 | Commit jo push nahi hue | M | Backup door bhi ho | S1 | OK (dekha) | 13 Oct: 133 commit GitHub par push hue (`2e17745e` tak). Aaj ka bina commit ka kaam abhi sirf laptop par hai |
| Y5 | Game ki log mein har frame ya har kill par likhna | B | Shoonya | S3 | BAAKI | `CampaignSignatureSystem` mein "Diagnostic" log bache hain |
| Y6 | Quality settings: Standalone ka default | M | Medium (performance contract) | S2 | MASLA | Default ab bhi Ultra hai |
| Y7 | Unity ka package error (pixel-perfect) | B | Saaf compile | S3 | BAAKI | Purana jaana hua, Unity 6000.0.43 mein theek |
| Y8 | Test jo kabhi-kabhi fail hote hain | T | Shoonya | S3 | BAAKI | Kuch pehle dekhe gaye (Soul Meteor, Pentagram chest) |
| Y9 | Bekaar ya dohra code, purane fields | M | Suchi bane | S4 | BAAKI | `EnemyData` ke purane drop fields, `Renderer2D.asset` |
| Y10 | AGENTS.md aur table asli code se milte hain | M | Koi purani baat nahi | S3 | BAAKI | Aaj ek purani tippani mili (Banish ranks) |

## Z. Release ki taiyaari

| ID | Kya jaanchna hai | Kaise | Pass ki rekha | Gambhirta | Aaj ki sthiti | Tippani |
|---|---|---|---|---|---|---|
| Z1 | Version number build mein aur screen par | M | Har build ka alag | S3 | BAAKI | |
| Z2 | Steam: achievements, cloud save, overlay | D | Sab chalen | S2 | BAAKI | Kuch bana nahi; game abhi offline hai |
| Z3 | Crash hone par report ya log kahan jaata hai | M | Khiladi bhej sake | S2 | BAAKI | |
| Z4 | Bhasha: sirf English; Hindi ya aur chahiye? | M | Aapka faisla | S3 | BAAKI | |
| Z5 | Umar ki rating (khoon, hinsa) | M | Store ke niyam | S2 | BAAKI | Disable Blood option hai |
| Z6 | Privacy, EULA, credits store page par | M | Poore | S1 | BAAKI | |
| Z7 | Sabse kam zaroori machine (minimum spec) tay aur jaanchi hui | D | Us par 30 FPS | S2 | BAAKI | Sirf ek laptop par naapa |
| Z8 | Build ka sign, antivirus ki chetavni | D | Windows roke nahi | S2 | BAAKI | |
| Z9 | Android: Play Store ke niyam (target API, 64-bit, size) | D | Sab poore | S2 | BAAKI | |
| Z10 | Art jo abhi placeholder hai, uski poori suchi | M | Har ek par aapki manzoori | S2 | BAAKI | L4, L10 ke saath |

---

## Jaanch ka kram (jab aap "shuru karo" kahen)

1. **S1 wali BAAKI rows pehle:** E14, J5, J6, N6, Q2, Q6, Q7.
2. **MASLA wali rows:** H4 (boss ki 5 minute ki seema), M3 (Pentagram ka jhatka), E11 (guardian ka damage), F9, G7, I4, I8, L3 (coin), L4, L5, M4, M5.
3. **Sound ka poora hissa (K4 se K10):** iske liye kisi insaan ko sunna hi padega; main sirf yeh bata sakta hoon ki kaun si clip kab baji.
4. Baaki BAAKI rows, hisse ke kram mein.

Har jaanch ka saboot (log, screenshot, test ka naam) is file ki tippani mein joda jayega.
