using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Border : MonoBehaviour
{
    public GameObject player;
    public GameObject boss;

    [SerializeField] SpriteRenderer borderU; //up
    [SerializeField] SpriteRenderer borderD; //down
    [SerializeField] SpriteRenderer borderR; //right
    [SerializeField] SpriteRenderer borderL; //left
    
    [SerializeField] float minL;
    
    // Update is called once per frame
    void Update()
    {
        float distanceU = borderU.transform.position.y - player.transform.position.y;
        float distanceD = player.transform.position.y - borderD.transform.position.y;
        float distanceL = player.transform.position.x - borderL.transform.position.x;
        float distanceR = borderR.transform.position.x - player.transform.position.x;
    
        float distanceBU = borderU.transform.position.y - boss.transform.position.y;
        float distanceBD = boss.transform.position.y - borderD.transform.position.y;
        float distanceBL = boss.transform.position.x - borderL.transform.position.x;
        float distanceBR = borderR.transform.position.x - boss.transform.position.x;
    
    
        float a = AlphaValue(Mathf.Min(distanceU, distanceD, distanceL, distanceR, distanceBU, distanceBD, distanceBL, distanceBR));

        ChangeAlpha(borderU, a);
        ChangeAlpha(borderD, a);
        ChangeAlpha(borderR, a);
        ChangeAlpha(borderL, a);
    }

    public float AlphaValue( float dis ){
        if(minL <= dis) return 0f;
        else return 1 - (dis - 0.3f)/minL;
    }

    public void ChangeAlpha(SpriteRenderer sr, float a){
        Color tmp = sr.color;
        tmp.a = a;
        sr.color = tmp;
    }


}
