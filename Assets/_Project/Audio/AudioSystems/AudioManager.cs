using UnityEngine;

namespace SoulHunter.Gameplay.Audio
{
    /// <summary>
    /// Learning Comment:
    /// Option 3 (Polish): Game bina awaz ke soona (dead) lagta hai.
    /// Ye ek universal Audio Manager hai. Game mein kahin se bhi koi bhi script
    /// AudioManager.Instance.PlaySFX(clip) bula kar sound baja sakti hai.
    /// Object pooling ki tarah isme bahut saare audio sources (channels) hain taaki awaz over-lap na ho.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sound Effects (SFX)")]
        public AudioClip GemPickupSound;
        public AudioClip LevelUpSound;
        public AudioClip EnemyHitSound;
        public AudioClip ChestOpenSound;
        public AudioClip HealSound;
        public AudioClip EnemyDeathSound;
        public AudioClip PlayerHurtSound;
        public AudioClip GameOverSound;
        public AudioClip StageClearSound;
        public AudioClip BossWarningSound;
        public AudioClip UIClickSound;
        public AudioClip UIConfirmSound;
        public AudioClip UnlockSound;
        [Tooltip("VS wiki (Treasure_Chest, Audio): plays when a chest is found.")]
        public AudioClip TreasureFoundSound;
        [Tooltip("VS: one jingle per chest size, for 1, 3 and 5 items.")]
        public AudioClip TreasureASound, TreasureBSound, TreasureCSound;

        /// <summary>The chest jingle for a chest of <paramref name="itemCount"/> items (VS Treasure A/B/C).</summary>
        public AudioClip TreasureJingle(int itemCount) => itemCount >= 5 ? TreasureCSound : itemCount >= 3 ? TreasureBSound : TreasureASound;

        [Header("Weapon Sounds")]
        public AudioClip ShootSound;
        public AudioClip SlashSound;
        public AudioClip MagicSound;
        public AudioClip ExplosionSound;

        [Header("Music (loops)")]
        public AudioClip MenuMusic;
        [Tooltip("One loop per stage, index 0 = Level 1.")]
        public AudioClip[] StageMusic;
        [Range(0f, 1f)] public float MusicVolume = 0.45f;

        [Header("Settings")]
        [Range(0f, 1f)] public float MasterVolume = 0.8f;

        [Tooltip("Ek baar mein kitni awazein baj sakti hain")]
        [SerializeField] private int _audioChannels = 10;

        private AudioSource[] _sfxSources;
        private AudioSource _musicSource;

        public AudioClip CurrentMusic => _musicSource != null ? _musicSource.clip : null;
        public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;

        /// <summary>The clip PlaySFX was last asked to play (tests read this; batchmode may not output sound).</summary>
        public AudioClip LastClip { get; private set; }
        public int PlayedCount { get; private set; }
        /// <summary>The last few clips PlaySFX was asked to play, newest last (tests: a weapon firing in the same frame can replace LastClip).</summary>
        public readonly System.Collections.Generic.List<AudioClip> RecentClips = new System.Collections.Generic.List<AudioClip>();
        private const int RecentClipCount = 8;

        private const string ResourcePath = "AudioManager";

        // No scene contains an AudioManager, so create the one from Resources
        // once scene objects (and GameServices) have woken up.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureInstance()
        {
            if (Instance != null) return; // Unity null: a destroyed instance (e.g. between PlayMode tests) is recreated
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[AudioManager] Resources/{ResourcePath} prefab not found; the game will be silent.");
                return;
            }
            Instantiate(prefab).name = prefab.name;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SoulHunter.Core.Persistence.SaveService save = null;
                if (SoulHunter.Core.Services.GameServices.Instance != null)
                    SoulHunter.Core.Services.GameServices.Instance.TryGet(out save);
                if (save != null) MasterVolume = Mathf.Clamp01(save.CurrentData.MasterVolume);
                AudioListener.volume = MasterVolume;
                InitializeAudioChannels();
                PreloadEffects();
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
                SoulHunter.Core.Persistence.GameSettings.Changed += RefreshVolumes;
                HandleSceneLoaded(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// The effect clips are imported without "Preload Audio Data", so each one was read from disk the first time it
        /// played: a late-run profile (2026-10-09) showed 263 ms in SoundManager.LoadFMODSound / File.Read when the chest
        /// screen played its jingle. Every effect (not the streamed music) is loaded once here, at startup.
        /// </summary>
        public int PreloadEffects()
        {
            int loaded = 0;
            foreach (var field in typeof(AudioManager).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                if (field.FieldType != typeof(AudioClip) || field.Name == nameof(MenuMusic)) continue;
                var clip = field.GetValue(this) as AudioClip;
                if (clip == null || clip.loadState == AudioDataLoadState.Loaded) continue;
                if (clip.LoadAudioData()) loaded++;
            }
            return loaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
                SoulHunter.Core.Persistence.GameSettings.Changed -= RefreshVolumes;
            }
        }

        // Options changed: the looping music follows the new Music volume at once.
        private void RefreshVolumes()
        {
            if (_musicSource != null) _musicSource.volume = MusicVolume * SoulHunter.Core.Persistence.GameSettings.MusicVolume;
        }

        // The menu has its own loop; stage music is started by StageAudio in gameplay.
        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // SceneService loads scenes additively, so the scene name decides, not the load mode.
            if (scene.name == "MainMenu") PlayMusic(MenuMusic);
        }

        /// <summary>Loops a music clip at normal pitch; does nothing if it is already playing.</summary>
        public void PlayMusic(AudioClip clip) => PlayMusic(clip, 1f);

        /// <summary>
        /// Loops a music clip at <paramref name="pitch"/> (VS Music Selection modifiers: speed and pitch together). A clip
        /// already playing keeps playing and only takes the new pitch.
        /// </summary>
        public void PlayMusic(AudioClip clip, float pitch)
        {
            if (_musicSource == null || clip == null) return;
            _musicSource.pitch = pitch;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;
            _musicSource.clip = clip;
            _musicSource.volume = MusicVolume * SoulHunter.Core.Persistence.GameSettings.MusicVolume;
            _musicSource.Play();
        }

        /// <summary>
        /// A stage's music: its own track by default, or the run's Soundtrack choice with its tempo (VS Music Selection,
        /// MusicSelection: "if the player has ... manually selected a different track, that track will be played upon
        /// entering a stage instead"). In Inverse mode a level's own track is its mirror level's (MusicSelection.StageTrackLevel).
        /// </summary>
        public void PlayStageMusic(int stage)
        {
            int trackLevel = MusicSelection.StageTrackLevel(MusicSelection.RunChoice, stage, SoulHunter.Gameplay.Data.RunModes.Inverse);
            PlayMusic(MusicSelection.ClipFor(this, MusicSelection.RunChoice, trackLevel), MusicSelection.PitchFor(MusicSelection.RunTempo));
        }

        /// <summary>The pitch the music plays at now (1 = normal).</summary>
        public float MusicPitch => _musicSource != null ? _musicSource.pitch : 1f;

        public void StopMusic()
        {
            if (_musicSource != null) _musicSource.Stop();
        }

        /// <summary>Plays a weapon's fire sound (quietly and throttled: many weapons fire every second).</summary>
        public void PlayWeaponSound(WeaponSound sound)
        {
            AudioClip clip = sound == WeaponSound.Shoot ? ShootSound
                : sound == WeaponSound.Slash ? SlashSound
                : sound == WeaponSound.Magic ? MagicSound
                : sound == WeaponSound.Explosion ? ExplosionSound : null;
            PlaySFXThrottled(clip, WeaponInterval, 0.35f);
        }

        private void InitializeAudioChannels()
        {
            _audioChannels = Mathf.Max(1, _audioChannels);
            _sfxSources = new AudioSource[_audioChannels];
            for (int i = 0; i < _audioChannels; i++)
            {
                // Naye audio source (speaker) banakar chupa do
                GameObject child = new GameObject($"SFX_Channel_{i}");
                child.transform.SetParent(transform);
                
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f; // 2D Sound
                
                _sfxSources[i] = source;
            }

            var music = new GameObject("Music_Channel");
            music.transform.SetParent(transform);
            _musicSource = music.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
        }

        /// <summary>Minimum seconds between two enemy-hit sounds; a horde takes hundreds of hits a second.</summary>
        public const float EnemyHitInterval = 0.06f;
        public const float EnemyDeathInterval = 0.05f;
        public const float PlayerHurtInterval = 0.2f;
        public const float WeaponInterval = 0.08f;
        private readonly System.Collections.Generic.Dictionary<AudioClip, float> _lastPlayed = new System.Collections.Generic.Dictionary<AudioClip, float>();

        /// <summary>Plays clip unless it already played less than minInterval (unscaled) seconds ago.</summary>
        public bool PlaySFXThrottled(AudioClip clip, float minInterval, float volumeMultiplier = 1f)
        {
            if (clip == null) return false;
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(clip, out float last) && now - last < minInterval) return false;
            _lastPlayed[clip] = now;
            PlaySFX(clip, volumeMultiplier);
            return true;
        }

        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null) return;
            LastClip = clip;
            PlayedCount++;
            RecentClips.Add(clip);
            if (RecentClips.Count > RecentClipCount) RecentClips.RemoveAt(0);

            // Khali speaker (AudioSource) dhoondho
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                if (!_sfxSources[i].isPlaying)
                {
                    _sfxSources[i].clip = clip;
                    _sfxSources[i].volume = volumeMultiplier * SoulHunter.Core.Persistence.GameSettings.SfxVolume; // master volume is AudioListener.volume
                    _sfxSources[i].Play();
                    return;
                }
            }
            
            // Agar sab baj rahe hain, toh pehle wale ko rok kar naya bajao (ya ignore karo)
            _sfxSources[0].clip = clip;
            _sfxSources[0].volume = volumeMultiplier * SoulHunter.Core.Persistence.GameSettings.SfxVolume;
            _sfxSources[0].Play();
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
        }
    }

    /// <summary>Fire sound category of a weapon.</summary>
    public enum WeaponSound { None, Shoot, Slash, Magic, Explosion }
}
