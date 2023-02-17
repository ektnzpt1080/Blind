using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class UIManager : MonoBehaviour
{

    public TextMeshProUGUI playerHP, playerStamina, bossHP, bossStamina;
    public PlayerBehaviour player;
    public Boss1Behaviour boss;

    void Update(){
        playerHP.text = "HP : " + player.GetPlayerHP() + " / 1000";
        float s = player.GetPlayerStamina();
        if (s > 4){
            playerStamina.text = "Stamina : 4 / 4";    
        }
        else {
            playerStamina.text = "Stamina : " + player.GetPlayerStamina().ToString("F2") + " / 4";
        }
        bossHP.text = "Boss HP : "+ boss.GetBossHP() + " / 1000";
        bossStamina.text = "Boss Stamina : "+ boss.GetBossStamina() + " / " + boss.GetBossMaxStamina();
    }
}   
