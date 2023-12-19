using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

public class AudioManager : SingletonBase<AudioManager>
{

    Dictionary<SFX_Name, AudioClip> sfxDict = new Dictionary<SFX_Name, AudioClip>();
    Dictionary<BGM_Name, AudioClip> bgmDict = new Dictionary<BGM_Name, AudioClip>();

    AudioSource bgmAudioSource;
    AudioSource sfxAudioSource;
    AudioMixer m_AudioMixer;

    [SerializeField] float lowpassCutoffUnderSea = 400f;
    [SerializeField] float lowpassCutoffOnSea = 6500f;    

    public void Init()
    {        
        if(instance == null) { instance = this; }
        foreach (var sfx in ConfigFile.GetConfigFile().soundEffects)
        {
            sfxDict.Add(sfx.name, sfx.clip);
        }
        foreach (var bgm in ConfigFile.GetConfigFile().backgroundMusics)
        {
            bgmDict.Add(bgm.name, bgm.clip);
        }

        bgmAudioSource = GetComponents<AudioSource>()[0];
        sfxAudioSource = GetComponents<AudioSource>()[1];
        m_AudioMixer = bgmAudioSource.outputAudioMixerGroup.audioMixer;

        PlayBGM(BGM_Name.Normal);

        ChangeAudioLowpassCutoff(false);
    }


    public void PlayGlobalSFX(SFX_Name targetSFX, float volume = 1)
    {        
        sfxAudioSource.PlayOneShot(sfxDict[targetSFX], volume);
    }

    public void PlayLocalSFX(SFX_Name targetSFX, Vector3 playPosition, float volume = 1)
    {
        AudioSource.PlayClipAtPoint(sfxDict[targetSFX], playPosition, volume);
    }

    public void PlayObjectSFX(AudioSource objectAudioSource, SFX_Name targetSFX, float volume)
    {
        if (objectAudioSource.isPlaying)
        {
            return;
        }
        objectAudioSource.clip = sfxDict[targetSFX];
        objectAudioSource.volume = volume;
        objectAudioSource.Play();
    }

    public void PlayBGM(BGM_Name targetBGM)
    {        
        if (bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
        }
        bgmAudioSource.clip = bgmDict[targetBGM];
        bgmAudioSource.Play();
    }

    public void ChangeAudioLowpassCutoff(bool isUnderSea)
    {        
        m_AudioMixer.SetFloat("LowpassCutoff", isUnderSea ? lowpassCutoffUnderSea : lowpassCutoffOnSea);
    }
}
