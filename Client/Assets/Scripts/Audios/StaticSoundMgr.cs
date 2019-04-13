using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticSoundMgr
{
    public static StaticSoundMgr Instance = new StaticSoundMgr();

    private GameObject staticSoundObject = null;

    private AudioSource btnAudioSource = null;
    private AudioSource backgroundAudioSource = null;

    private Dictionary<string, AudioClip> staticClips = new Dictionary<string, AudioClip>();

    public StaticSoundMgr()
    {

    }

    public void Init()
    {
        staticSoundObject = new GameObject("StaticSound");
        GameObject.DontDestroyOnLoad(staticSoundObject);

        staticSoundObject.AddComponent<AudioListener>();
        btnAudioSource = staticSoundObject.AddComponent<AudioSource>();
        backgroundAudioSource = staticSoundObject.AddComponent<AudioSource>();
        backgroundAudioSource.loop = true;

        AudioClip clip1 = Resources.Load<AudioClip>(@"Audio\StaticAudio\UI_Click") as AudioClip;
        AudioClip clip2 = Resources.Load<AudioClip>(@"Audio\StaticAudio\BGM") as AudioClip;
        AudioClip clip3 = Resources.Load<AudioClip>(@"Audio\StaticAudio\YouLose") as AudioClip;
        AudioClip clip4 = Resources.Load<AudioClip>(@"Audio\StaticAudio\YouWin") as AudioClip;
        AudioClip clip5 = Resources.Load<AudioClip>(@"Audio\StaticAudio\Login") as AudioClip;

        staticClips.Add("BtnClick", clip1);
        staticClips.Add("BGM", clip2);
        staticClips.Add("Win", clip3);
        staticClips.Add("Lose", clip4);
        staticClips.Add("Login", clip5);
    }

    public void Dispose()
    {
        if (null != staticSoundObject)
        {
            GameObject.Destroy(staticSoundObject);
            staticSoundObject = null;
        }
    }

    public void PlaySound(string key)
    {
        AudioClip clip = null;
        staticClips.TryGetValue(key, out clip);
        if (null == clip)
            return;

        btnAudioSource.clip = clip;
        btnAudioSource.Play();
    }

    public void PlayBackgroundSound(string key)
    {
        if (backgroundAudioSource.isPlaying)
            return;

        AudioClip clip = null;
        staticClips.TryGetValue(key, out clip);
        if (null == clip)
            return;

        backgroundAudioSource.clip = clip;
        backgroundAudioSource.Play();
    }

    public void StopBackgroundSound()
    {
        backgroundAudioSource.clip = null;
    }
}
