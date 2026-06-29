using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Music and sound effect manager.
/// </summary>
public class MusicMgr : BaseManager<MusicMgr>
{
    private AudioSource bkMusic;
    private float bkMusicValue = 0.16f;
    private readonly List<AudioSource> soundList = new List<AudioSource>();
    private float soundValue = 0.32f;
    private bool soundIsPlay = true;

    private MusicMgr()
    {
        MonoMgr.Instance.AddFixedUpdateListener(Update);
    }

    private void Update()
    {
        if (!soundIsPlay)
            return;

        for (int i = soundList.Count - 1; i >= 0; --i)
        {
            if (soundList[i] == null)
            {
                soundList.RemoveAt(i);
                continue;
            }

            if (!soundList[i].isPlaying)
            {
                soundList[i].clip = null;
                PoolMgr.Instance.PushObj(soundList[i].gameObject);
                soundList.RemoveAt(i);
            }
        }
    }

    public void PlayBKMusic(string name)
    {
        if (bkMusic == null)
        {
            GameObject obj = new GameObject("BKMusic");
            GameObject.DontDestroyOnLoad(obj);
            bkMusic = obj.AddComponent<AudioSource>();
        }

        AudioClip clip = Resources.Load<AudioClip>($"Music/{name}");
        if (clip == null || bkMusic == null)
            return;

        if (bkMusic.clip == clip && bkMusic.isPlaying)
        {
            bkMusic.volume = bkMusicValue;
            return;
        }

        bkMusic.clip = clip;
        bkMusic.loop = true;
        bkMusic.volume = bkMusicValue;
        bkMusic.Play();
    }

    public void StopBKMusic()
    {
        if (bkMusic != null)
            bkMusic.Stop();
    }

    public void PauseBKMusic()
    {
        if (bkMusic != null)
            bkMusic.Pause();
    }

    public void ChangeBKMusicValue(float v)
    {
        bkMusicValue = v;
        if (bkMusic != null)
            bkMusic.volume = bkMusicValue;
    }

    public void PlaySound(string name, bool isLoop = false, bool isSync = false, UnityAction<AudioSource> callBack = null)
    {
        PlaySoundFrom(name, 0f, isLoop, isSync, callBack);
    }

    public void PlaySoundFrom(string name, float startTime, bool isLoop = false, bool isSync = false, UnityAction<AudioSource> callBack = null)
    {
        AudioClip resourceClip = Resources.Load<AudioClip>($"Sound/{name}");
        if (resourceClip != null)
        {
            PlayClip(resourceClip, isLoop, callBack, startTime);
            return;
        }

        Debug.LogError($"Sound not found: Resources/Sound/{name}");
    }

    public void StopSound(AudioSource source)
    {
        if (source == null || !soundList.Contains(source))
            return;

        source.Stop();
        soundList.Remove(source);
        source.clip = null;
        PoolMgr.Instance.PushObj(source.gameObject);
    }

    public void ChangeSoundValue(float v)
    {
        soundValue = v;
        for (int i = 0; i < soundList.Count; i++)
        {
            if (soundList[i] != null)
                soundList[i].volume = v;
        }
    }

    public void PlayOrPauseSound(bool isPlay)
    {
        soundIsPlay = isPlay;
        for (int i = 0; i < soundList.Count; i++)
        {
            if (soundList[i] == null)
                continue;

            if (isPlay)
                soundList[i].Play();
            else
                soundList[i].Pause();
        }
    }

    public void ClearSound()
    {
        for (int i = 0; i < soundList.Count; i++)
        {
            if (soundList[i] == null)
                continue;

            soundList[i].Stop();
            soundList[i].clip = null;
            PoolMgr.Instance.PushObj(soundList[i].gameObject);
        }

        soundList.Clear();
    }

    private void PlayClip(AudioClip clip, bool isLoop, UnityAction<AudioSource> callBack, float startTime = 0f)
    {
        if (clip == null)
            return;

        GameObject soundObj = PoolMgr.Instance.GetObj("Sound/soundObj");
        if (soundObj == null)
            return;

        AudioSource source = soundObj.GetComponent<AudioSource>();
        if (source == null)
            source = soundObj.AddComponent<AudioSource>();

        source.Stop();
        source.clip = clip;
        source.loop = isLoop;
        source.volume = soundValue;
        source.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.01f));
        source.Play();

        if (!soundList.Contains(source))
            soundList.Add(source);

        callBack?.Invoke(source);
    }
}
