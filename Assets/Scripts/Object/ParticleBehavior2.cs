using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ParticleBehavior2 : MonoBehaviour
{
    private void Awake() {
        GameManager.Instance.DestructChargeParticles.AddListener(DestroyThis);
    }

    public void DestroyThis(){
        transform.DOKill(false);
        Destroy(this.gameObject);
    }
}
