using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class PlayerTutorial : MonoBehaviour
{
    public GameObject textbox;
    public TextMeshProUGUI text;
    SpriteRenderer sr;
    public MasterTutorial mt;
    public BossTutorial bt;
    int index;
    float t;
    List<bool> boolList;
    Animator animator;
    
    Vector2 mouseVec;
    public Camera mainCamera;
    public GameObject parryEffect;
    
    enum PlayerState{
        Idle, //가는 방향
        Attack, //마우스 방향
        Damaged, // 데미지 받은 방향
        Roll, // 가는 방향
        Running, // 가는 방향
        Guard, //마우스 방향
        QTE, // 돌진 방향
        OnlyAttack, // 보스 방향
        Dead // 무조건 오른쪽
    }

    PlayerState playerstate;
    List<Attack> attackList;

    public List<string> stringList;

    void Start(){
        boolList = new List<bool>{false};
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        index = -1;
        t = 1f;
        NextWork();
    }

    void Update(){
        if(Input.GetMouseButton(0)) t = 2f;
        else t = 1f;

        if(boolList[0] && Input.GetMouseButtonDown(1)){
            if (playerstate == PlayerState.Idle)
            {
                animator.SetTrigger("Guard");
                playerstate = PlayerState.Guard;
                Parrying();
            }
        }
        
        if(boolList[0] && Input.GetMouseButtonUp(1)){
            if (playerstate == PlayerState.Guard) {
                playerstate = PlayerState.Idle;
                animator.SetTrigger("Idle");
            }
        }

        if(attackList[0].time > Time.time) {
            attackList.RemoveAt(0);
        }

        mouseVec = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
    
    }

    void Parrying(){
        if(attackList.Count > 0 && attackList[0].attacktype == Pattern.AttackType.white && Vector2.Angle(-attackList[0].attackDirection, mouseVec) < 75){
            
            Instantiate(parryEffect, (transform.position + bt.transform.position) / 2, Quaternion.identity);
            attackList.RemoveAt(0);
        }
    }

    public void AddAttack( Attack a){
        attackList.Add(a);
    }

    

    public void Dialogue(string s){
        textbox.SetActive(true);
        StartCoroutine(Typing(text, s));
    }

    public void a255(){
        Color temp = sr.color;
        temp.a = 1;
        sr.color = temp;
    }

    IEnumerator Typing(TextMeshProUGUI typingText, string message, float speed = 0.05f) {
        for (int i = 0; i < message.Length; i++) {
            typingText.text = message.Substring(0, i + 1);
            yield return new WaitForSeconds(speed / t);
        }
        yield return new WaitForSeconds(3f / t);
        textbox.SetActive(false);
        NextWork();
    }

    public void NextWork(){
        index++;
        Debug.Log(index);
        Debug.Log(stringList[index]);
        if(stringList[index] == "") mt.NextWork();
        else Dialogue(stringList[index]);
    }

    public void B0(){
        boolList[0] = true;
    }

}
