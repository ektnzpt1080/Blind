using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class BlackOut_boss : MonoBehaviour
{
    public Image i;

    void Start(){
        i.DOFade(0, 2f);
    }
}
