using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QTEJudgeLine : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] Image image;
    [SerializeField] float x_last = 700f;
    [SerializeField] float x_fade = -1024f;
    
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        rectTransform.localPosition += Vector3.right * speed * Time.deltaTime;
        if(x_fade > -1000f) AlphaColor( Mathf.Lerp(1f, 0f, (rectTransform.anchoredPosition.x - x_fade) / (x_last - x_fade)));
    }

    void AlphaColor(float x){
        Color temp = image.color;
        temp.a = x;
        image.color = temp;
    }

    public void end(){
        x_fade = rectTransform.anchoredPosition.x;
    }
}
