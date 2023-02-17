using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Attacktype = Pattern.AttackType;
using UnityEngine.Events;

public class Boss1Behaviour : MonoBehaviour
{
    enum EnemyState{
        Start,
        Defensive,
        SuperArmor,
        Defenseless,
        Special,
        QTEEnable,
        BackStep

    }
    
    boss1PatternExtracter b1pe;
    Animator animator;
    [SerializeField] EnemyState enemystate;
    
    [SerializeField] List<Pattern> boss1PatternList; // 패턴 리스트
    [SerializeField] Vector2 border; //Border Vector
    [SerializeField] PlayerBehaviour player; //Player
    [SerializeField] BanishingObject footPrintObject; //Boss 발자국
    [SerializeField] GameObject afterimage; // 잔상
    [SerializeField] GameObject preattackEffectW; // 공격전 이펙트 W
    [SerializeField] GameObject preattackEffectR; // 공격전 이펙트 R
    [SerializeField] GameObject preattackEffectB; // 공격전 이펙트 B
    [SerializeField] float walkSpeed; //Boss가 움직이는 속도 (Walk)
    [SerializeField] float walkToPatternTime; //Boss가 Walk -> Pattern 에 들어가는 시간
    [SerializeField] float walkToPatternDistance; //Boss가 Walk -> Pattern 에 들어가는 거리
    [SerializeField] float walkMaxTime; //Boss가 Walk -> Chase에 들어가는 시간
    [SerializeField] float chaseSpeed; //Boss가 움직이는 속도 (Chase)
    [SerializeField] float footprintCoolTime; //Footprint 간격시간
    [SerializeField] float footprintTime; //Footprint 지속시간
    [SerializeField] float defenselessTime; //Defenseless가 된 시간
    [SerializeField] int bossStaminaMax; // boss 스태미나 max
    [SerializeField] float streakTime = 5f; // streak가 끝나는 시간
    [SerializeField] float backStepSpeed; // backstep 속도
    [SerializeField] float backStepTime; // backstep 속도
    [SerializeField] int bossHPMax; // backstep 속도
    
    bool isDamageStopPattern; // 현재 진행중인 Pattern이 데미지 받으면 패턴이 종료되는 지
    bool isParryStopPattern; // 현재 진행중인 pattern이 패링으로 QTE모드를 발동시키는 지
    bool footprintEnable; //Boss Footprint 출력 여부
    Vector2 footprintDirection; //Footprint 방향
    int bossHP; // Boss HP
    int bossStamina; // boss의 스태미나 (QTE)
    int streak = 0; // 당하고 있는 연속공격 수
    float lastDamaged;
    SpriteRenderer sr; //spriterenderer


    //for debug
    bool debugColor;

    private void Awake() {
        b1pe = GetComponent<boss1PatternExtracter>();
        b1pe.StartPatternList(this);
        b1pe.patternList.Add(new PatternLink("Pattern0"));
        b1pe.patternList.Add(new PatternLink("Pattern1"));
        b1pe.patternList.Add(new PatternLink("Pattern2"));
        b1pe.patternList.Add(new PatternLink("Pattern3"));
        b1pe.patternList.Add(new PatternLink("Pattern4", 1f));
        b1pe.patternList.Add(new PatternLink("Pattern5", true));
        b1pe.patternList.Add(new PatternLink("Pattern6", true));
        b1pe.patternList.Add(new PatternLink("Pattern7"));
        b1pe.patternList.Add(new PatternLink("Pattern8", false, true));
        b1pe.patternList[2].advanced = (new PatternLink("Pattern9", false, true));
        b1pe.patternList[4].advanced = (new PatternLink("Pattern10", false, true));
        //b1pe.patternList.Add(new PatternLink("Pattern0"));

        b1pe.PreparePatterns();
    }    
    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        enemystate = EnemyState.Start;
        StartCoroutine(FootPrint());
        footprintEnable = true;
        debugColor = true;
        bossHP = bossHPMax;
        bossStamina = bossStaminaMax;
        lastDamaged = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if(enemystate == EnemyState.Start) StartCoroutine(Walk());
        
        if(Time.time > defenselessTime + 2f){
            if(enemystate == EnemyState.Defenseless){
                StartCoroutine(Walk());
            }
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

        if(streak != 0 && Time.time > lastDamaged + streakTime) streak = 0;
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
        enemystate = EnemyState.Defensive;
        
        float walkStartTime = Time.time;

        int direction;
        Vector3 v1 = Vector3.Cross(transform.position - player.transform.position, Vector3.forward * 1);
        Vector3 v2 = Vector3.Cross(transform.position - player.transform.position, Vector3.forward * -1);
        float angle1 = Vector2.Angle(transform.position, v1);
        float angle2 = Vector2.Angle(transform.position, v2);
        if(angle1 > angle2) direction = 1;
        else direction = -1;

        while(true){
            if(Time.time - walkStartTime > walkMaxTime) {
                StartCoroutine(Chase());
                break;
            }
            else if(Time.time - walkStartTime > walkToPatternTime && VectorBtoP().magnitude < walkToPatternDistance) {
                StartPattern(b1pe.ClosePattern(out int i));
                break;
            }
            else {
                Vector3 v = Vector3.Cross(transform.position - player.transform.position, Vector3.forward * direction);
                footprintDirection = v;
                transform.Translate(new Vector3(v.x, v.y, 0).normalized * walkSpeed * Time.deltaTime);
                yield return new WaitForFixedUpdate();
            }
        }
    }

    IEnumerator ShortWalk(){
        FootprintEnable(true);
        enemystate = EnemyState.Defensive;
        float walkStartTime = Time.time;

        while(true){
            Vector3 v;
            Vector3 v1 = Vector3.Cross(transform.position - player.transform.position, Vector3.forward * 1);
            Vector3 v2 = Vector3.Cross(transform.position - player.transform.position, Vector3.forward * -1);
            float angle1 = Vector2.Angle(transform.position, v1);
            float angle2 = Vector2.Angle(transform.position, v2);
            if(angle1 > angle2) v = v1;
            else v = v2;
            
            if(Time.time - walkStartTime > walkToPatternTime) {
                StartCoroutine(Chase());
                break;
            }
            else {
                footprintDirection = v;
                transform.Translate(new Vector3(v.x, v.y, 0).normalized * 2f * walkSpeed * Time.deltaTime);
                yield return new WaitForFixedUpdate();
            }
        }
    }

    IEnumerator Chase(){
        FootprintEnable(true);
        enemystate = EnemyState.SuperArmor;
        int i;
        string nextPattern = b1pe.NextPattern(out i);

        while(true){
            if( VectorBtoP().magnitude < boss1PatternList[i].startDistance ){
            //if(VectorBtoP().magnitude < 15f ){
                StartCoroutine(nextPattern);
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

    IEnumerator BackStep(){
        enemystate = EnemyState.BackStep;
        FootprintEnable(true);
        float t = Time.time;
        Vector3 v = VectorBtoP();
        while(t > Time.time - backStepTime){
            footprintDirection = v;
            transform.Translate(new Vector3(-v.x, -v.y, 0).normalized * backStepSpeed * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }
        StartCoroutine(Chase());
    }

    void StartPattern( string s){
        enemystate = EnemyState.SuperArmor;
        StartCoroutine(s);
    }

    GameObject MakePreAttackEffect( Attacktype type){
        if(type == Attacktype.white){
            return Instantiate(preattackEffectW, transform.position, Quaternion.identity);
        }
        else if(type == Attacktype.red){
            return Instantiate(preattackEffectR, transform.position, Quaternion.identity);
        }
        else if(type == Attacktype.blue){
            return Instantiate(preattackEffectB, transform.position, Quaternion.identity);
        }
        else return null;
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

    GameObject MakeDamagedSprite(){
        GameObject damagedSprite = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        damagedSprite.GetComponent<SpriteRenderer>().DOFade(0, 0.8f).SetEase(Ease.InQuint).OnComplete(() => {
            Destroy(damagedSprite);
        });
        return damagedSprite;
    }
       
    public void FootprintEnable(bool tf){
        footprintEnable = tf;
    }

    public Vector3 VectorBtoP(){
        return (player.transform.position - transform.position);
    }

    void DefenselessStart(){
        if(enemystate == EnemyState.QTEEnable){
            return;
        }
        defenselessTime = Time.time;
        enemystate = EnemyState.Defenseless;
    }

    IEnumerator DestoryAfterimage( GameObject afterimage, float duration){
        yield return new WaitForSeconds(duration);
        if(afterimage != null) Destroy(afterimage);
    }

    public void CeasePattern(bool playerDamaged = false){
        StopAllCoroutines();
        StartCoroutine(FootPrint());
        StartCoroutine(ParticleDestruct());
        //쳐맞음
        if(playerDamaged) {
            StartCoroutine(CeaseAndWalk(1.1f));
        }
        

    }

    IEnumerator CeaseAndWalk(float f){
        yield return new WaitForSeconds(f);
        StartCoroutine(Walk());
    }

    GameObject lastSpecialDamagedOne;
    public void Damaged(bool QTEsuccess = false, int damage = 10){
        if(enemystate == EnemyState.Defenseless){
            if(streak > 1){
                float r = Random.Range(0f, Mathf.Pow(2,streak-1));
                if (r < 1){
                    bossHP -= 10;
                    streak += 1;
                    // TODO 넉백도 추가할 것
                }
                else {
                    enemystate = EnemyState.Defensive;
                    Damaged();
                }
            }
        }
        else if(enemystate == EnemyState.QTEEnable){
            enemystate = EnemyState.Special;
            StartCoroutine(AfterimageDestruct());
            player.StartQTE();
        }
        else if(enemystate == EnemyState.Special){
            lastSpecialDamagedOne = MakeDamagedSprite();
            transform.DOKill(false);
            transform.DOMove(transform.position + -VectorBtoP().normalized * 1.2f, 0.2f);
            if(QTEsuccess) bossHP -= 10;
        }
        else if(enemystate == EnemyState.SuperArmor){
            bossHP -= 10;
        }
        else if(enemystate == EnemyState.Defensive){
            CeasePattern();
            transform.DOMove(VectorBtoP().normalized * -0.7f, 0.2f).SetRelative().SetDelay(0.2f).OnComplete(() => {
                int i = Random.Range(0,2);
                if(i == 0) StartCoroutine(Chase());
                else StartCoroutine(BackStep());
            });
            
            // 뭔가 효과 있으면 좋을 듯?
            

        }

        if(bossHP < bossHPMax/2) b1pe.Advanced();
    }
    public void Damaged(int damage){
        Damaged(false, damage);
    }

    IEnumerator ParticleDestruct(){
        yield return new WaitForFixedUpdate();
        GameManager.Instance.DestructAttackParticles.Invoke();
    }
    IEnumerator AfterimageDestruct(){
        yield return new WaitForFixedUpdate();
        GameManager.Instance.DestructAfterimage.Invoke();
        MakeDamagedSprite();
    }

    public void StaminaDamaged(int damage = 10, bool red = false){
        bossStamina -= damage;
        if(bossStamina <= 0){
            if( VectorBtoP().magnitude > 0.5f ){
                transform.position = player.transform.position - VectorBtoP().normalized * 1.5f;
            }
            enemystate = EnemyState.QTEEnable;
            CeasePattern();
            GameManager.Instance.CameraSetting.PreZoomIn();
            player.StartZoom();
        }
        else if(red){
            if( VectorBtoP().magnitude > 0.5f ){
                transform.position = player.transform.position - VectorBtoP().normalized * 1.5f;
            }
            GameManager.Instance.CameraSetting.PreZoomIn();
            player.StartZoom();
        }
    }

    public void EndQTE(){
        transform.DOKill(false);
        lastSpecialDamagedOne.transform.DOMove(transform.position + -VectorBtoP().normalized * 3.1f, 0.4f).SetEase(Ease.OutCubic);
        transform.DOMove(transform.position + -VectorBtoP().normalized * 3.1f, 0.4f).SetEase(Ease.OutCubic).OnComplete(() =>{
            StartCoroutine(Walk());
        });
        bossStamina = bossStaminaMax;
    }

    public void SkipQTE(){
        if(enemystate == EnemyState.QTEEnable){
            bossStamina += 30;
            StartCoroutine(Walk());
        }
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
    
    // 항상 max distance만큼 돌진하고 돌진경로에 플레이어가 있으면 startposition 방향에서 공격함
    void AttackPattern2(float maxdistance, Attacktype attacktype, int attackdamage){
        Vector3 startposition = transform.position;
        Vector3 v = VectorBtoP().normalized * maxdistance;
        transform.Translate(v);
        RaycastHit2D hit = Physics2D.Raycast(startposition, transform.position - startposition, (transform.position - startposition).magnitude, LayerMask.GetMask("Player"));
        if(hit.collider != null){
            player.Attacked(startposition, attackdamage, attacktype);
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

            MakePreAttackEffect(pattern.attacktype[i]);
            yield return new WaitForSeconds(pattern.stanbyTime[i]);
            
            AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
            MakeAttackSprite();
        }

        DefenselessStart();
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

        MakePreAttackEffect(pattern.attacktype[0]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[1]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        i = 1;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 2;
        MakePreAttackEffect(pattern.attacktype[2]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);

        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        DefenselessStart();
    }

    // 1 - 2 흰흰 - 흰흰 - 흰
    IEnumerator Pattern2(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[2];
        int i;
        
        i = 0;
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[i] + 0.2f));        
    
        MakePreAttackEffect(pattern.attacktype[0]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[1]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 1;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        i = 2;
        MakePreAttackEffect(pattern.attacktype[2]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[3]);
        
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 3;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 4;
        MakePreAttackEffect(pattern.attacktype[4]);                
        yield return new WaitForSeconds(pattern.stanbyTime[i]);

        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        DefenselessStart();
    }

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

            MakePreAttackEffect(pattern.attacktype[i]);
            yield return new WaitForSeconds(pattern.stanbyTime[i]);
            
            AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
            MakeAttackSprite();
        }

        DefenselessStart();
    }

    // 4 흰 - 옆으로 돌아서 흰
    IEnumerator Pattern4(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[4];
        int i;
        
        i = 0;
        GameObject image0 = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = image0.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image0, pattern.stanbyTime[i]));

        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        
        AttackPattern2(pattern.maxDashDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 1;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        int direction = Random.Range(0,2) * 2 - 1;
        Vector3 moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * direction).normalized * pattern.attackDistance[i];
        if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
            moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * - direction).normalized * pattern.attackDistance[i];
            if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
                DefenselessStart();
                yield break;
            }
        }
        transform.position = moveposition;
        
        i = 2;
        GameObject image1 = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = image1.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image1, pattern.stanbyTime[i]));
        
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);

        AttackPattern2(pattern.maxDashDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        DefenselessStart();
    }

    // 5 돌진 - (빨강 공격) (멀리서 공격)
    IEnumerator Pattern5(){
        FootprintEnable(false);
        int i;
        Pattern pattern = boss1PatternList[5];
        
        i = 0;
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[0]));
        MakePreAttackEffect(pattern.attacktype[i]);

        yield return new WaitForSeconds(pattern.stanbyTime[0]);
        
        AttackPattern2(pattern.maxDashDistance[0], pattern.attacktype[0], pattern.attackDamage[0]);
        MakeAttackSprite();
        DefenselessStart();
    }
    
    // 6 흰 - 흰 (멀리서 공격) (멀리서 흰색 -> 발자국으로 가까워짐 -> attack)
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
        MakePreAttackEffect(pattern.attacktype[i]);
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
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        DefenselessStart();
    }
 
    // 7 흰흰흰 (빠르게)
    IEnumerator Pattern7(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[7];
        int i;
        
        i = 0;
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[0] + 0.4f));        
        
        isDamageStopPattern = pattern.damageStopPattern[0];

        MakePreAttackEffect(pattern.attacktype[0]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[1]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[2]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        i = 1;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 2;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        DefenselessStart();
    }
    
    // 8 백스텝 파랑 공격 (멀리 + 가까이서 패턴) 
    IEnumerator Pattern8(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[8];
        
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        GameObject effect = MakePreAttackEffect(pattern.attacktype[0]);

        Vector3 v = transform.position - VectorBtoP().normalized * pattern.maxDashDistance[0];
        image.transform.DOMove(v, pattern.stanbyTime[0]).SetEase(Ease.OutSine).OnComplete(() => {
            Destroy(image);
        });
        effect.transform.DOMove(v, pattern.stanbyTime[0]).SetEase(Ease.OutSine);
        transform.DOMove(v, pattern.stanbyTime[0]).SetEase(Ease.OutSine);
        yield return new WaitForSeconds(pattern.stanbyTime[0] * 3/4);
        
        player.AttackedBlue(transform.position, player.transform.position, pattern.attackDamage[0], 0.3f);
        DefenselessStart();
    }
    
    // 9 흰흰-흰흰-빨 (2 강화) 
    IEnumerator Pattern9(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[9];
        int i;
        
        i = 0;
        GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = afterimage.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[i] + 0.2f));        
    
        MakePreAttackEffect(pattern.attacktype[0]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[1]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 1;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        i = 2;
        MakePreAttackEffect(pattern.attacktype[2]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[3]);
        
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 3;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        AttackPattern1(pattern.maxDashDistance[i], pattern.properDashDistance[i], pattern.attackDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        i = 4;
        MakePreAttackEffect(pattern.attacktype[4]);                
        yield return new WaitForSeconds(pattern.stanbyTime[i]);

        AttackPattern2(pattern.maxDashDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();

        DefenselessStart();
    }

    // 세부조정
    // 10 흰 - (흰?파) - (흰?파) - (흰?파) (4 강화) 
    IEnumerator Pattern10(){
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[10];
        int i;
        
        i = 0;
        GameObject image0 = Instantiate(afterimage, transform.position, Quaternion.identity);
        // SpriteRenderer aiSR = image0.GetComponent<SpriteRenderer>();
        // aiSR.sprite = pattern.preAttackSprites[0]
        StartCoroutine(DestoryAfterimage(image0, pattern.stanbyTime[i]));

        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
        AttackPattern2(pattern.maxDashDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
        MakeAttackSprite();
        
        for (int j = 0 ; j < 3; j++){
            i = 1;
            int direction = Random.Range(0,2) * 2 - 1;
            Vector3 moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * direction).normalized * pattern.attackDistance[i];
            if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
                moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * - direction).normalized * pattern.attackDistance[i];
                if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
                    DefenselessStart();
                    yield break;
                }
            }
            transform.position = moveposition;
        
            yield return new WaitForSeconds(pattern.stanbyTime[i]);
        
            int wb = Random.Range(0,2);
            if (wb == 0 ) {
                //w
                i = 2;    
                GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
                // SpriteRenderer aiSR = image1.GetComponent<SpriteRenderer>();
                // aiSR.sprite = pattern.preAttackSprites[0]
                StartCoroutine(DestoryAfterimage(image, pattern.stanbyTime[i]));
        
                MakePreAttackEffect(pattern.attacktype[i]);
                yield return new WaitForSeconds(pattern.stanbyTime[i]);
                
                AttackPattern2(pattern.maxDashDistance[i], pattern.attacktype[i], pattern.attackDamage[i]);
                MakeAttackSprite();
            }
            else{
                //b
                i = 3;
                GameObject image = Instantiate(afterimage, transform.position, Quaternion.identity);
                // SpriteRenderer aiSR = image1.GetComponent<SpriteRenderer>();
                // aiSR.sprite = pattern.preAttackSprites[0]
                GameObject effect = MakePreAttackEffect(pattern.attacktype[i]);
                Vector3 v = transform.position - VectorBtoP().normalized * pattern.maxDashDistance[i];
                image.transform.DOMove(v, pattern.stanbyTime[i] - 0.05f).SetEase(Ease.OutSine).OnComplete(() => {
                    Destroy(image);
                });
                effect.transform.DOMove(v, pattern.stanbyTime[i] - 0.05f).SetEase(Ease.OutSine);
                transform.DOMove(v, pattern.stanbyTime[i] - 0.05f).SetEase(Ease.OutSine);
                player.AttackedBlue(transform.position, player.transform.position, pattern.attackDamage[i], 0.7f);
                
                yield return new WaitForSeconds(pattern.stanbyTime[i]);
                
            }
        }
        DefenselessStart();
    }
    public int GetBossHP(){
        return bossHP;
    }

    public int GetBossStamina(){
        return bossStamina;
    }
    public int GetBossMaxStamina(){
        return bossStaminaMax;
    }

    public void EnemyColor(){
        if(enemystate == EnemyState.Defenseless){
            sr.color = Color.yellow;
        }
        else if (enemystate == EnemyState.Special){
            sr.color = Color.magenta;
        }
        else {
            sr.color = Color.white;
        }
    }
}
