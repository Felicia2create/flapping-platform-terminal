using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Audio : MonoBehaviour 
{
    public AudioClip audioClip01;
    public AudioClip audioClip02;

    private AudioSource audioSource;

    bool audio01;
    void Start() 
    {
        audioSource = this.GetComponent<AudioSource>(); // get audio component
        audio01 = true;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.R)) resetAudio();
    }
        void Open_Mouth01()
    {
        if (audio01)
        {
            eagleSound(audioClip01, 1.0f, 1.0f, 0.5f, 0.0f, 1.0f, false);
        } else
        {
            eagleSound(audioClip02, 0.85f, 1.15f, 0.5f, 1.1f, 2.0f, true);
        }
    }

    void eagleSound(AudioClip clip, float vol, float pitch, float blend, float rev, float doppler, bool B)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[Audio] eagleSound: audioSource 为空，跳过播放");
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning("[Audio] eagleSound: clip 为空，跳过播放");
            return;
        }
        audioSource.volume = vol;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = blend;
        audioSource.reverbZoneMix = rev;
        audioSource.dopplerLevel = doppler;
        audioSource.PlayOneShot(clip);

        audio01 = B;
    }

    void resetAudio()
    {
        audio01 = true;
    }
}
