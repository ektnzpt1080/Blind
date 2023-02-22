using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class MasterTutorial : MonoBehaviour
{
    public GameObject textbox;
    public TextMeshProUGUI text;
    SpriteRenderer sr;
    public PlayerTutorial pt;
    public BossTutorial bt;
    int index;
    float t;

    public GameObject parryEffect;


    public List<string> stringList;

    private void Start() {
        sr = GetComponent<SpriteRenderer>();
        index = -1;
        t = 1f;
    }

    void Update(){
        if(Input.GetMouseButton(0)) t = 2f;
        else t = 1f;
    }

    public void Dialogue(string s){
        textbox.SetActive(true);
        sr.DOKill(false);
        a255();
        sr.DOFade(0, 10f);
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
        
        if(index == 10) StartCoroutine(Index10());
        else if(index == 13) StartCoroutine(Index13());
        else if(stringList[index] == "") pt.NextWork();
        else Dialogue(stringList[index]);
    }

    IEnumerator Index10(){
        Instantiate(parryEffect, bt.transform.position, Quaternion.identity);
        yield return new WaitForSeconds(3f);
    }

    int parried13;
    IEnumerator Index13(){
        pt.B0();
        while(true){
            Instantiate(parryEffect, bt.transform.position, Quaternion.identity);
            pt.AddAttack(new Attack(bt.transform.position, pt.transform.position, 0, Time.time + 4f, Pattern.AttackType.white));
            yield return new WaitForSeconds(5f);
        }
    }


}
