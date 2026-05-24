using System.Collections;
using UnityEngine;

namespace GemsSort.Services
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GemsSortAudio : MonoBehaviour
    {
        public static GemsSortAudio Instance { get; private set; }

        [Header("Playback")]
        [Tooltip("Master toggle for UI audio.")]
        [SerializeField] private bool soundEnabled = true;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Header("Music")]
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private bool musicEnabled = true;

        [Header("Audio Clips")]
        [Tooltip("Played when selecting a board group or shelf color.")]
        [SerializeField] private AudioClip selectClip;
        [Tooltip("Played once per move action.")]
        [SerializeField] private AudioClip moveClip;
        [Tooltip("Played when a move is invalid.")]
        [SerializeField] private AudioClip errorClip;
        [Tooltip("Played when clearing a selection.")]
        [SerializeField] private AudioClip deselectClip;
        [Tooltip("Played when a level is complete.")]
        [SerializeField] private AudioClip winClip;
        [Tooltip("Played when one color gets fully sorted.")]
        [SerializeField] private AudioClip colorSortedClip;
        [Tooltip("Played when a coin lands on the counter during the win reward animation.")]
        [SerializeField] private AudioClip coinCollectClip;
        [Tooltip("Played when clicking a UI button.")]
        [SerializeField] private AudioClip buttonClickClip;
        [Tooltip("Played when purchasing hint packs.")]
        [SerializeField] private AudioClip buyClip;

        private AudioSource audioSource;

        public bool SoundEnabled
        {
            get => soundEnabled;
            set
            {
                soundEnabled = value;
                PlayerPrefs.SetInt("GemsSort.SoundEnabled", soundEnabled ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public bool MusicEnabled
        {
            get => musicEnabled;
            set
            {
                musicEnabled = value;
                PlayerPrefs.SetInt("GemsSort.MusicEnabled", musicEnabled ? 1 : 0);
                PlayerPrefs.Save();
                UpdateMusicPlayback();
            }
        }

        private void Awake()
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            soundEnabled = PlayerPrefs.GetInt("GemsSort.SoundEnabled", 1) == 1;
            musicEnabled = PlayerPrefs.GetInt("GemsSort.MusicEnabled", 1) == 1;

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            if (musicClip == null)
            {
                musicClip = Resources.Load<AudioClip>("Sound/bgm");
            }

            if (buttonClickClip == null)
            {
                buttonClickClip = Resources.Load<AudioClip>("Sound/creatorshome-keyboard-click-327728");
            }

            if (buyClip == null)
            {
                buyClip = Resources.Load<AudioClip>("Sound/freesound_crunchpixstudio-drop-coin-384921");
            }

            musicSource.clip = musicClip;
            UpdateMusicPlayback();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void UpdateMusicPlayback()
        {
            if (musicSource == null) return;

            if (musicEnabled && musicClip != null)
            {
                if (!musicSource.isPlaying)
                {
                    musicSource.volume = volume * 0.4f; // slightly quieter background music
                    musicSource.Play();
                }
            }
            else
            {
                if (musicSource.isPlaying)
                {
                    musicSource.Stop();
                }
            }
        }

        public void Select(int count)
        {
            Play(selectClip);
        }

        public void Move()
        {
            Play(moveClip);
        }

        public void Error()
        {
            Play(errorClip);
        }

        public void Deselect()
        {
            Play(deselectClip);
        }

        public void Win()
        {
            Play(winClip);
        }

        public void ColorSorted()
        {
            Play(colorSortedClip);
        }

        public void CoinCollect()
        {
            Play(coinCollectClip);
        }

        public void ButtonClick()
        {
            Play(buttonClickClip);
        }

        public void Buy()
        {
            Play(buyClip);
        }

        private void Play(AudioClip clip)
        {
            if (!soundEnabled || audioSource == null || clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, volume);
        }
    }
}
