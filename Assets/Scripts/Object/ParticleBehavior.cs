using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ParticleBehavior : MonoBehaviour
{
    private void Awake() {
        GameManager.Instance.DestructAttackParticles.AddListener(DestroyThis);
    }

    public void DestroyThis(){
        transform.DOKill(false);
        Destroy(this.gameObject);
    }
}
