using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AfterimageBehavior : MonoBehaviour
{
    [SerializeField] SpriteRenderer sr;
    private void Awake() {
        GameManager.Instance.DestructAfterimage.AddListener(DestroyThis);
    }

    public void DestroyThis(){
        transform.DOKill(false);
        sr.DOKill(false);
        Destroy(this.gameObject);
    }
}
