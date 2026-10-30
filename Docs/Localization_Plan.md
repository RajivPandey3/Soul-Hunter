# Localization plan (owner, 2026-10-30: English only for now)

Release audit question 6. The game ships in English; nothing changes now. This plan is how a second language can be added later without touching every screen at once.

## Where the text is today

- **About 340 English sentences are written in C#** (release review, `D:/SoulHunter-Validation/run-20261030-audit/agent-release.md`). They are mostly in:
  - `UI/HUD/*`: Options, pause, level-up, chest, Results, Map, Collection, Unlocks;
  - `CharacterStatSheet`, `PowerUps`, `ArcanaCatalog`;
  - `CampaignStory` and `StoryTwist`;
  - the notices in `CharacterUnlockTracker` and `LevelProgressionManager`.
- **Names and descriptions in assets:** WeaponDefinition, UpgradeData, CharacterData, EnemyData and ArcanaData.
- **Code that compares shown text:** some code keys on the visible name instead of an id. For example, Results rows are found by name when a source has no weapon (`RunStatsTracker._byName`), and the Arcana weapon checks read prefab names. Each needs an id before its text can be translated.

## Steps, when a language is wanted

1. **One string table.** A `Strings` ScriptableObject in `Resources`, holding key → English text plus one column per language. A small `T("key")` helper returns the current language's text and falls back to English.
2. **Keys, not text, in code.** Move the C# sentences into the table screen by screen, starting with the menus. Each screen's PlayMode fixture runs before the next one starts. Code that compares names switches to ids first (see above).
3. **Asset text.** Names and descriptions in assets get a key. The English stays the default, so nothing breaks while the other columns are empty.
4. **Fonts.** Cinzel and Barlow have no Devanagari (and no CJK). Hindi needs a Devanagari font under SIL OFL (for example Noto Sans Devanagari) as a TextMesh Pro fallback, with its own SDF atlas. A UI fit check (`UiStandardSizePlayModeTests`, the responsive fixtures) runs per language, because Hindi lines run longer.
5. **Options.** A Language row (VS has one: wiki Options, "Language"), saved in `GameData`.
6. **The story text.** The GDD lines are the owner's words (`CampaignStoryTests` checks them word for word); a translation needs the owner's approval.

Not started; estimate before any work begins.
