using UnityEngine;
using SoulHunter.Core.Persistence;

namespace SoulHunter.Gameplay.Audio
{
    /// <summary>
    /// VS Music Selection (release item P3-05; Soul Hunter's screen is "Soundtrack"). vampire.survivors.wiki/w/Stages, "Music
    /// selection" (raw page re-fetched 2026-10-09): "Music Selection is a feature that allows players to customize the
    /// background music of a stage by choosing from any unlocked music tracks in the game. Players can also apply modifiers to
    /// the music, which include Normal, Hyper, and Forsaken. The Normal modifier plays the track in its original form, while
    /// Hyper increases the speed and pitch of the music, and conversely, Forsaken slows the music down and lowers its pitch."
    /// vampire.survivors.wiki/w/Music: "By default, when the player visits a stage, the music track of that stage will be
    /// played ... if the player has ... manually selected a different track, that track will be played upon entering a stage
    /// instead"; Patch 1.0: "New UI option to lock the selection of a music track so it persists between runs"; Patch 0.6.1:
    /// "Music and music mods can now be previewd in stage selection screen".
    ///
    /// Soul Hunter: every one of its 11 CC0 tracks can be picked from the start (owner, 2026-10-09; VS unlocks a track with
    /// its stage, "Every Side B track unlocks together with the stage it belongs to"). The modifiers get Soul Hunter's own
    /// plain names: Normal, Faster (VS Hyper) and Slower (VS Forsaken). Character themes are NOT_APPLICABLE (Soul Hunter's
    /// characters have none). Inverse alternate tracks ("the stage theme will change to the alternate track if available"):
    /// Soul Hunter has no alternate recordings (audio sourcing is the owner's), so Inverse plays the mirror level's track
    /// (<see cref="StageTrackLevel"/>, 2026-10-13). Not built: any music change from the Hyper run mode (the wiki's "Stage
    /// modifiers now get applied correctly for normal music" does not say what they do).
    /// </summary>
    public static class MusicSelection
    {
        /// <summary>Choices, as GameData.MusicChoice stores them: 0 = each level's own track, 1 = the main theme, 2-11 = Levels 1-10.</summary>
        public const int LevelsOwn = 0, MainTheme = 1, FirstLevelTrack = 2;
        public const int ChoiceCount = SaveService.MusicChoiceCount;

        /// <summary>Modifiers, as GameData.MusicTempo stores them.</summary>
        public const int Normal = 0, Faster = 1, Slower = 2;
        public static readonly string[] TempoNames = { "Normal", "Faster", "Slower" };

        /// <summary>
        /// The pitch of each modifier (AudioSource.pitch changes speed and pitch together, exactly what the wiki describes).
        /// The wiki gives no numbers; balance method (2026-10-09): two semitones each way (2^(2/12) = 1.1225, 2^(-2/12) =
        /// 0.8909), a step every listener hears as clearly faster or slower while the loops stay in a musical key and
        /// readable at stage length. Not yet heard by the owner (listen check).
        /// </summary>
        public static readonly float[] TempoPitch = { 1f, 1.1225f, 0.8909f };

        public static float PitchFor(int tempo) => TempoPitch[Mathf.Clamp(tempo, 0, TempoPitch.Length - 1)];

        /// <summary>The choice and tempo this run plays (copied from the save when the run starts).</summary>
        public static int RunChoice { get; private set; }
        public static int RunTempo { get; private set; }

        /// <summary>
        /// Starts a run's music: the saved choice and tempo apply to the whole run. Without the lock, the choice lasts this run
        /// only: the save goes back to each level's own track (it is written with the next save).
        /// </summary>
        public static void BeginRun(GameData data)
        {
            RunChoice = data != null ? Mathf.Clamp(data.MusicChoice, 0, ChoiceCount - 1) : LevelsOwn;
            RunTempo = data != null ? Mathf.Clamp(data.MusicTempo, 0, TempoPitch.Length - 1) : Normal;
            if (data != null && !data.MusicLocked)
            {
                data.MusicChoice = LevelsOwn;
                data.MusicTempo = Normal;
            }
        }

        /// <summary>Back to each level's own track at normal pitch (when a run's scene goes away).</summary>
        public static void Reset()
        {
            RunChoice = LevelsOwn;
            RunTempo = Normal;
        }

        /// <summary>The clip of a choice: the stage's own for <see cref="LevelsOwn"/>.</summary>
        public static AudioClip ClipFor(AudioManager audio, int choice, int stage)
        {
            if (audio == null) return null;
            if (choice == MainTheme) return audio.MenuMusic;
            int level = choice >= FirstLevelTrack ? choice - FirstLevelTrack + 1 : stage;
            if (audio.StageMusic == null || audio.StageMusic.Length == 0) return null;
            return audio.StageMusic[Mathf.Clamp(level, 1, audio.StageMusic.Length) - 1];
        }

        /// <summary>How many levels have a track of their own (choices 2-11).</summary>
        public const int LevelTrackCount = ChoiceCount - FirstLevelTrack;

        /// <summary>
        /// The level whose track a stage plays. VS Inverse (vampire.survivors.wiki/w/Stages, "Inverse Mode", raw, fetched
        /// 2026-10-13): "When playing in Inverse Mode on some stages, the stage theme will change to the alternate track if
        /// available. However, playing as a character with a unique theme ... will override this effect." The Music page
        /// lists a second track per stage for "(inverse mode)".
        /// Soul Hunter has 11 CC0 tracks and no alternate recordings. Balance method (owner rule 2026-10-09, wiki rule kept,
        /// content chosen from what exists): in Inverse with "Each Level's Own", Level n plays the track of its mirror
        /// level, 11 - n (1 and 10, 2 and 9 ... 5 and 6), so every stage sounds different from its normal run and no level
        /// keeps its own track (ten levels have no middle one). A track chosen on the Soundtrack card wins, as a VS
        /// override does. Not yet heard by the owner (listen check).
        /// </summary>
        public static int StageTrackLevel(int choice, int stage, bool inverse)
        {
            stage = Mathf.Clamp(stage, 1, LevelTrackCount);
            return inverse && choice == LevelsOwn ? LevelTrackCount + 1 - stage : stage;
        }

        /// <summary>The name a choice shows: Soul Hunter's own level names.</summary>
        public static string ChoiceName(int choice)
        {
            if (choice == MainTheme) return "Main Theme";
            if (choice >= FirstLevelTrack && choice < ChoiceCount)
            {
                int level = choice - FirstLevelTrack + 1;
                return $"Level {level}: {SoulHunter.Gameplay.Data.CampaignLevelCatalog.Get(level).Name}";
            }
            return "Each Level's Own";
        }
    }
}
