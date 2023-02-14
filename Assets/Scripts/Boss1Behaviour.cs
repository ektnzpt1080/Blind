using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Attacktype = Pattern.AttackType;

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
    
    [SerializeField] List<Pattern> boss1PatternList; // 패턴 리스트
    [SerializeField] Vector2 border; //Border Vector
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
    

    Pattern lastPattern; // 현재 진행중인 Pattern
    Coroutine currentPattern; // 현재 진행중인 Pattern 코루틴
    bool isDamageStopPattern; // 현재 진행중인 Pattern이 데미지 받으면 패턴이 종료되는 지
    bool isParryStopPattern; // 현재 진행중인 pattern이 패링으로 QTE모드를 발동시키는 지
    bool defenselessEnd; //Boss Defenseless 끝나는 트리거
    bool footprintEnable; //Boss Footprint 출력 여부
    Vector2 footprintDirection; //Footprint 방향
    int bossHP; // Boss HP
    int bossStaminaMax; // boss 스태미나 max
    int bossStamina; // QTE에 들어가는 boss의 스태미나
    SpriteRenderer sr; //spriterenderer

    //for debug
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
        
        if(Input.GetKeyDown(KeyCode.X)){
            debugColor = !debugColor;
            Color tmp = sr.color;
            tmp.a = 1 - tmp.a;
            sr.color = tmp;
        }
        
        float x = Mathf.Clamp(transform.position.x, -border.x, border.x);
        float y = Mathf.Clamp(transform.position.y, -border.y, border.y);
        transform.position = new Vector3(x,y,0);

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
            //if( VectorBtoP().magnitude < lastPattern.startDistance ){
            if(VectorBtoP().magnitude < 15f ){
                currentPattern = StartCoroutine(Pattern6());
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

    //나중에 attacksprite 받게 할 것
    void MakeAttackSprite(){
        GameObject attackSprite = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        attackSprite.GetComponent<SpriteRenderer>().DOFade(0, 1.7f).SetEase(Ease.InQuint).OnComplete(() => {
            Destroy(attackSprite);
        });
    }   

    // boss가 player에게 최대 max distance만큼 돌진 후 attackdistance만큼 attacktype으로 공격, 
    // proper distance만큼 돌진하려고 함, proper distance보다 가까우면 현재 자리에서 시전
    void AttackPattern1(float maxdistance, float properdistance, float attackdistance, Attacktype attacktype, int attackdamage){
        Vector3 v = VectorBtoP().normalized;
        Vector3 maxV = v * maxdistance;
        Vector3 properV = VectorBtoP() - v * properdistance;
        if(Vector3.Dot(properV, v) < 0) properV = Vector3.zero; // 너무 가까우면 제자리에서 패턴 진행
        Vector3 pos = (maxV.magnitude < properV.magnitude) ? maxV : properV;
        transform.Translate(pos);
        if (VectorBtoP().magnitude <= attackdistance){
            player.Attacked(transform.position, attackdamage, attacktype);
        }
    }
    

    // 0 흰-흰-흰 정박
    IEnumerator Pattern0(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[0];
        int i;
        int maxAttack = 3;
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]

        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[0]));

        for(i = 0 ; i < maxAttack; i++){
            isDamageStopPattern = pattern.damageStopPattern[i];
            isParryStopPattern = pattern.parryStopPattern[i];

            Instantiate(preattackEffect, transform.position, Quaternion.identity);
            yield return new WaitForSeconds(pattern.stanbyTime[i]);
            
            AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
            MakeAttackSprite();
        }

        StartCoroutine(DefenselessStart());
    }

    // 1 흰흰 - 흰
    IEnumerator Pattern1(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[1];
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        int i;
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]

        i = 0;
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[0] + 0.2f));        
        
        isDamageStopPattern = pattern.damageStopPattern[0];
        isParryStopPattern = pattern.parryStopPattern[0];

        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.2f);
        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        i = 1;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 2;
        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);

        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        StartCoroutine(DefenselessStart());
    }

    // 1 - 2 흰흰 - 흰흰 - 흰
    IEnumerator Pattern2(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[2];
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        int i;
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]

        i = 0;
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[i] + 0.2f));        

        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.2f);
        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 1;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        i = 2;
        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.2f);
        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 3;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 4;
        Instantiate(preattackEffect, transform.position, Quaternion.identity);                
        yield return new WaitForSeconds(pattern.stanbyTime[i]);

        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        StartCoroutine(DefenselessStart());
    }

    // 세부조정
    // 3 흰 - 흰(엇박)
    IEnumerator Pattern3(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[3];
        int i;
        int maxAttack = 2;
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]

        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[0]));
        
        for(i = 0 ; i < maxAttack; i++){
            isDamageStopPattern = pattern.damageStopPattern[i];
            isParryStopPattern = pattern.parryStopPattern[i];

            Instantiate(preattackEffect, transform.position, Quaternion.identity);
            yield return new WaitForSeconds(pattern.stanbyTime[0]);
            
            AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
            MakeAttackSprite();
        }

        StartCoroutine(DefenselessStart());
    }

    // 세부조정
    // 4 흰 - 옆으로 돌아서 흰
    IEnumerator Pattern4(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[4];
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        int i;
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]

        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[0]));

        
        isDamageStopPattern = pattern.damageStopPattern[0];
        isParryStopPattern = pattern.parryStopPattern[0];

        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(pattern.stanbyTime[0]);
        
        i = 0;
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 1;
        yield return new WaitForSeconds(pattern.stanbyTime[1]);
        int direction = Random.Range(0,2) * 2 - 1;
        Vector3 moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * direction).normalized * pattern.properDashDistance[i];
        if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
            moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * - direction).normalized * pattern.properDashDistance[i];
            if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
                StartCoroutine(DefenselessStart());
                yield break;
            }
        }

        i = 2;        
        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(pattern.stanbyTime[2]);

        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        StartCoroutine(DefenselessStart());
    }

    [SerializeField] float redMaxDistance;
    //세부 조정
    // 5 돌진 - (빨강 공격) (멀리서 공격)
    IEnumerator Pattern5(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[5];
        
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[0]));
        Instantiate(preattackEffect, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(pattern.stanbyTime[0]);
        
        Vector3 startposition = transform.position;
        Vector3 v = VectorBtoP().normalized * pattern.maxDashDistance[0];
        transform.Translate(v);

        RaycastHit2D hit = Physics2D.Raycast(startposition, transform.position - startposition, (transform.position - startposition).magnitude, LayerMask.GetMask("Player"));
        if(hit.collider != null){
            player.Attacked(startposition, pattern.attackDamage[0], Pattern.AttackType.red);
        }

        GameObject attackSprite = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        attackSprite.GetComponent<SpriteRenderer>().DOFade(0, 1.7f).SetEase(Ease.InQuint).OnComplete(() => {
            Destroy(attackSprite);
        });

        StartCoroutine(DefenselessStart());
    }
    
    // 세부조정
    // 6 흰 - 흰 (멀리서) (멀리서 흰색 -> 발자국으로 가까워짐 -> attack)
    IEnumerator Pattern6(){
        Pattern pattern = boss1PatternList[6];
        int i;
        
        float original_fpct = footprintCoolTime;
        footprintCoolTime = 0.15f;
        
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        i = 0;
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[i]));
        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);

        while(VectorBtoP().magnitude > pattern.properDashDistance[i]) {
            Vector3 v = VectorBtoP();
            footprintDirection = v;
            transform.Translate(new Vector3(v.x, v.y, 0).normalized * 9.8f * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }
        FootprintEnable(false);

        footprintCoolTime = original_fpct;
        
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 1;
        Instantiate(preattackEffect, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        StartCoroutine(DefenselessStart());
    }
    public float blueattack1;
    public float blueattack2;
    //파랑 공격
    IEnumerator Pattern003(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[0];
        
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image, blueattack1));
        Instantiate(preattackEffect, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(blueattack1);
        
        player.AttackedBlue(transform.position, player.transform.position, 10, blueattack2);
        StartCoroutine(DefenselessStart());
        
    }


    public void FootprintEnable(bool tf){
        footprintEnable = tf;
    }

    public Vector3 VectorBtoP(){
        return (player.transform.position - transform.position);
    }

    IEnumerator DefenselessStart( float time = -1f){
        enemystate = EnemyState.Defenseless;
        if(time > 0) yield return new WaitForSeconds(time);
        else yield return new WaitForSeconds(defenselessTime);
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
