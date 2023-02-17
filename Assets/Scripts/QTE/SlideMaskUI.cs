using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideMaskUI : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector3 farLeft;
    private Vector3 farRight;
    private bool isEnd;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        farLeft = rectTransform.position - new Vector3(rectTransform.rect.width, 0f);
        farRight = rectTransform.position;
        isEnd = false;
    }
    
    private void Start(){
        rectTransform.position = farLeft;
    }
    
    private void Update() {
        if(rectTransform.position.x < farRight.x)
            rectTransform.Translate(Vector3.right * 20f);
        if(isEnd)
            rectTransform.Translate(Vector3.right * 20f);
    }

    public void EndQTEMask(){
        isEnd = true;
    }

    
}
