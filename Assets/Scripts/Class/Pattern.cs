using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Pattern")]
public class Pattern : ScriptableObject 
{
    public string explanation;

    public enum AttackType {
        white,
        red,
        blue,
        move
    }
    public List<AttackType> attacktype;
    public float startDistance; // 패턴을 시작하는 거리
    public List<float> stanbyTime; // 패턴 대기 시간
    public List<float> attackDistance; // 공격 범위
    public List<float> maxDashDistance; // 최대 돌진 사거리
    public List<float> properDashDistance; // 적정 거리
    public List<bool> damageStopPattern; // 데미지가 패턴을 멈추는지 여부
    public List<Sprite> preAttackSprites;
    public List<Sprite> attackSprites;
    public List<int> attackDamage;
}
