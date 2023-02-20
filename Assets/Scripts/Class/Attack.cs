using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Attacktype = Pattern.AttackType;

public class Attack
{
    public int damage;
    public float time; // 언제 attack의 기한이 끝나는지
    public Vector3 startPoint;
    public Vector3 endPoint;
    public Vector3 attackDirection;
    public float attackDistance;
    public Attacktype attacktype;
    

    public Attack(Vector3 startpoint, Vector3 endpoint, int damage, float time, Attacktype attacktype){
        this.damage = damage;
        this.time = time;
        startPoint = startpoint;
        endPoint = endpoint;
        attackDirection = (endPoint - startPoint).normalized;
        this.attacktype = attacktype;
        attackDistance = (endpoint - startpoint).magnitude;
    }
    public Attack(Vector3 startpoint, Vector3 direction, float distance, int damage, float time, Attacktype attacktype){
        this.damage = damage;
        this.time = time;
        startPoint = startpoint;
        attackDirection = direction;
        attackDistance = distance;
        this.attacktype = attacktype;
        endPoint = startPoint + attackDistance * attackDirection;
    }

    public Attack(Vector2 startpoint, Vector2 endpoint, int damage, float time, Attacktype attacktype){
        this.damage = damage;
        this.time = time;
        startPoint = startpoint;
        endPoint = endpoint;
        attackDirection = (endPoint - startPoint).normalized;
        this.attacktype = attacktype;
        attackDistance = (endpoint - startpoint).magnitude;

    }
    
    public Attack(Vector2 startpoint, Vector2 direction, float distance, int damage, float time, Attacktype attacktype){
        this.damage = damage;
        this.time = time;
        startPoint = startpoint;
        attackDirection = direction;
        attackDistance = distance;
        this.attacktype = attacktype;
        endPoint = startPoint + attackDistance * attackDirection;
    }

}
