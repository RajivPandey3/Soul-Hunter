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
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
                SoulHunter.Core.Persistence.GameSettings.Changed += RefreshVolumes;
                HandleSceneLoaded(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Destroy(gameObject);
            }
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

        /// <summary>Loops a music clip; does nothing if it is already playing.</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (_musicSource == null || clip == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;
            _musicSource.clip = clip;
            _musicSource.volume = MusicVolume * SoulHunter.Core.Persistence.GameSettings.MusicVolume;
            _musicSource.Play();
        }

        public void PlayStageMusic(int stage)
        {
            if (StageMusic == null || StageMusic.Length == 0) return;
            PlayMusic(StageMusic[Mathf.Clamp(stage, 1, StageMusic.Length) - 1]);
        }

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
