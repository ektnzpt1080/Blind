using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class UIManager : MonoBehaviour
{
    public PlayerBehaviour player;
    public Boss1Behaviour boss;
    [SerializeField] Slider sliderHP, sliderRecoveryHP, sliderBossHP, sliderAttackGauge, sliderStamina;
    float hpLerp, recoveryHPLerp, bossHPLerp, attackGaugeLerp, staminaLerp, guardGauage;
    [SerializeField] Image fire0, fire1, fire2, fire3;
    [SerializeField] Animator fire0a, fire1a, fire2a, fire3a;

    [SerializeField] Color minColor, maxColor, fireColor;
    Vector3 minScale, maxScale;
    [SerializeField] float t;
    [SerializeField] GameObject R, Thank;


    void Start(){
        maxScale = new Vector3(1f,1f,1f);
        minScale = new Vector3(0.5f,0.5f,1f);

        hpLerp = player.GetMaxHealth();
        recoveryHPLerp = player.GetRecoveryHealth();
        staminaLerp = 4;
        bossHPLerp = 0;
        guardGauage = 2.5f;
        sliderBossHP.maxValue = boss.GetBossHPMax();
        isPlaying = false;
    }

    void Update(){
        hpLerp = Mathf.Lerp(hpLerp, player.GetPlayerHP(), Time.deltaTime * t);
        sliderHP.value = hpLerp;
        
        recoveryHPLerp = Mathf.Lerp(recoveryHPLerp, player.GetRecoveryHealth(), Time.deltaTime * t);
        sliderRecoveryHP.value = recoveryHPLerp;
        
        staminaLerp = Mathf.Lerp(staminaLerp, player.GetPlayerStamina(), Time.deltaTime * t);
        sliderStamina.value = player.GetPlayerStamina();
        
        bossHPLerp = Mathf.Lerp(bossHPLerp, boss.GetBossHP(), Time.deltaTime * t);
        sliderBossHP.value = bossHPLerp;

        guardGauage = player.GetGuardGauge();
        
        FireStamina(fire0, fire0a, 0, 1);
        FireStamina(fire1, fire1a, 1, 2);
        FireStamina(fire2, fire2a, 2, 3);
        FireStamina(fire3, fire3a, 3, 4);
        
    }

    void FireStamina(Image fire, Animator fireAnimation, float min, float max){
        if(min < 0.5f && isPlaying && guardGauage < max){
            return;
        }
        else if(guardGauage < min){
            fire.transform.localScale = minScale;
            fire.color = minColor;
            fireAnimation.enabled = false;
        }
        else if(guardGauage > max){
            fire.transform.localScale = maxScale;
            fire.color = maxColor;
            fireAnimation.enabled = true;
        }
        else{
            fire.transform.localScale = Vector3.Lerp(minScale, maxScale, guardGauage - min);
            fire.color = minColor;
            fireAnimation.enabled = false;
        }
    }
    
    bool isPlaying;
    public void FireNotEnough(){
        StartCoroutine(FireNotEnough_());
    }
    
    IEnumerator FireNotEnough_(){
        if(isPlaying) yield break;
        
        isPlaying = true;
        for (int i = 0 ; i < 4; i++){
            fire0.color = fireColor;
            yield return new WaitForSeconds(0.2f);
            fire0.color = minColor;
            yield return new WaitForSeconds(0.2f);
        }
        isPlaying = false;
    }
    
    public void PressRToRestart(){
        R.SetActive(true);
    }

    public void ThankYouForPlaying(){
        Thank.SetActive(true);
    }

}   
