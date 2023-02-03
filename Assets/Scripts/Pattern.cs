using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pattern 
{
    public enum AttackType {
        white,
        red,
        blue
    }
    public AttackType attacktype;
    public float chaseDistance; // 패턴을 시작하는 거리
    public float stanbyTime; // 패턴 대기 시간
    public float attackDistance; // 공격 범위
    public float maxDashDistance; // 최대 돌진 사거리
    public float properDashDistance; // 적정 거리
    public bool damageStopPattern; // 데미지가 패턴을 멈추는지 여부
    public bool parryStopPattern; // 패링으로 QTE를 만들 수 있는지 여부
    
    public List<Sprite> preAttackSprites;
    public List<Sprite> attackSprites;
    public List<int> attackDamage;
    
}
