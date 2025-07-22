using System.Collections.Generic;
using UnityEngine;

public class SoundController : Singleton<SoundController>
{
    [Header("AudioSources")] [SerializeField]
    private AudioSource musicSource;

    public AudioSource sfxSource_walk;
    public AudioSource sfxSource_shooting;

    [Header("Music Clips")] [SerializeField]
    private List<AudioClip> musicClips;

    [Header("SFX Clips")] [SerializeField] private List<AudioClip> footstepClips_floor;
    [SerializeField] private List<AudioClip> footstepClips_grass;
    [SerializeField] private List<AudioClip> shootingClips;

    private int currentMusicIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
        }

        if (sfxSource_walk == null)
        {
            sfxSource_walk = gameObject.AddComponent<AudioSource>();
            sfxSource_walk.loop = false;
        }
    }

    #region Background Music

    public void PlayMusic(int index, bool loop = true)
    {
        if (musicClips == null || musicClips.Count == 0) return;
        index = Mathf.Clamp(index, 0, musicClips.Count - 1);
        currentMusicIndex = index;
        musicSource.clip = musicClips[index];
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlayNextMusic(bool loop = true)
    {
        if (musicClips == null || musicClips.Count == 0) return;
        currentMusicIndex = (currentMusicIndex + 1) % musicClips.Count;
        PlayMusic(currentMusicIndex, loop);
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    #endregion

    #region SFX Sounds Effects

    public void PlayFootstep_floor(float volume = 1f, float pitch = 1f)
        => PlayRandomClip(sfxSource_walk, footstepClips_floor, volume, pitch);

    public void PlayFootstep_grass(float volume = 1f, float pitch = 1f)
        => PlayRandomClip(sfxSource_walk, footstepClips_grass, volume, pitch);

    public void PlayShooting(float volume = 1f, float pitch = 1f)
        => PlayRandomClip(sfxSource_shooting, shootingClips, volume, pitch);

    public void PlaySFX(AudioSource source, AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        source.PlayOneShot(clip, volume);
    }

    public bool IsSFXWalkPlaying()
    {
        return sfxSource_walk.isPlaying;
    }

    public bool IsSFXShootingPlaying()
    {
        return sfxSource_shooting.isPlaying;
    }

    private void PlayRandomClip(AudioSource source, List<AudioClip> clips, float volume, float pitch)
    {
        if (clips == null || clips.Count == 0) return;
        int idx = Random.Range(0, clips.Count);
        AudioClip clip = clips[idx];
        PlaySFX(source, clip, volume, pitch);
    }

    public void PlaySFX(AudioSource source, AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        source.pitch = Mathf.Clamp(pitch, -3f, 3f);
        source.PlayOneShot(clip, volume);
    }

    #endregion
}