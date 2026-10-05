using UnityEngine;

/// <summary>
/// Manages all audio for the museum experience:
/// - Voiceover narration (spoken intro, station intros)
/// - Ambient sound (port atmosphere, underwater sounds)
/// - SFX (selection clicks, transitions)
/// 
/// COMP5424 Phase 2 — Harbour City Museum (Group 5)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceoverSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip sessionIntroClip;
    [SerializeField] private AudioClip ambientPortClip;
    [SerializeField] private AudioClip ambientUnderwaterClip;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip stationEnterSound;
    [SerializeField] private AudioClip stationCompleteSound;

    [Header("Volume")]
    [SerializeField] private float voiceoverVolume = 0.9f;
    [SerializeField] private float ambientVolume = 0.4f;
    [SerializeField] private float sfxVolume = 0.7f;

    [Header("Settings")]
    [SerializeField] private bool enableAudio = true;
    [SerializeField] private bool loopAmbient = true;

    private AudioClip currentVoiceover;
    private AudioClip currentAmbient;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize audio sources if not assigned
        if (voiceoverSource == null)
        {
            voiceoverSource = gameObject.AddComponent<AudioSource>();
        }
        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure sources
        voiceoverSource.volume = voiceoverVolume;
        voiceoverSource.playOnAwake = false;

        ambientSource.volume = ambientVolume;
        ambientSource.loop = loopAmbient;
        ambientSource.playOnAwake = false;

        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// Plays a voiceover narration clip.
    /// </summary>
    public void PlayVoiceover(AudioClip clip)
    {
        if (!enableAudio || clip == null)
            return;

        currentVoiceover = clip;
        voiceoverSource.Stop();
        voiceoverSource.clip = clip;
        voiceoverSource.Play();

        Debug.Log($"[AudioManager] Playing voiceover: {clip.name}");
    }

    /// <summary>
    /// Plays a one-shot sound effect.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (!enableAudio || clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Starts ambient background sound.
    /// </summary>
    public void PlayAmbient(AudioClip clip)
    {
        if (!enableAudio || clip == null)
            return;

        currentAmbient = clip;
        ambientSource.Stop();
        ambientSource.clip = clip;
        ambientSource.Play();

        Debug.Log($"[AudioManager] Playing ambient: {clip.name}");
    }

    /// <summary>
    /// Stops the current ambient sound.
    /// </summary>
    public void StopAmbient()
    {
        ambientSource.Stop();
        currentAmbient = null;
    }

    /// <summary>
    /// Stops all audio playback.
    /// </summary>
    public void StopAll()
    {
        voiceoverSource.Stop();
        ambientSource.Stop();
        sfxSource.Stop();
    }

    /// <summary>
    /// Plays the session introduction voiceover.
    /// </summary>
    public void PlaySessionIntro()
    {
        if (sessionIntroClip != null)
        {
            PlayVoiceover(sessionIntroClip);
        }
    }

    /// <summary>
    /// Switches ambient to port atmosphere.
    /// </summary>
    public void SetAmbientPort()
    {
        if (ambientPortClip != null)
        {
            PlayAmbient(ambientPortClip);
        }
    }

    /// <summary>
    /// Switches ambient to underwater sounds (for helmet station).
    /// </summary>
    public void SetAmbientUnderwater()
    {
        if (ambientUnderwaterClip != null)
        {
            PlayAmbient(ambientUnderwaterClip);
        }
    }

    /// <summary>
    /// Plays the hover sound effect.
    /// </summary>
    public void PlayHoverSound()
    {
        PlaySFX(hoverSound);
    }

    /// <summary>
    /// Plays the selection sound effect.
    /// </summary>
    public void PlaySelectSound()
    {
        PlaySFX(selectSound);
    }

    /// <summary>
    /// Plays the station enter sound effect.
    /// </summary>
    public void PlayStationEnterSound()
    {
        PlaySFX(stationEnterSound);
    }

    /// <summary>
    /// Plays the station complete sound effect.
    /// </summary>
    public void PlayStationCompleteSound()
    {
        PlaySFX(stationCompleteSound);
    }

    /// <summary>
    /// Sets the master volume for all audio.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        voiceoverSource.volume = volume * voiceoverVolume;
        ambientSource.volume = volume * ambientVolume;
        sfxSource.volume = volume * sfxVolume;
    }

    /// <summary>
    /// Enables or disables all audio.
    /// </summary>
    public void SetAudioEnabled(bool enabled)
    {
        enableAudio = enabled;

        if (!enabled)
        {
            StopAll();
        }
    }

    /// <summary>
    /// Returns whether a voiceover is currently playing.
    /// </summary>
    public bool IsVoiceoverPlaying()
    {
        return voiceoverSource.isPlaying;
    }

    /// <summary>
    /// Returns the remaining duration of the current voiceover.
    /// </summary>
    public float GetVoiceoverRemaining()
    {
        if (!voiceoverSource.isPlaying || voiceoverSource.clip == null)
            return 0f;

        return voiceoverSource.clip.length - voiceoverSource.time;
    }
}
