using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KrakenCache.Controllers
{
    public enum SoundCue { UiTap, CubePop, SpecialCreate, ComboCharge, Rocket, Tnt, Stone, Vase, Door, Cup, Win, Lose, SoundToggle, Continue }

    /// <summary>Bounded 2D sound voices, independent of gameplay timing and random state.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const string MutedKey = "KC_SfxMuted";
        private const int VoiceLimit = 8;
        private static GameAudio instance;
        public static event Action<bool> OnMuteChanged;
        public static bool Muted => PlayerPrefs.GetInt(MutedKey, 0) != 0;
        [SerializeField, Range(0, 1)] private float masterVolume = .65f;
        [SerializeField, Range(0, .3f)] private float musicVolume = .15f;
        private static readonly string[] Names = { "ui_tap", "cube_pop", "special_create", "combo_charge", "rocket", "tnt", "stone", "vase", "door", "cup", "win", "lose", "sound_toggle", "continue" };
        private static readonly float[] Gains = { .24f, .34f, .32f, .35f, .48f, .62f, .3f, .3f, .35f, .25f, .55f, .4f, .24f, .3f };
        private static readonly float[] Cooldowns = { .05f, .045f, .08f, .1f, .06f, .12f, .045f, .06f, .08f, .035f, .3f, .3f, .05f, .05f };
        private readonly System.Random pitchRandom = new System.Random(7031);
        private AudioClip[] clips;
        private double[] lastStarts;
        private Voice[] voices;
        private AudioListener listener;
        private AudioSource musicSource;
        private bool musicStarted;
        private sealed class Voice
        {
            public AudioSource Source;
            public double Until;
            public SoundCue Cue;
            public float Gain;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; OnMuteChanged = null; }
        private static GameAudio Ensure()
        {
            if (!Application.isPlaying) return null;
            if (instance == null) instance = FindFirstObjectByType<GameAudio>();
            if (instance == null) instance = new GameObject("GameAudio").AddComponent<GameAudio>();
            return instance;
        }
        public static void Prepare() { Ensure(); }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            clips = new AudioClip[Names.Length]; lastStarts = new double[Names.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = Resources.Load<AudioClip>("Audio/" + Names[i]);
                lastStarts[i] = double.NegativeInfinity;
            }
            voices = new Voice[VoiceLimit];
            for (int i = 0; i < voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = 0; source.dopplerLevel = 0;
                voices[i] = new Voice { Source = source };
            }
            listener = gameObject.AddComponent<AudioListener>();
            EnsureListener();
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false; musicSource.spatialBlend = 0; musicSource.dopplerLevel = 0;
            musicSource.priority = 200; musicSource.loop = true; musicSource.volume = 0;
            musicSource.clip = Resources.Load<AudioClip>("Audio/lounge_loop");
            ResumeMusic();
            SceneManager.sceneLoaded += SceneLoaded;
        }
        private void EnsureListener()
        {
            if (listener == null) return;
            bool another = false;
            foreach (var candidate in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (candidate != listener && candidate.isActiveAndEnabled) { another = true; break; }
            listener.enabled = !another;
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode) { ClearGameplayVoices(); EnsureListener(); }
        public static bool Play(SoundCue cue)
        {
            if (Muted) return false;
            var audio = Ensure();
            return audio != null && audio.PlayInternal(cue);
        }
        private static bool IsUiCue(SoundCue cue) => cue == SoundCue.UiTap || cue == SoundCue.SoundToggle || cue == SoundCue.Continue;
        private static int Priority(SoundCue cue) => cue == SoundCue.Win || cue == SoundCue.Lose ? 3
            : IsUiCue(cue) || cue == SoundCue.Rocket || cue == SoundCue.Tnt ? 2 : 1;
        private bool PlayInternal(SoundCue cue)
        {
            int index = (int)cue;
            if (index < 0 || index >= clips.Length || clips[index] == null) return false;
            double now = Time.unscaledTimeAsDouble;
            if (now - lastStarts[index] < Cooldowns[index]) return false;
            Voice selected = null;
            foreach (var voice in voices) if (voice.Until <= now) { selected = voice; break; }
            if (selected == null)
            {
                foreach (var voice in voices)
                    if (Priority(voice.Cue) <= Priority(cue) && (selected == null || voice.Until < selected.Until)) selected = voice;
            }
            if (selected == null) return false;
            lastStarts[index] = now;
            selected.Source.Stop(); selected.Source.clip = clips[index];
            selected.Cue = cue; selected.Gain = Gains[index];
            selected.Source.pitch = cue == SoundCue.Win || cue == SoundCue.Lose ? 1 : .97f + (float)pitchRandom.NextDouble() * .06f;
            selected.Until = now + clips[index].length / selected.Source.pitch;
            selected.Source.priority = 128 - Priority(cue) * 30;
            ApplyVolumes(now);
            selected.Source.Play();
            return true;
        }
        private void Update() => ApplyVolumes(Time.unscaledTimeAsDouble);
        private void ApplyVolumes(double now)
        {
            int active = 0;
            foreach (var voice in voices) if (voice.Until > now) active++;
            float gain = masterVolume / Mathf.Sqrt(Mathf.Max(1, active));
            bool muted = Muted;
            foreach (var voice in voices) voice.Source.volume = muted ? 0 : gain * voice.Gain;
            if (musicSource != null)
            {
                float target = muted ? 0 : masterVolume * musicVolume * (active > 0 ? .55f : 1);
                musicSource.volume = muted ? 0 : Mathf.MoveTowards(musicSource.volume, target, Time.unscaledDeltaTime*.12f);
            }
        }
        public static void SetMuted(bool muted)
        {
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0); PlayerPrefs.Save();
            if (instance != null)
            {
                if (muted)
                {
                    instance.StopVoices(false);
                    instance.musicSource.Pause(); instance.musicSource.volume = 0;
                }
                else instance.ResumeMusic();
            }
            OnMuteChanged?.Invoke(muted);
        }
        private void ResumeMusic()
        {
            if (Muted || musicSource == null || musicSource.clip == null) return;
            if (musicStarted) musicSource.UnPause();
            else { musicSource.Play(); musicStarted = true; }
        }
        public static void StopGameplay() { if (instance != null) instance.ClearGameplayVoices(); }
        private void ClearGameplayVoices()
        {
            StopVoices(true);
            for (int i = 1; i < lastStarts.Length; i++) lastStarts[i] = double.NegativeInfinity;
        }
        private void StopVoices(bool keepUi)
        {
            foreach (var voice in voices)
            {
                if (keepUi && IsUiCue(voice.Cue)) continue;
                voice.Source.Stop(); voice.Until = 0;
            }
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (instance == this) instance = null;
        }
    }
}
