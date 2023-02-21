using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Attacktype = Pattern.AttackType;
using UnityEngine.Events;

public class Boss1Behaviour : MonoBehaviour
{
    enum EnemyState{
        Idle,
        Special,
        QTEEnable,
        BackStep
    }
    
    boss1PatternExtracter b1pe;
    [SerializeField] EnemyState enemystate;
    
    [SerializeField] List<Pattern> boss1PatternList; // 패턴 리스트
    [SerializeField] Vector2 border; //Border Vector
    [SerializeField] PlayerBehaviour player; //Player
    [SerializeField] BanishingObject footPrintObject; //Boss 발자국
    [SerializeField] SpriteRenderer afterimage; // 잔상
    [SerializeField] GameObject preattackEffectW; // 공격전 이펙트 W
    [SerializeField] GameObject preattackEffectR; // 공격전 이펙트 R
    [SerializeField] GameObject preattackEffectB; // 공격전 이펙트 B
    [SerializeField] float walkSpeed; //Boss가 움직이는 속도 (Walk)
    [SerializeField] float walkToPatternTime; //Boss가 Walk -> Pattern 에 들어가는 시간
    [SerializeField] float walkMaxTime; //Boss가 Walk -> Chase에 들어가는 시간
    [SerializeField] float chaseSpeed; //Boss가 움직이는 속도 (Chase)
    [SerializeField] float footprintCoolTime; //Footprint 간격시간
    [SerializeField] int bossStaminaMax; // boss 스태미나 max
    [SerializeField] float backStepSpeed; // backstep 속도
    [SerializeField] float backStepTime; // backstep 속도
    [SerializeField] int bossHPMax; // backstep 속도
    [SerializeField] GameObject hitEffect; // HitEffect
    [SerializeField] Sprite damageSprite;
    [SerializeField] Sprite guardSprite;

    bool footprintEnable; //Boss Footprint 출력 여부
    Vector2 footprintDirection; //Footprint 방향
    int bossHP; // Boss HP
    [SerializeField] int bossStamina; // boss의 스태미나 (QTE)
    float lastDamaged;
    SpriteRenderer sr; //spriterenderer
    float footprintCoolTimeOriginal;
    GameObject lastSpecialDamagedOne;

    [SerializeField] bool nextPattern; //다음 패턴을 사용
    [SerializeField] bool playerDamaged; // 플레이어가 데미지를 입음

    //for debug
    bool debugColor;

    private void Awake() {
        b1pe = GetComponent<boss1PatternExtracter>();
        b1pe.StartPatternList(this);
        b1pe.patternList.Add(new PatternLink("Pattern0", 0));
        b1pe.patternList.Add(new PatternLink("Pattern1", 1));
        b1pe.patternList.Add(new PatternLink("Pattern2", 2));
        b1pe.patternList.Add(new PatternLink("Pattern3", 3));
        b1pe.patternList.Add(new PatternLink("Pattern4", 4, 1f));
        b1pe.patternList.Add(new PatternLink("Pattern5", 5, true));
        b1pe.patternList.Add(new PatternLink("Pattern7", 7));
        b1pe.patternList.Add(new PatternLink("Pattern8", 8, false, true));
        b1pe.patternList[2].advanced = (new PatternLink("Pattern9", 9, false, true));
        b1pe.patternList[4].advanced = (new PatternLink("Pattern10", 10, false, true));
        //b1pe.patternList.Add(new PatternLink("Pattern0"));

        b1pe.PreparePatterns();
    }    
    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(FootPrint());
        footprintEnable = true;
        debugColor = true;
        bossHP = bossHPMax;
        bossStamina = bossStaminaMax/2;
        lastDamaged = Time.time;
        footprintCoolTimeOriginal = footprintCoolTime;
        StartCoroutine(Walk());
        nextPattern = false;
        playerDamaged = false;
        alreadyWaiting = false;
        CoroutineAgain = false;
    }

    // Update is called once per frame
    void Update()
    {
        float x = Mathf.Clamp(transform.position.x, -border.x, border.x);
        float y = Mathf.Clamp(transform.position.y, -border.y, border.y);
        transform.position = new Vector3(x,y,0);


        /*        
        if(Input.GetKeyDown(KeyCode.X)){
            debugColor = !debugColor;
            Color tmp = sr.color;
            tmp.a = 1 - tmp.a;
            sr.color = tmp;
        }
        if(debugColor){
            EnemyColor();
        }
*/       
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
        PatternDamaged = false;
        FootprintEnable(true);
        enemystate = EnemyState.Idle;
        
        float walkStartTime = Time.time;

        int direction;
        Vector3 v1 = Vector3.Cross(transform.position - player.transform.position, Vector3.forward * 1);
        Vector3 v2 = Vector3.Cross(transform.position - player.transform.position, Vector3.forward * -1);
        float angle1 = Vector2.Angle(transform.position, v1);
        float angle2 = Vector2.Angle(transform.position, v2);
        if(angle1 > angle2) direction = 1;
        else direction = -1;

        while(true){
            if(PatternDamaged){
                PatternDamaged = false;
                yield break;
            }
            if(Time.time - walkStartTime > walkMaxTime) {
                StartCoroutine(Chase());
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

    IEnumerator Chase(){
        PatternDamaged = false;
        FootprintEnable(true);
        enemystate = EnemyState.Idle;
        int i;
        string nextPattern = b1pe.NextPattern(out i);
        while(true){
            if(PatternDamaged){
                PatternDamaged = false;
                yield break;
            }
            if( VectorBtoP().magnitude < boss1PatternList[i].startDistance ){
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
    [SerializeField] Sprite backStepSprite;
/*
    IEnumerator BackStep(){
        yield return new WaitForFixedUpdate();
        GameManager.Instance.DestructAfterimage.Invoke();
        PatternDamaged = false;
        enemystate = EnemyState.BackStep;
        FootprintEnable(false);
        GameObject sprite = MakePreAttackSprite(backStepSprite);
        sprite.GetComponent<SpriteRenderer>().DOFade(0, backStepTime + 0.1f).OnComplete(() =>{
            Destroy(sprite);
        });
        float t = Time.time;
        Vector3 v = -VectorBtoP();
        while(t > Time.time - backStepTime){
            if(PatternDamaged) {
                PatternDamaged = false;
                yield break;
            }
            footprintDirection = v;
            transform.Translate(new Vector3(v.x, v.y, 0).normalized * backStepSpeed * Time.deltaTime);
            sprite.transform.position = transform.position;
            yield return new WaitForFixedUpdate();
        }
        
        StartCoroutine(Chase());
    }
*/
    public void BackStep(){
        PatternDamaged = false;
        enemystate = EnemyState.BackStep;
        FootprintEnable(false);
        GameObject sprite = MakePreAttackSprite(backStepSprite);
        sprite.GetComponent<SpriteRenderer>().DOFade(0, backStepTime + 0.1f).OnComplete(() =>{
            Destroy(sprite);
        });
        float t = Time.time;
        Vector3 v = -VectorBtoP();
        footprintDirection = v;
        sprite.transform.DOMove(transform.position + new Vector3(v.x, v.y, 0).normalized * backStepSpeed, backStepTime).SetEase(Ease.OutCubic);
        transform.DOMove(transform.position + new Vector3(v.x, v.y, 0).normalized * backStepSpeed, backStepTime).SetEase(Ease.OutCubic).OnComplete(() =>{
            StartCoroutine(Walk());
        });
    }
       
    public void FootprintEnable(bool tf){
        footprintEnable = tf;
    }

    public Vector3 VectorBtoP(){
        return (player.transform.position - transform.position);
    }

    IEnumerator DestoryAfterimage( GameObject afterimage, float duration){
        yield return new WaitForSeconds(duration);
        if(afterimage != null) Destroy(afterimage);
    }

    bool alreadyWaiting;
    bool CoroutineAgain;
    IEnumerator CeaseAndWalk(float f){
        if(alreadyWaiting) CoroutineAgain = true;//이미 돌아가는 중

        alreadyWaiting = true;
        yield return new WaitForSeconds(f);
        if(CoroutineAgain) {
            CoroutineAgain = false;
            yield break;
        }
        footprintCoolTime = footprintCoolTimeOriginal;
        StartCoroutine(Walk());
        alreadyWaiting = false;
    }

    IEnumerator CeaseAndChase(float f){
        if(alreadyWaiting) CoroutineAgain = true;//이미 돌아가는 중

        alreadyWaiting = true;
        yield return new WaitForSeconds(f);
        if(CoroutineAgain) {
            CoroutineAgain = false;
            yield break;
        }
        footprintCoolTime = footprintCoolTimeOriginal;
        alreadyWaiting = false;
        StartCoroutine(Chase());
    }

    public void Damaged(int damage){
        Damaged(false, damage);
    }

    //아마 싹 고쳐야 될듯?

    [SerializeField] float attackKnuckbackDistance;
    //0.7
    [SerializeField] float attackKnuckbackTime;
    //0.2

    public void Damaged(bool QTEsuccess = false, int damage = 15){
        if(enemystate == EnemyState.Idle){
            bossHP -= damage;
            GameObject md = MakeDamagedSprite(false);
            transform.DOMove(VectorBtoP().normalized * -attackKnuckbackDistance, attackKnuckbackTime).SetRelative();
            md.transform.DOMove(VectorBtoP().normalized * -attackKnuckbackDistance, attackKnuckbackTime).SetRelative();
            PatternDamaged = true;
            StartCoroutine(CeaseAndChase(0.2f));
        }
        else if(enemystate == EnemyState.QTEEnable){
            enemystate = EnemyState.Special;
            StartCoroutine(AfterimageDestruct());
            player.StartQTE();
        }
        else if(enemystate == EnemyState.Special){
            lastSpecialDamagedOne = MakeDamagedSprite(!QTEsuccess);
            transform.DOKill(false);
            transform.DOMove(transform.position + -VectorBtoP().normalized * 1.2f, 0.2f);
            if(QTEsuccess) {
                bossHP -= damage;
                GameObject he = Instantiate(hitEffect, lastSpecialDamagedOne.transform);
                if(VectorBtoP().x > 0){
                    Vector3 temp = he.transform.localScale;
                    temp.x = -temp.x;
                    he.transform.localScale = temp;
                    temp = he.transform.localPosition;
                    temp.x = -temp.x;
                    he.transform.localPosition = temp;
                }
            }
        }

        if(bossHP < bossHPMax/2) b1pe.Advanced();
    }

    IEnumerator ParticleDestruct(){
        yield return new WaitForFixedUpdate();
        GameManager.Instance.DestructAttackParticles.Invoke();
    }
    IEnumerator AfterimageDestruct(){
        yield return new WaitForFixedUpdate();
        GameManager.Instance.DestructAfterimage.Invoke();
        MakeDamagedSprite(false);
    }

    public void StaminaDamaged(int damage = 10, bool red = false){
        bossStamina -= damage;
        if(bossStamina <= 0){
            if(VectorBtoP().magnitude > 0.5f ){
                transform.position = player.transform.position - VectorBtoP().normalized * 1.5f;
            }
            enemystate = EnemyState.QTEEnable;
            StartCoroutine(ParticleDestruct());
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

    GameObject MakePreAttackSprite(Sprite sprite, float f = -1){
        SpriteRenderer image = Instantiate(afterimage, InsideFenceTransform(transform.position), Quaternion.identity);
        image.sprite = sprite;
        if(VectorBtoP().x < 0) image.flipX = true;
        if(f > 0) StartCoroutine(DestoryAfterimage(image.gameObject, f));
        return image.gameObject;
    }

    void MakeAttackSprite(Sprite sprite, bool inverseFlip = false){
        SpriteRenderer attackSprite = Instantiate(afterimage, InsideFenceTransform(transform.position), Quaternion.identity);
        attackSprite.sprite = sprite;
        if( (VectorBtoP().x < 0 && !inverseFlip) || (VectorBtoP().x > 0 && inverseFlip) ) attackSprite.flipX = true;
        attackSprite.GetComponent<SpriteRenderer>().DOFade(0, 1.7f).SetEase(Ease.InQuint).OnComplete(() => {
            Destroy(attackSprite.gameObject);
        });
    }

    GameObject MakeDamagedSprite(bool isGuard){
        SpriteRenderer dSprite = Instantiate(afterimage, InsideFenceTransform(transform.position), Quaternion.identity);
        if(isGuard) dSprite.sprite = guardSprite;
        else dSprite.sprite = damageSprite;
        if(VectorBtoP().x < 0) dSprite.flipX = true;
        dSprite.GetComponent<SpriteRenderer>().DOFade(0, 0.8f).SetEase(Ease.InQuint).OnComplete(() => {
            Destroy(dSprite.gameObject);
        });
        return dSprite.gameObject;
    }

    Vector3 InsideFenceTransform(Vector3 v){
        float x = Mathf.Clamp(v.x, -border.x, border.x);
        float y = Mathf.Clamp(v.y, -border.y, border.y);
        return new Vector3(x,y,0);
    }

    // boss가 player에게 최대 max distance만큼 돌진 후 attackdistance만큼 attacktype으로 공격, 
    // proper distance만큼 돌진하려고 함, proper distance보다 가까우면 현재 자리에서 시전
    // 이제 이동만 함
    void AttackPattern1(float maxdistance, float properdistance){
        Vector3 v = VectorBtoP().normalized;
        Vector3 maxV = v * maxdistance;
        Vector3 properV = VectorBtoP() - v * properdistance;
        if(Vector3.Dot(properV, v) < 0) properV = Vector3.zero; // 너무 가까우면 제자리에서 패턴 진행
        Vector3 pos = (maxV.magnitude < properV.magnitude) ? maxV : properV;
        transform.Translate(pos);
    }
    
    // 항상 max distance만큼 돌진하고 돌진경로에 플레이어가 있으면 startposition 방향에서 공격함
    // 이제 이동만 함
    void AttackPattern2(float maxdistance, float properdistance){
        Vector3 startposition = transform.position;
        Vector3 v = VectorBtoP().normalized * maxdistance;
        transform.Translate(v);
        
        if(player.Zoomed() && VectorBtoP().magnitude > 0.5f ){
            transform.position = player.transform.position - VectorBtoP().normalized * 1.5f;
        }

    }
    
    //다음 패턴으로 넘어감 + 현재 자리에 공격 스프라이트 만듦
    bool DoNextPattern(System.Action<float, float> Function, Pattern pattern, int pattern_num, bool inverse = false){
        if(nextPattern){
            nextPattern = false;
            Function(pattern.maxDashDistance[pattern_num], pattern.properDashDistance[pattern_num]);
            MakeAttackSprite(pattern.attackSprites[pattern_num], inverse);
            return true;
        }
        return false;
    }

    bool IsPlayerDamaged(Pattern pattern, int pattern_num){
        if(playerDamaged){
            playerDamaged = false;
            if(pattern.damageStopPattern[pattern_num]) {
                StartCoroutine(CeaseAndWalk(1.5f));
                return true;
            }
        }
        return false;
    }

    public void NextPattern(bool isPlayerDamaged){
        nextPattern = true;
        playerDamaged = isPlayerDamaged;
    }

    [SerializeField] bool PatternDamaged = true; // 패턴이 시작하면 false, 패턴 중 맞으면 true가 되고 패턴을 종료시킴, 후속으로 걷는 패턴 (walk, shortwalk, chase)에서도 true로 만듦

    // 0 흰-흰-흰 정박
    IEnumerator Pattern0(){
        PatternDamaged = false;
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[0];
        int i;
        int maxAttack = 3;

        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[i]);
        for (i = 0 ; i < maxAttack ; i++){
            MakePreAttackEffect(pattern.attacktype[i]);
            yield return new WaitForSeconds(0.25f);
            if(PatternDamaged) {
                Destroy(preattack);
                yield break;
            }
            player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
            while(true){
                if(PatternDamaged) {
                    if(i == 0) Destroy(preattack);
                    yield break;
                }
                if(DoNextPattern(AttackPattern1, pattern, i)){
                    if(i == 0) Destroy(preattack);
                    if(IsPlayerDamaged(pattern, i)) yield break;
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
            if(enemystate == EnemyState.QTEEnable) yield break;
        }

        StartCoroutine(CeaseAndWalk(1.2f));
    }

    // 1 흰흰 - 흰
    IEnumerator Pattern1(){
        PatternDamaged = false;
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[1];
        int i;
        
        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[0]);
        
        MakePreAttackEffect(pattern.attacktype[0]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[1]);
        yield return new WaitForSeconds(0.25f);
        if(PatternDamaged) {
            Destroy(preattack);
            yield break;
        }
        for(i = 0; i < 2 ; i++){
            player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[0], pattern.attackDamage[0], pattern.stanbyTime[0], pattern.attacktype[0]);
            while(true){
                if(PatternDamaged) {
                    if(i == 0) Destroy(preattack);
                    yield break;
                }
                if(DoNextPattern(AttackPattern1, pattern, i)){
                    if(i == 0) Destroy(preattack);
                    if(IsPlayerDamaged(pattern, i)) yield break;
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
            if(enemystate == EnemyState.QTEEnable) yield break;    
        }
        
        i = 2;
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.25f);
        
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(DoNextPattern(AttackPattern1, pattern, i)){
                if(i == 0) Destroy(preattack);
                if(IsPlayerDamaged(pattern, i)) {
                    yield break;
                }
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;

        yield return new WaitForSeconds(0.8f);
        BackStep();
    }


    // 1 - 2 흰흰 - 흰흰 - 흰
    IEnumerator Pattern2(){
        PatternDamaged = false;
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[2];
        int i;
        
        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[0]);
        
        MakePreAttackEffect(pattern.attacktype[0]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[1]);
        yield return new WaitForSeconds(0.25f);
        
        if(PatternDamaged) {
            Destroy(preattack);
            yield break;
        }        
        for(i = 0; i < 2 ; i++){
            player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
            while(true){
                if(PatternDamaged) {
                    if(i == 0) Destroy(preattack);
                    yield break;
                }
                if(DoNextPattern(AttackPattern1, pattern, i)){
                    if(i == 0) Destroy(preattack);
                    if(IsPlayerDamaged(pattern, i)) yield break;
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
            if(enemystate == EnemyState.QTEEnable) yield break;    
        }

        MakePreAttackEffect(pattern.attacktype[2]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[3]);
        yield return new WaitForSeconds(0.25f);

        for(i = 2; i < 4 ; i++){
            player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
            while(true){
                if(PatternDamaged) {
                    if(i == 0) Destroy(preattack);
                    yield break;
                }
                if(DoNextPattern(AttackPattern1, pattern, i)){
                    if(i == 0) Destroy(preattack);
                    if(IsPlayerDamaged(pattern, i)) yield break;
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
            if(enemystate == EnemyState.QTEEnable) yield break;    
        }

        i = 4;
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.25f);
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(DoNextPattern(AttackPattern1, pattern, i)){
                if(i == 0) Destroy(preattack);
                if(IsPlayerDamaged(pattern, i)) {
                    yield break;
                }
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;

        StartCoroutine(CeaseAndWalk(1.2f));
    }

    // 3 흰 - 흰(엇박)(빠른 엇박)
    IEnumerator Pattern3(){
        PatternDamaged = false;
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[3];
        int i;
        
        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[i]);
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.25f);
        if(PatternDamaged) {
            Destroy(preattack);
            yield break;
        }        
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(PatternDamaged) {
                if(i == 0) Destroy(preattack);
                yield break;
            }
            if(DoNextPattern(AttackPattern1, pattern, i)){
                if(i == 0) Destroy(preattack);
                if(IsPlayerDamaged(pattern, i)) yield break;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;
    
        i = 1;
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.1f);
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(PatternDamaged) {
                if(i == 0) Destroy(preattack);
                yield break;
            }
            if(DoNextPattern(AttackPattern1, pattern, i)){
                if(i == 0) Destroy(preattack);
                if(IsPlayerDamaged(pattern, i)) yield break;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;        
        
        StartCoroutine(CeaseAndChase(0.3f));
    }

    // 4 흰 돌진 - 옆으로 돌아서 흰 돌진
    IEnumerator Pattern4(){
        PatternDamaged = false;
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[4];
        int i;
        
        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[i]);
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.25f);
        if(PatternDamaged) {
            Destroy(preattack);
            yield break;
        }
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(PatternDamaged) {
                if(i == 0) Destroy(preattack);
                yield break;
            }
            if(DoNextPattern(AttackPattern2, pattern, i, true)){
                if(i == 0) Destroy(preattack);
                if(IsPlayerDamaged(pattern, i)) yield break;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;

        i = 1;
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        int direction = Random.Range(0,2) * 2 - 1;
        Vector3 moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * direction).normalized * pattern.attackDistance[i];
        if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
            moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * - direction).normalized * pattern.attackDistance[i];
            if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
                StartCoroutine(CeaseAndWalk(1.5f));
                yield break;
            }
        }
        transform.position = moveposition;


        i = 2;
        GameObject preattack1 = MakePreAttackSprite(pattern.preAttackSprites[0]);
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.25f);
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(PatternDamaged) {
                if(i == 2) Destroy(preattack1);
                yield break;
            }
            if(DoNextPattern(AttackPattern2, pattern, i, true)){
                if(i == 2) Destroy(preattack1);
                if(IsPlayerDamaged(pattern, i)) yield break;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;

        StartCoroutine(CeaseAndWalk(1.2f));
    }

    // 5 돌진 - (빨강 공격) (멀리서 공격)
    IEnumerator Pattern5(){
        PatternDamaged = false;
        FootprintEnable(false);
        int i;
        Pattern pattern = boss1PatternList[5];
        
        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[i]);
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.25f);
        if(PatternDamaged) {
            Destroy(preattack);
            yield break;
        }
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(PatternDamaged) {
                if(i == 0) Destroy(preattack);
                yield break;
            }
            if(DoNextPattern(AttackPattern2, pattern, i, true)){
                if(i == 0) Destroy(preattack);
                if(IsPlayerDamaged(pattern, i)) yield break;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;

        StartCoroutine(CeaseAndWalk(1.2f));
    }
    
    // 6 흰 - 흰 (멀리서 공격) (멀리서 흰색 -> 발자국으로 가까워짐 -> attack) 폐기!
    IEnumerator Pattern6(){
        PatternDamaged = false;
        Pattern pattern = boss1PatternList[6];
        int i;
        i = 0;
        footprintCoolTime = 0.15f;
        
        MakePreAttackSprite(pattern.preAttackSprites[0], pattern.stanbyTime[0]);
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(pattern.stanbyTime[i]);
        while(VectorBtoP().magnitude > pattern.properDashDistance[i]) {
            Vector3 v = VectorBtoP();
            footprintDirection = v;
            transform.Translate(new Vector3(v.x, v.y, 0).normalized * 9.8f * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }
        FootprintEnable(false);
        footprintCoolTime = footprintCoolTimeOriginal;
        
        i = 0;
        if(PatternDamaged) {
            yield break;
        }
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(PatternDamaged) {
                yield break;
            }
            if(DoNextPattern(AttackPattern1, pattern, i)){
                if(IsPlayerDamaged(pattern, i)) yield break;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;
        
        i = 1;
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.25f);
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(PatternDamaged) {
                yield break;
            }
            if(DoNextPattern(AttackPattern1, pattern, i)){
                if(IsPlayerDamaged(pattern, i)) yield break;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;

        yield return new WaitForSeconds(0.8f);
        BackStep();
    }

    // 7 흰흰흰 (빠르게)
    IEnumerator Pattern7(){
        PatternDamaged = false;
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[7];
        int i;
        
        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[0]);
        
        MakePreAttackEffect(pattern.attacktype[0]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[1]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[2]);
        yield return new WaitForSeconds(0.1f);

        if(PatternDamaged) {
            Destroy(preattack);
            yield break;
        }
        for(i = 0; i < 3 ; i++){
            player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
            while(true){
                if(PatternDamaged) {
                    if(i == 0) Destroy(preattack);
                    yield break;
                }
                if(DoNextPattern(AttackPattern1, pattern, i)){
                    if(i == 0) Destroy(preattack);
                    if(IsPlayerDamaged(pattern, i)) yield break;
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
            if(enemystate == EnemyState.QTEEnable) yield break;    
        }

        StartCoroutine(CeaseAndWalk(1.2f));
    }
    
    // 8 백스텝 파랑 공격 (멀리 + 가까이서 패턴) 
    IEnumerator Pattern8(){
        PatternDamaged = false;
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[8];
        int i;

        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[0]);
        GameObject effect = MakePreAttackEffect(pattern.attacktype[0]);
        
        Vector3 v = transform.position - VectorBtoP().normalized * pattern.maxDashDistance[0];
        preattack.transform.DOMove(v, pattern.stanbyTime[0]).SetEase(Ease.OutSine).OnComplete(() => {
            Destroy(preattack);
        });
        effect.transform.DOMove(v, pattern.stanbyTime[0]).SetEase(Ease.OutSine);
        transform.DOMove(v, pattern.stanbyTime[0]).SetEase(Ease.OutSine);
        
        if(PatternDamaged) {
            Destroy(preattack);
            yield break;
        }
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);     
        while(true){
            if(PatternDamaged) {
                yield break;
            }
            if(nextPattern){
                nextPattern = false;
                if(playerDamaged){
                    playerDamaged = false;
                    if(pattern.damageStopPattern[i]){
                        StartCoroutine(CeaseAndWalk(1.5f));
                        yield break;
                    } 
                }
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break; 

        StartCoroutine(CeaseAndWalk(1.2f));
    }

    // 9 흰흰-흰흰-빨 (2 강화) 
    IEnumerator Pattern9(){
        PatternDamaged = false;
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[9];
        int i;
        
        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[0]);
        
        MakePreAttackEffect(pattern.attacktype[0]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[1]);
        yield return new WaitForSeconds(0.25f);
        if(PatternDamaged) {
            Destroy(preattack);
            yield break;
        }
        for(i = 0; i < 2 ; i++){
            player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
            while(true){
                if(PatternDamaged) {
                    if(i == 0) Destroy(preattack);
                    yield break;
                }
                if(DoNextPattern(AttackPattern1, pattern, i)){
                    if(i == 0) Destroy(preattack);
                    if(IsPlayerDamaged(pattern, i)) yield break;
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
            if(enemystate == EnemyState.QTEEnable) yield break;    
        }

        MakePreAttackEffect(pattern.attacktype[2]);
        yield return new WaitForSeconds(0.2f);
        MakePreAttackEffect(pattern.attacktype[3]);
        yield return new WaitForSeconds(0.25f);

        if(PatternDamaged) yield break;
        for(i = 2; i < 4 ; i++){
            player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
            while(true){
                if(PatternDamaged) {
                    if(i == 0) Destroy(preattack);
                    yield break;
                }
                if(DoNextPattern(AttackPattern1, pattern, i)){
                    if(i == 0) Destroy(preattack);
                    if(IsPlayerDamaged(pattern, i)) yield break;
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
            if(enemystate == EnemyState.QTEEnable) yield break;    
        }

        i = 4;
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.25f);
        if(PatternDamaged) yield break;
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(DoNextPattern(AttackPattern2, pattern, i)){
                if(i == 0) Destroy(preattack);
                if(IsPlayerDamaged(pattern, i)) {
                    yield break;
                }
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;

        StartCoroutine(CeaseAndWalk(1.2f));
    }

    // 세부조정
    // 10 흰 - (흰?파) - (흰?파) - (흰?파) (4 강화) 
    IEnumerator Pattern10(){
        PatternDamaged = false;
        FootprintEnable(false);
        Pattern pattern = boss1PatternList[10];
        int i;
        
        i = 0;
        GameObject preattack = MakePreAttackSprite(pattern.preAttackSprites[0]);
        MakePreAttackEffect(pattern.attacktype[i]);
        yield return new WaitForSeconds(0.25f);
        if(PatternDamaged) {
            Destroy(preattack);
            yield break;
        }
        player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
        while(true){
            if(PatternDamaged) {
                if(i == 0) Destroy(preattack);
                yield break;
            }
            if(DoNextPattern(AttackPattern2, pattern, i, true)){
                if(i == 0) Destroy(preattack);
                if(IsPlayerDamaged(pattern, i)) yield break;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if(enemystate == EnemyState.QTEEnable) yield break;

        for(int j = 0 ; j < 3; j++){
            i = 1;
            yield return new WaitForSeconds(pattern.stanbyTime[i]);
            transform.DOKill(false);
            int direction = Random.Range(0,2) * 2 - 1;
            Vector3 moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * direction).normalized * pattern.attackDistance[i];
            if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
                moveposition = player.transform.position + Vector3.Cross(VectorBtoP(), Vector3.forward * - direction).normalized * pattern.attackDistance[i];
                if(moveposition.x > border.x || moveposition.x < -border.x || moveposition.y > border.y || moveposition.y < -border.y){
                    StartCoroutine(CeaseAndWalk(1.5f));
                    yield break;
                }
            }
            transform.position = moveposition;

            int wb = Random.Range(0,2);
            if(wb == 0){
                i = 2;
                GameObject preattack1 = MakePreAttackSprite(pattern.preAttackSprites[0]);
                MakePreAttackEffect(pattern.attacktype[i]);
                yield return new WaitForSeconds(0.25f);
                player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
                while(true){
                    if(PatternDamaged) {
                        if(i == 2) Destroy(preattack1);
                        yield break;
                    }
                    if(DoNextPattern(AttackPattern2, pattern, i, true)){
                        if(i == 2) Destroy(preattack1);
                        if(IsPlayerDamaged(pattern, i)) yield break;
                        break;
                    }
                    yield return new WaitForFixedUpdate();
                }
                if(enemystate == EnemyState.QTEEnable) yield break; 
            }
            else{
                i = 3;
                GameObject preattack1 = MakePreAttackSprite(pattern.preAttackSprites[1]);
                GameObject effect = MakePreAttackEffect(pattern.attacktype[i]);
                
                Vector3 v = transform.position - VectorBtoP().normalized * pattern.maxDashDistance[i];
                preattack1.transform.DOMove(v, pattern.stanbyTime[i]).SetEase(Ease.OutSine).OnComplete(() => {
                    Destroy(preattack1);
                });
                effect.transform.DOMove(v, pattern.stanbyTime[i]).SetEase(Ease.OutSine);
                transform.DOMove(v, pattern.stanbyTime[i]).SetEase(Ease.OutSine);
                
                player.AttackListAdd(transform.position, VectorBtoP().normalized, pattern.attackDistance[i], pattern.attackDamage[i], pattern.stanbyTime[i], pattern.attacktype[i]);
                while(true){
                    if(PatternDamaged) {
                        yield break;
                    }
                    if(nextPattern){
                        nextPattern = false;
                        if(playerDamaged){
                            playerDamaged = false;
                            if(pattern.damageStopPattern[i]){
                                StartCoroutine(CeaseAndWalk(1.5f));
                                yield break;
                            } 
                        }
                        break;
                    }
                    yield return new WaitForFixedUpdate();
                }
                if(enemystate == EnemyState.QTEEnable) yield break; 
            }
        }
        StartCoroutine(CeaseAndWalk(1.2f));
    }

    
    public int GetBossHP(){
        return bossHP;
    }
    public int GetBossHPMax(){
        return bossHPMax;
    }
    public int GetBossStamina(){
        return bossStamina;
    }
    public int GetBossMaxStamina(){
        return bossStaminaMax;
    }

    public void EnemyColor(){
        if (enemystate == EnemyState.Special){
            sr.color = Color.magenta;
        }
        else {
            sr.color = Color.white;
        }
    }
}
