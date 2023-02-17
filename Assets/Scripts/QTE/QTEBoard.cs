using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QTEBoard : MonoBehaviour
{
    [SerializeField] QTENote note;
    [SerializeField] SlideMaskUI mask;
    [SerializeField] QTEJudgeLine judge;
    [SerializeField] List<QTENote> listNote;
    PlayerQTE pqte;
    bool firstCall;
    public void StartQTEBoard(int notenum, PlayerBehaviour pb, PlayerQTE p){
        pqte = p;
        firstCall = true;
        List<float> listL = new List<float>{-450f, -350f, -250f, -150f, -50f};
        List<float> listR = new List<float>{550f, 450f, 350f, 250f, 150f, 50f};
        listNote = new List<QTENote>(notenum);
        
        QTENote temp_1 = Instantiate(note, mask.transform);
        temp_1.Set_x(listL[0]);
        temp_1.SetJudge(judge);
        listNote.Add(temp_1);
        listL.RemoveAt(0);

        for(int i = 0 ; i < notenum / 2 - 1; i++){
            QTENote temp = Instantiate(note, mask.transform);
            int ran = Random.Range(0, listL.Count);
            temp.Set_x(listL[ran]);
            temp.SetJudge(judge);
            listNote.Add(temp);
            listL.RemoveAt(ran);
        }

        for(int i = 0 ; i < notenum - notenum / 2 ; i++){
            QTENote temp = Instantiate(note, mask.transform);
            int ran = Random.Range(0, listR.Count);
            temp.Set_x(listR[ran]);
            temp.SetJudge(judge);
            listNote.Add(temp); 
            listR.RemoveAt(ran);
        }

        listNote.Sort((a,b) => a.transform.position.x.CompareTo(b.transform.position.x));
        listNote[0].SetFirst();
    }

    public void NextNote(bool tf){
        listNote.RemoveAt(0);
        pqte.GetPlayerBehaviour().QTEDash(firstCall, tf);
        firstCall = false;

        if(listNote.Count > 0 ) listNote[0].SetFirst();
        else {
            judge.end();
            StartCoroutine(EndQTEBoard());
        }
    }

    IEnumerator EndQTEBoard(){
        mask.EndQTEMask();
        pqte.EndQTE();
        yield return new WaitForSeconds(2.5f);
        Destroy(this.gameObject);
    }

}
