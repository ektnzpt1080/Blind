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
    [SerializeField] Slider sliderHP, sliderRecoveryHP, sliderBossHP, sliderAttackGauge, sliderGuardGauage;
    float hpLerp, recoveryHPLerp, bossHPLerp, attackGaugeLerp, guardGauageLerp;
    float stamina;
    [SerializeField] Image fire0, fire1, fire2, fire3;
    [SerializeField] Animator fire0a, fire1a, fire2a, fire3a;

    [SerializeField] Color minColor, maxColor;
    Vector3 minScale, maxScale;
    [SerializeField] float t;

    void Start(){
        maxScale = new Vector3(1f,1f,1f);
        minScale = new Vector3(0.5f,0.5f,1f);

        hpLerp = player.GetMaxHealth();
        recoveryHPLerp = player.GetRecoveryHealth();
        stamina = 4;
        bossHPLerp = 0;
        guardGauageLerp = 1.05f;
        sliderBossHP.maxValue = boss.GetBossHPMax();
    }

    void Update(){
        hpLerp = Mathf.Lerp(hpLerp, player.GetPlayerHP(), Time.deltaTime * t);
        sliderHP.value = hpLerp;
        
        recoveryHPLerp = Mathf.Lerp(recoveryHPLerp, player.GetRecoveryHealth(), Time.deltaTime * t);
        sliderRecoveryHP.value = recoveryHPLerp;
        
        stamina = Mathf.Lerp(stamina, player.GetPlayerStamina(), Time.deltaTime * t);
        //staminaLerp = player.GetPlayerStamina();
        
        bossHPLerp = Mathf.Lerp(bossHPLerp, boss.GetBossHP(), Time.deltaTime * t);
        sliderBossHP.value = bossHPLerp;

        sliderAttackGauge.value = player.GetAttackGauge();
        
        guardGauageLerp = Mathf.Lerp(sliderGuardGauage.value, player.GetGuardGauge(), Time.deltaTime * t);
        sliderGuardGauage.value = guardGauageLerp;
        
        FireStamina(fire0, fire0a, 0, 1);
        FireStamina(fire1, fire1a, 1, 2);
        FireStamina(fire2, fire2a, 2, 3);
        FireStamina(fire3, fire3a, 3, 4);
        
    }

    void FireStamina(Image fire, Animator fireAnimation, float min, float max){
        if(stamina < min){
            fire.transform.localScale = minScale;
            fire.color = minColor;
            fireAnimation.enabled = false;
        }
        else if(stamina > max){
            fire.transform.localScale = maxScale;
            fire.color = maxColor;
            fireAnimation.enabled = true;
        }
        else{
            fire.transform.localScale = Vector3.Lerp(minScale, maxScale, stamina - min);
            fire.color = Color.Lerp(minColor, maxColor, stamina - min);
            fireAnimation.enabled = true;
        }
    }
}   
