using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class Select : MonoBehaviour
{
    public Camera mainCamera;
    public Transform left, right;
    public SpriteRenderer black;
    int select;
    // Update is called once per frame
    bool selectable = true;
    void Start(){
        selectable = true;
    }

    void Update()
    {
        if(selectable){
            Vector3 f = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            if(left.position.x > f.x) select = 0; //start
            else if(right.position.x < f.x) select = 2; //exit
            else select = 1; //how to play

            if( select == 0 ) transform.position = new Vector3(-5.5f,-3f,0f);
            else if( select == 1 ) transform.position = new Vector3(0f,-3f,0f);
            else if( select == 2 ) transform.position = new Vector3(5.5f,-3f,0f);

            if(Input.GetMouseButtonDown(0)){
                if(select == 0){
                    black.DOFade(1, 2f).OnComplete(() =>{
                        SceneManager.LoadScene("Boss1");
                    });
                    
                }
                else if (select == 1){
                    SceneManager.LoadScene("Tutorial_naive");
                }
                else {
                    Application.Quit();
                }
            }
        }
        
    }

}
