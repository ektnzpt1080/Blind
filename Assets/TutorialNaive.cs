using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class TutorialNaive : MonoBehaviour
{
    int i;
    public TextMeshPro left;
    public TextMeshPro right;
    bool canMove;
    public GameObject p0,p1,p2,p3;
    void Start(){
        i = 0;
        canMove = true;
    }
    // Update is called once per frame
    void Update()
    {
        if(canMove){
            if(Input.GetMouseButtonDown(0)){
                Forward();
            }
            if(Input.GetMouseButtonDown(1)){
                Backward();
            }
        }
        
        if (i == 0){
            right.text = "<color=grey>이전 페이지";
        }
        else right.text = "이전 페이지";
        
        if (i == 3){
            left.text = "메인 메뉴로";
        }
        else {
            left.text = "다음 페이지";
        }
    }

    void Backward(){
        if(i == 0){
            return;
        }
        else{
            i--;
            canMove = false;
            p0.transform.DOMoveX(20,1.2f).SetEase(Ease.Linear).SetRelative();
            p1.transform.DOMoveX(20,1.2f).SetEase(Ease.Linear).SetRelative();
            p2.transform.DOMoveX(20,1.2f).SetEase(Ease.Linear).SetRelative();
            p3.transform.DOMoveX(20,1.2f).SetEase(Ease.Linear).SetRelative().OnComplete(() => {
                canMove = true;
            });
        }
    }
    void Forward(){
        if(i == 3){
            SceneManager.LoadScene("MainMenu");
        }
        else{
            i++;
            canMove = false;
            p0.transform.DOMoveX(-20,1.2f).SetEase(Ease.Linear).SetRelative();
            p1.transform.DOMoveX(-20,1.2f).SetEase(Ease.Linear).SetRelative();
            p2.transform.DOMoveX(-20,1.2f).SetEase(Ease.Linear).SetRelative();
            p3.transform.DOMoveX(-20,1.2f).SetEase(Ease.Linear).SetRelative().OnComplete(() => {
                canMove = true;
            });
        }
    }
}
