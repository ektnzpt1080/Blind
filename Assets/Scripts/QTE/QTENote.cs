using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QTENote : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    Vector3 startPosition;
    bool isLock;
    bool isFirst;
    bool isFirst2;
    bool isEnable;
    QTEJudgeLine judge;
    [SerializeField] UnityEngine.UI.Image image;

    void Awake(){
        isLock = false;
        isFirst = false;
        isEnable = true;
    }  


    public void Set_x(float x){
        Vector2 tmp = rectTransform.anchoredPosition;
        tmp.x = x;
        rectTransform.anchoredPosition = tmp;
        startPosition = rectTransform.position;
        isLock = true;
    }

    void Update(){
        if(isEnable){
            if(judge.transform.position.x > transform.position.x + rectTransform.rect.width/2){
                Judge(false);
            }
            if(judge.transform.position.x < transform.position.x - rectTransform.rect.width/2 
                && judge.transform.position.x > transform.position.x - rectTransform.rect.width*7/8 && isFirst2){
                if(Input.GetMouseButtonDown(0)){
                    Judge(false);
                }
            }
            if(judge.transform.position.x < transform.position.x + rectTransform.rect.width/2 
                && judge.transform.position.x > transform.position.x - rectTransform.rect.width/2){
                if(Input.GetMouseButtonDown(0)){
                    Judge(true);
                }
            }
        }
    }

    private void LateUpdate() {
        if (isLock)
        {
            rectTransform.position = startPosition;
        }    
        isFirst2 = isFirst;
    }


    public void SetFirst(){
        isFirst = true;
    }

    public void SetJudge(QTEJudgeLine j){
        judge = j;
    }

    public void Judge(bool tf){
        QTEBoard board = transform.parent.parent.GetComponent<QTEBoard>();
        isEnable = false;
        board.NextNote(tf);
        if(tf){
            image.color = Color.green;
        }
        else{
            image.color = Color.red;
        }
    }




}
