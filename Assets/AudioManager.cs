using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    public List<SFX> sfxList;
    public List<AudioClip> blue, red, white, charge_complete, charge_attack, kunai, parrying, streak, dash, guard;
    public AudioSource soundSource;
    
    public enum SoundType{
        blue,
        red,
        white,
        charge_complete,
        charge_attack,
        kunai,
        parrying,
        streak,
        dash,
        guard
    }

    void Start(){
        sfxList= new List<SFX>();
        sfxList.Add(new SFX(blue, SoundType.blue));
        sfxList.Add(new SFX(red, SoundType.red));
        sfxList.Add(new SFX(white, SoundType.white));
        sfxList.Add(new SFX(charge_complete, SoundType.charge_complete));
        sfxList.Add(new SFX(charge_attack, SoundType.charge_attack));
        sfxList.Add(new SFX(kunai, SoundType.kunai));
        sfxList.Add(new SFX(parrying, SoundType.parrying));
        sfxList.Add(new SFX(streak, SoundType.streak));
        sfxList.Add(new SFX(dash, SoundType.dash));
        sfxList.Add(new SFX(guard, SoundType.guard));

        

    }

    public void Play(SoundType st, float pitch = 1.0f)
	{
        AudioClip audioClip = sfxList[(int)st].Next();
        if (audioClip == null)
            return;
        soundSource.pitch = pitch;
        soundSource.PlayOneShot(audioClip);
	}



}

public class SFX
{
    List<AudioClip> sfxList;
    AudioManager.SoundType st;
    int i;

    public SFX(List<AudioClip> sfxList, AudioManager.SoundType st){
        this.sfxList = sfxList;
        this.st = st;
        i = 0;
    }

    public AudioClip Next(){
        int ret = i;
        i++;
        if(sfxList.Count <= i) i = 0;
        return sfxList[ret];
    }
}