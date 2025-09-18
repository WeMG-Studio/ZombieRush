using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioSource[] audioPool;

    public float soundsVolume = 0.5f;
    [SerializeField] private AudioClip bgmClip;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        audioPool = new AudioSource[100];
    }

    public void ChangeVolume(float volume) //type = 1 Bgm / type = 0 SFX
    {
        soundsVolume = volume;
    }

    private IEnumerator DestroyAudio(GameObject audioGO, float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        Destroy(audioGO);
    }
    public void PlaySound(AudioClip _clip)
    {
        for (int i = 0; i < audioPool.Length; i++)
        {
            if (audioPool[i] == null || !audioPool[i].isPlaying)
            {
                GameObject go = new GameObject { name = _clip.name };
                audioPool[i] = go.AddComponent<AudioSource>();
                audioPool[i].transform.position = Camera.main.transform.position;
                audioPool[i].spatialBlend = 0.0f;
                audioPool[i].PlayOneShot(_clip, soundsVolume);


                StartCoroutine(DestroyAudio(go, _clip.length * 3.5f));
                return;
            }
        }
        return;
    }
    public void StopAllBGM()
    {
        for (int i = 0; i < audioPool.Length; i++)
        {
            if (audioPool[i] != null && audioPool[i].isPlaying && audioPool[i].loop)
            {
                audioPool[i].Stop();
                Destroy(audioPool[i].gameObject);
                audioPool[i] = null;
            }
        }
    }
    public void PlayBGM()
    {
        for (int i = 0; i < audioPool.Length; i++)
        {
            if (audioPool[i] == null || !audioPool[i].isPlaying)
            {
                GameObject go = new GameObject { name = bgmClip.name };
                audioPool[i] = go.AddComponent<AudioSource>();
                audioPool[i].transform.position = Camera.main.transform.position;
                audioPool[i].spatialBlend = 0.0f;

                audioPool[i].clip = bgmClip;
                audioPool[i].volume = soundsVolume;
                audioPool[i].Play();
                //audioPool[i].PlayOneShot(bgmClip, soundsVolume);
                audioPool[i].loop = true;

                return;
            }
        }
        return;
    }
    public void PlayBGM(AudioClip _clip)
    {
        for (int i = 0; i < audioPool.Length; i++)
        {
            if (audioPool[i] == null || !audioPool[i].isPlaying)
            {
                GameObject go = new GameObject { name = _clip.name };
                audioPool[i] = go.AddComponent<AudioSource>();
                audioPool[i].transform.position = Camera.main.transform.position;
                audioPool[i].spatialBlend = 0.0f;

                audioPool[i].clip = _clip;
                audioPool[i].volume = soundsVolume;
                audioPool[i].Play();
                //audioPool[i].PlayOneShot(bgmClip, soundsVolume);
                audioPool[i].loop = true;

                return;
            }
        }
        return;
    }

}