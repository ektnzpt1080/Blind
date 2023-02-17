using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueAttack
{
    public int damage;
    public float time; // 언제 blue attack의 기한이 끝나는지
    public Vector3 startPoint;
    public Vector3 endPoint;
    public Vector3 attackDirection;

    public BlueAttack(Vector3 startpoint, Vector3 endpoint, int damage, float time){
        this.damage = damage;
        this.time = time;
        startPoint = startpoint;
        endPoint = endpoint;
        attackDirection = (endPoint - startPoint).normalized;
    }

    public BlueAttack(Vector2 startpoint, Vector2 endpoint, int damage, float time){
        this.damage = damage;
        this.time = time;
        startPoint = startpoint;
        endPoint = endpoint;
        attackDirection = (endPoint - startPoint).normalized;
    }
}
