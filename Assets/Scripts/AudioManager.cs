using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource sfxSource;
    [SerializeField]
    private AudioSource bgmSource;
    
    public void PlayClipInstance(AudioClip clip) 
    {
        if (!sfxSource || !clip) 
        { 
            return;
        };

        sfxSource.PlayOneShot(clip);
    }

    public IEnumerator PlayClipDelay(AudioClip clip, float delay) {
        yield return new WaitForSeconds(delay);
        PlayClipInstance(clip);
    }

    public void ChangeBGM(AudioClip bgm) {
        bgmSource.clip = bgm;
        bgmSource.Play();
    }
}
