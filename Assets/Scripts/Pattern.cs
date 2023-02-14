using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Pattern")]
public class Pattern : ScriptableObject 
{
    public enum AttackType {
        white,
        red,
        blue
    }
    //갈아엎어야 됨 죄다 리스트로 바꿔야 할 듯
    public List<AttackType> attacktype;
    public float startDistance; // 패턴을 시작하는 거리
    public List<float> stanbyTime; // 패턴 대기 시간
    public List<float> attackDistance; // 공격 범위
    public List<float> maxDashDistance; // 최대 돌진 사거리
    public List<float> properDashDistance; // 적정 거리
    public List<bool> damageStopPattern; // 데미지가 패턴을 멈추는지 여부
    public List<bool> parryStopPattern; // 패링으로 QTE를 만들 수 있는지 여부
    
    public List<Sprite> preAttackSprites;
    public List<Sprite> attackSprites;
    public List<int> attackDamage;
    
}
