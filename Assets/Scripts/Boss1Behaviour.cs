using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Boss1Behaviour : MonoBehaviour
{
    enum EnemyState{
        Ready,
        Start,
        Damaged,
        Defenseless,
        Guard
    }

    Animator animator;
    EnemyState enemystate;
    
    [SerializeField] List<Pattern> boss1PatternList;
    Pattern lastPattern;
    Coroutine currentPattern;

    [SerializeField] PlayerBehaviour player; //Player
    [SerializeField] BanishingObject footPrintObject; //Boss 발자국
    [SerializeField] GameObject afterimage; // 잔상
    [SerializeField] GameObject preattackEffect; // 공격전 이펙트

    [SerializeField] float walkSpeed; //Boss가 움직이는 속도 (Walk)
    [SerializeField] float walkToPatternTime; //Boss가 Walk -> Pattern 에 들어가는 시간
    [SerializeField] float walkToPatternDistance; //Boss가 Walk -> Pattern 에 들어가는 거리
    [SerializeField] float walkMaxTime; //Boss가 Walk -> Chase에 들어가는 시간
    [SerializeField] float chaseSpeed; //Boss가 움직이는 속도 (Chase)
    [SerializeField] float footprintCoolTime; //Footprint 간격시간
    [SerializeField] float footprintTime; //Footprint 지속시간
    [SerializeField] float defenselessTime; //Defenseless 지속시간
    
    [SerializeField] bool defenselessEnd; //Boss Defenseless 끝나는 트리거
    [SerializeField] bool footprintEnable; //Boss Footprint 출력 여부
    Vector2 footprintDirection; //Footprint 방향

    int bossHP; // Boss HP


    SpriteRenderer sr;

    bool debugColor;
    
    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        enemystate = EnemyState.Ready;
        StartCoroutine(FootPrint());
        footprintEnable = true;
        debugColor = true;
        bossHP = 1000;
    }

    // Update is called once per frame
    void Update()
    {
        if(defenselessEnd){
            defenselessEnd = false;
            if(enemystate == EnemyState.Defenseless){
                enemystate = EnemyState.Ready;
            }
        }
        
        else if(enemystate == EnemyState.Ready){
            enemystate = EnemyState.Start;
            StartCoroutine(Walk());
        }
        
        //Debug
        if(Input.GetKeyDown(KeyCode.Z)){
            StartCoroutine(Pattern1());
        }
        if(Input.GetKeyDown(KeyCode.X)){
            debugColor = !debugColor;
            Color tmp = sr.color;
            tmp.a = 1 - tmp.a;
            sr.color = tmp;
        }
        
        if(debugColor){
            EnemyColor();
        }
        
    }
    
    IEnumerator FootPrint(){
        int d = 1;
        float offset = 0.2f;
        while(true){
            if(footprintEnable){
                Vector3 dv3 = footprintDirection;
                Vector3 v = Vector3.Cross(dv3, Vector3.forward).normalized;
                BanishingObject footprint = Instantiate(footPrintObject, this.transform.position + v * d * offset, Quaternion.FromToRotation(Vector3.right, footprintDirection));
                
                if( d == 1) footprint.transform.localScale = Vector3.Scale(footprint.transform.localScale, new Vector3(1f, -1f, 1f));
                d *= -1; 
                yield return new WaitForSeconds(footprintCoolTime);
            }
            else{
                yield return new WaitForFixedUpdate();
            }
            
        }
    }

    //player와 수직 방향으로 걸음
    IEnumerator Walk(){
        FootprintEnable(true);
        int direction = Random.Range(0,2) * 2 - 1;
        float walkStartTime = Time.time;
        Vector3 v = Vector3.Cross(transform.position - player.transform.position, Vector3.forward * direction);

        while(true){
            if(Time.time - walkStartTime > walkMaxTime) {
                StartCoroutine(Chase());
                break;
            }
            else if(Time.time - walkStartTime > walkToPatternTime && false) {
                //TODO : Pattern 사용하게 만들 것 & 거리 따지게 만들 것
                break;
            }
            else {
                footprintDirection = v;
                transform.Translate(new Vector3(v.x, v.y, 0).normalized * walkSpeed * Time.deltaTime);
                yield return new WaitForFixedUpdate();
            }
            
        }
    }

    IEnumerator Chase(){
        FootprintEnable(true);
        //TODO : 패턴을 랜덤으로 뽑는 함수 만들것
        lastPattern = boss1PatternList[0];
        
        while(true){
            if( VectorBtoP().magnitude < lastPattern.startDistance ){
                currentPattern = StartCoroutine(Pattern2());
                break;
            }
            else {
                Vector3 v = VectorBtoP();
                footprintDirection = v;
                transform.Translate(new Vector3(v.x, v.y, 0).normalized * chaseSpeed * Time.deltaTime);
                yield return new WaitForFixedUpdate();
            }
        }
    }

    IEnumerator Pattern1(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[0];
        
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime));
        //TODO : 공격준비 알림 좀 더 디테일하게

        yield return new WaitForSeconds(pattern.stanbyTime);
        Vector3 v = VectorBtoP().normalized;
        Vector3 maxV = v * pattern.maxDashDistance;
        Vector3 properV = VectorBtoP() - v * pattern.properDashDistance;
        if(Vector3.Dot(properV, v) < 0) properV = Vector3.zero; // 너무 가까우면 제자리에서 패턴 진행
        Vector3 pos = (maxV.magnitude < properV.magnitude) ? maxV : properV;
        transform.Translate(pos);
        if (VectorBtoP().magnitude <= pattern.attackDistance){
            player.Attacked(transform.position, 10, true);
        }

        GameObject attackSprite = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        attackSprite.GetComponent<SpriteRenderer>().DOFade(0, 1.7f).SetEase(Ease.InQuint).OnComplete(() => {
            Destroy(attackSprite);
        });

        StartCoroutine(DefenselessStart());
        
    }

    
    IEnumerator Pattern2(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[0];
        int maxAttack = 3;
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime));
        //TODO : 공격준비 알림 좀 더 디테일하게

        for(int i = 0 ; i < maxAttack; i++){
            //Instantiate(preattackEffect, transform.position + Vector3.down * 0.4f, Quaternion.identity);
            Instantiate(preattackEffect, transform.position, Quaternion.identity);
            yield return new WaitForSeconds(pattern.stanbyTime);
            Vector3 v = VectorBtoP().normalized;
            Vector3 maxV = v * pattern.maxDashDistance;
            Vector3 properV = VectorBtoP() - v * pattern.properDashDistance;
            if(Vector3.Dot(properV, v) < 0) properV = Vector3.zero; // 너무 가까우면 제자리에서 패턴 진행
            Vector3 pos = (maxV.magnitude < properV.magnitude) ? maxV : properV;
            transform.Translate(pos);
            if (VectorBtoP().magnitude <= pattern.attackDistance){
                player.Attacked(transform.position, 10, true);
            }

            GameObject attackSprite = Instantiate(afterimage, transform.position, Quaternion.identity);
            // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
            // aiSR.sprite = pattern.preAttackSprites[0]
            attackSprite.GetComponent<SpriteRenderer>().DOFade(0, 1.7f).SetEase(Ease.InQuint).OnComplete(() => {
                Destroy(attackSprite);
            });
        }
        
        StartCoroutine(DefenselessStart());
        
    }


    public void FootprintEnable(bool tf){
        footprintEnable = tf;
    }

    public Vector3 VectorBtoP(){
        return (player.transform.position - transform.position);
    }

    IEnumerator DefenselessStart(){
        enemystate = EnemyState.Defenseless;
        yield return new WaitForSeconds(defenselessTime);
        defenselessEnd = true;
    }

    public void EnemyColor(){
        if(enemystate == EnemyState.Defenseless){
            sr.color = Color.yellow;
        }
        else {
            sr.color = Color.white;
        }
    }

    IEnumerator DestoryAfterimage( GameObject afterimage, float duration){
        yield return new WaitForSeconds(duration);
        if(afterimage != null) Destroy(afterimage);
    }

    IEnumerator CeasePattern(){
        StopCoroutine(currentPattern); 
        yield return new WaitForSeconds(5f);
        enemystate = EnemyState.Ready;
    }

    public void damaged(){
        bossHP -= 10;
    }

    public int GetBossHP(){
        return bossHP;
    }
}
