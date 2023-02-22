using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Attacktype = Pattern.AttackType;
using UnityEngine.SceneManagement;

public class PlayerBehaviour : MonoBehaviour
{
    
    enum PlayerState
    {
        Idle,
        Attack,
        Damaged,
        Roll,
        Running,
        Guard,
        QTE,
        OnlyAttack,
        Dead
    }

    //건드릴 것
    [SerializeField] float playerMoveSpeed; //Player가 움직이는 속도
    [SerializeField] float playerRollSpeed; //구르기 속도
    [SerializeField] float playerRollTime; //구르기 시간 (거리)
    [SerializeField] float playerRollCooltime; //구르기 쿨타임
    [SerializeField] float staminaRecoveryTime; //스태미나가 회복되기위해 대기하는 시간
    [SerializeField] float staminaRecoverySpeed; //스태미나 회복 속도
    [SerializeField] float playerRunningSpeed; //Player 달리기 속도
    [SerializeField] float playerRunningStaminaSpendingSpeed; //Player 달리기 스태미나 소비 속도
    [SerializeField] float playerStaminaMax = 4.0f; //Player 스태미나 Max
    [SerializeField] float playerHealth; //Player 체력
    [SerializeField] float playerMaxHealth; //Player Max 체력
    [SerializeField] float playerRecoveryHealth; //Player 회복할 수 있는 체력 
    [SerializeField] float recoverySpeed; //Player 체력 회복 속도
    [SerializeField] float recoveryTime; //Player 체력 회복 기다릴 시간
    [SerializeField] float knuckbackSmall = 0.2f; // 넉백 거리 (작음, 가드, 패리 성공)
    [SerializeField] float knuckbackBig = 1.0f; // 넉백 거리 (큼, 데미지 당함)
    [SerializeField] float attackDashDistance; // 공격 대시 사거리
    [SerializeField] Vector2 border; // 나갈 수 없는 테두리
    [SerializeField] float QTEDashDistance; //QTE 대시 거리
    [SerializeField] float QTEDashTime; //QTE 대시 시간
    [SerializeField] float zoomMaxTime; //zoom max 시간
    [SerializeField] float parryCoolTime; // 패리 정상화 시간

    [SerializeField] GameObject attackRange;
    
    [SerializeField] Boss1Behaviour boss;

    Collider2D playerCollider;
    Collider2D attackCollider;
    
    [SerializeField] GameObject parryEffect; // parry 했을 때 이펙트
    [SerializeField] GameObject blueObject; // blue parry 했을 때 object
    [SerializeField] GameObject chargeAttackComplete; // 차지가 완료될 때 이펙트
    [SerializeField] GameObject chargeAttackEffect; // 차지가 시작할 때 이펙트
    
    //blue object 관련 값들
    [SerializeField] float boFloatDistanceY;
    [SerializeField] float boFloatDistanceX;
    [SerializeField] float boTime;
    [SerializeField] float boTime2;

    //건드리면 안됨
    float lastRollTime; //Player가 마지막으로 구르기를 한 시간
    float lastStaminaSpendTime; //Player가 마지막으로 구르거나 달린 시간 (스태미나 회복용)
    float lastDamagedTime; //Player가 마지막으로 맞은 시간
    float playerStamina; //Player 스태미나
    [SerializeField] PlayerState playerstate; //PlayerState
    Vector2 moveDirection; // 움직이는 방향
    Vector2 rollDirection; // 롤 방향 평소엔 영벡터
    bool parryable; //패리 가능 상태
    float lastParryTime; // 마지막으로 패리가 된 상태
    List<Attack> attackList;
    float guardGauge;
    float attackGauge;

    Camera mainCamera;
    SpriteRenderer sr;
    PlayerQTE pQTE; //QTE 관련
    Vector2 mouseVec; //mouse 벡터
    bool zoomed; //줌인 되어 있으면 true
    float zoomedTime; //줌인 된 시간
    Vector3 QTEStartpoint; // QTE 보조용 벡터
    Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        pQTE = GetComponent<PlayerQTE>();
        animator = GetComponent<Animator>();
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        playerstate = PlayerState.Idle;
        guardGauge = 4f;
        lastRollTime = Time.time;
        lastParryTime = Time.time;
        playerHealth = playerMaxHealth;
        playerRecoveryHealth = playerMaxHealth;
        lastStaminaSpendTime = Time.time;
        playerStamina = playerStaminaMax;
        attackCollider = attackRange.GetComponent<Collider2D>();
        attackList = new List<Attack>();

    }


    // Update is called once per frame
    void Update()
    {
        //움직임, 공격, 방어 등등
        PlayerControl();

        //패링 회복 관련
        if (!parryable)
        {
            if (lastParryTime + parryCoolTime < Time.time) ParryNormalize();
        }

        //스태미나 관련
        if (playerStamina < playerStaminaMax)
        {
            if (lastStaminaSpendTime + staminaRecoveryTime < Time.time){
                playerStamina += staminaRecoverySpeed * Time.deltaTime;
            }
        }
        //border밖으로 못나가게
        float x = Mathf.Clamp(transform.position.x, -border.x, border.x);
        float y = Mathf.Clamp(transform.position.y, -border.y, border.y);
        transform.position = new Vector3(x,y,0);

        //공격 판정
        AttackProcess();
        
        //줌 끝날때
        if(zoomed && zoomedTime + zoomMaxTime < Time.time) {
            EndZoom();
            playerstate = PlayerState.Idle;
            animator.SetTrigger("Idle");
        }

        mouseVec = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        float angle = Vector2.SignedAngle(Vector2.right, mouseVec);
        attackRange.transform.rotation = Quaternion.Euler(0,0,angle);

        //PlayerColor();
    }


    bool chargeComplete = false;

    void PlayerControl()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
            EndZoom();
            
        }
        else if (Input.GetMouseButton(0)){
            if(playerstate == PlayerState.Attack){
                attackGauge += Time.deltaTime / 1.2f;
                if(attackGauge > guardGauge) attackGauge = guardGauge;
                if(attackGauge > 1f && !chargeComplete) {
                    chargeComplete = true;
                    Instantiate(chargeAttackComplete, this.transform);
                }
            }
        }
        else if(Input.GetMouseButtonUp(0)){
            GameManager.Instance.DestructChargeParticles.Invoke();
            if(guardGauge > 1f && attackGauge > 1f) {
                guardGauge -= 1;
                AttackDash();
            } 
            else if(playerstate == PlayerState.Attack) {
                playerstate = PlayerState.Idle;
                animator.SetTrigger("Idle");
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            if (playerstate == PlayerState.Idle)
            {
                animator.SetTrigger("Guard");
                playerstate = PlayerState.Guard;
                if(parryable){
                    lastParryTime = Time.time;
                    parryable = false;
                    Parrying();
                    Debug.Log("Parrying block : " + Time.time);
                }
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            if (playerstate == PlayerState.Guard) {
                playerstate = PlayerState.Idle;
                animator.SetTrigger("Idle");
            }
        }

        //attackGauge 초기화
        if(playerstate != PlayerState.Attack) {
            attackGauge = 0;
            chargeComplete = false;
        }

        //HP 회복
        if(lastDamagedTime + recoveryTime < Time.time && playerstate != PlayerState.Dead){
            playerHealth += recoverySpeed * Time.deltaTime;
            if(playerHealth > playerRecoveryHealth) playerHealth = playerRecoveryHealth;
        }


        moveDirection = Vector2.zero;
        if (Input.GetKey(KeyCode.D)) moveDirection.x = 1;
        else if (Input.GetKey(KeyCode.A)) moveDirection.x = -1;
        if (Input.GetKey(KeyCode.W)) moveDirection.y = 1;
        else if (Input.GetKey(KeyCode.S)) moveDirection.y = -1;

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space))
        {
            if (playerstate == PlayerState.Idle && (moveDirection.x != 0 || moveDirection.y != 0) && lastRollTime + playerRollCooltime < Time.time && playerStamina > 0.95f)
            {
                StartCoroutine(Rolling(new Vector2(moveDirection.x,moveDirection.y)));
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.Space))
        {
            if (playerstate == PlayerState.Running) playerstate = PlayerState.Idle;
        }

        if (playerstate == PlayerState.Idle)
        {
            if(moveDirection.x != 0 || moveDirection.y != 0) animator.SetBool("Move", true);
            else animator.SetBool("Move", false);
            transform.Translate(V2toV3(moveDirection).normalized * playerMoveSpeed * Time.deltaTime);
            
        }
        else if(playerstate == PlayerState.Running){
            animator.SetBool("Move", true);
            transform.Translate(V2toV3(moveDirection).normalized * playerRunningSpeed * Time.deltaTime);
            lastStaminaSpendTime = Time.time;
            playerStamina -= playerRunningStaminaSpendingSpeed * Time.deltaTime;
            if(playerStamina < 0 || (moveDirection.x == 0 && moveDirection.y == 0)) playerstate = PlayerState.Idle;
        }
    }

    public void Attack(){
        if(playerstate == PlayerState.OnlyAttack && Vector2.Angle(boss.transform.position - transform.position, mouseVec) < 75){
            animator.SetTrigger("Attack");
            boss.Damaged(25);
            if(playerstate != PlayerState.QTE){
                transform.DOKill(false);
                transform.DOMove(transform.position + V2toV3(mouseVec).normalized * 0.5f, 0.2f).OnComplete(() => {
                    animator.SetTrigger("Idle");
                });
            }
        }
        else if(attackList.Count > 0 && attackList[0].attacktype == Attacktype.blue && Vector2.Angle(attackList[0].attackDirection, -mouseVec) < 75){
            animator.SetTrigger("BlueGuard");
            attackList.RemoveAt(0);
            boss.NextPattern(false);
            GainGuardGauge(0.37f);
            Instantiate(parryEffect, transform.position + V2toV3(mouseVec).normalized * 0.4f, Quaternion.identity);
            InstantiateBlueObject2(transform.position + V2toV3(mouseVec).normalized * 0.4f, -mouseVec);
        }
        else if(AttackAvailiableState()) {
            animator.SetTrigger("AttackReady");
            playerstate = PlayerState.Attack;
            if(guardGauge > 1f) Instantiate(chargeAttackEffect, this.transform);
        }
    }

    //공격할 때 나오는 대시
    public void AttackDash(){
        animator.SetTrigger("Attack");
        ContactFilter2D cf2d = new ContactFilter2D();
        cf2d.SetLayerMask(LayerMask.GetMask("Boss"));
        List<Collider2D> res = new List<Collider2D>(); 
        if(attackCollider.OverlapCollider(cf2d, res) > 0) {
            boss.Damaged( 25 );
            attackList = new List<Attack>();
        }
        transform.DOKill(false);
        transform.DOMove(transform.position + V2toV3(mouseVec).normalized * attackDashDistance, 0.4f).OnComplete(() => {
            if(playerstate == PlayerState.Attack) {
                playerstate = PlayerState.Idle;
                animator.SetTrigger("Idle");
            }
        });
    }
    
    IEnumerator Rolling(Vector2 v)
    {
        animator.SetTrigger("Roll");
        rollDirection = v;
        lastRollTime = Time.time;
        lastStaminaSpendTime = Time.time;
        playerstate = PlayerState.Roll;
        playerStamina -= 1.0f;
        for(int i = 0 ; i < playerRollTime / Time.deltaTime / 2; i++){
            transform.Translate(V2toV3(v).normalized * playerRollSpeed * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }

        if(attackList.Count > 0 && attackList[0].attacktype == Attacktype.red){
            Attack a = attackList[0];
            if(Vector2.Angle(-attackList[0].attackDirection, v) < 75) {
                animator.SetTrigger("SpecialAttack");
                attackList.RemoveAt(0);
                boss.NextPattern(false);
                boss.StaminaDamaged(30, true);
                transform.DOMove(V2toV3(rollDirection).normalized, 0.8f).SetRelative(true);
                rollDirection = Vector2.zero;
                yield break;
            }
        }

        for(int i = 0 ; i < playerRollTime / Time.deltaTime / 2; i++){
            transform.Translate(V2toV3(v).normalized * playerRollSpeed * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }

        if(playerstate == PlayerState.Roll){
            animator.SetTrigger("Idle");
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Space)) playerstate = PlayerState.Running;
            else {
                playerstate = PlayerState.Idle;
                
            }
        }
        rollDirection = Vector2.zero;

    }

    public void Parrying(){
        if(attackList.Count > 0 && attackList[0].attacktype == Attacktype.white){
            Attack a = attackList[0];
            if(Vector2.Angle(-attackList[0].attackDirection, mouseVec) < 75) {
                InstantiateParryEffect(transform.position - attackList[0].attackDirection.normalized * 0.5f);
                ParryNormalize();
                GuardKnuckback(attackList[0].attackDirection.normalized);
                attackList.RemoveAt(0);
                boss.NextPattern(false);
                boss.StaminaDamaged();
                GainGuardGauge(0.23f);
            }
        }
    }

    public void ParryNormalize()
    {
        parryable = true;
    }

    public bool AttackAvailiableState()
    {
        return playerstate == PlayerState.Idle || playerstate == PlayerState.Running || 
            playerstate == PlayerState.Guard;
    }

    public bool DamageAvailiableState(){
        return playerstate == PlayerState.Idle || playerstate == PlayerState.Running || 
            playerstate == PlayerState.Attack;
    }

    public GameObject InstantiateParryEffect(Vector3 v){
        return Instantiate(parryEffect, v, Quaternion.identity);
    }

    //쿠나이 돌리기
    public void InstantiateBlueObject2(Vector3 pos, Vector3 bAttackDirection){
        GameObject bo = Instantiate(blueObject, pos, Quaternion.identity);
        SpriteRenderer sr = bo.GetComponent<SpriteRenderer>();
        int direction;
        if(bAttackDirection.x > 0) direction = 1;
        else direction = -1;
        float vectorX = Random.Range(boFloatDistanceX / 2, boFloatDistanceX);
        Sequence sequence = DOTween.Sequence()
        .Append(bo.transform.DOMoveX(direction * vectorX, boTime + boTime2).SetRelative().SetEase(Ease.Linear))
        .Join(bo.transform.DOMoveY(boFloatDistanceY, boTime).SetRelative().SetEase(Ease.OutCubic))
        .Insert(boTime, bo.transform.DOMoveY(-3f * boFloatDistanceY, boTime2).SetRelative().SetEase(Ease.InSine))
        .Join(sr.DOFade(0,boTime2).SetEase(Ease.OutSine))
        .Insert(0, bo.transform.DORotate(new Vector3(0,0,360), 0.8f, RotateMode.FastBeyond360).SetLoops(15, LoopType.Restart).SetEase(Ease.Linear))
        .OnComplete(() => {
            Destroy(bo);
        });
        sequence.Play();
    }

    public void PreQTE(){
        playerstate = PlayerState.OnlyAttack;
    }

    int lastmove;
    public void StartQTE(int num = -1){
        zoomed = false;
        playerstate = PlayerState.QTE;
        int attacknum = num;
        if(attacknum < 0 ) attacknum = Random.Range(7,9);
        
        transform.DOKill(false);
        boss.transform.position = Vector3.zero + (boss.transform.position - transform.position).normalized * 2.0f;
        transform.position = Vector3.zero;
        transform.DOMove((boss.transform.position - transform.position).normalized * 4.0f, QTEDashTime).SetEase(Ease.OutQuint).SetRelative();
        lastmove = 2;

        pQTE.StartQTE(attacknum);
    }

    public void EndQTE(){
        StartCoroutine(EndQTE_());    
        boss.EndQTE();
    }

    IEnumerator EndQTE_(){
        yield return new WaitForSeconds(0.4f);
        playerstate = PlayerState.Idle;
        animator.SetTrigger("Idle");
    }

    public void QTEDash(bool firstcall, bool isSuccess){
        int i;
        do {
            i = Random.Range(0,3);
        } while (lastmove == i) ;
        
        if(i == 0) animator.SetTrigger("SP1");
        else if(i == 1) animator.SetTrigger("SP2");
        else if(i == 2) animator.SetTrigger("SP3");
        lastmove = i;
        
        if(firstcall) QTEStartpoint = transform.position;
        Vector3 d = (boss.transform.position - QTEStartpoint).normalized * QTEDashDistance;
        float delta = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
        Vector3 rotated = new Vector2(d.x *  Mathf.Cos(delta) - d.y * Mathf.Sin(delta), d.x * Mathf.Sin(delta) + d.y * Mathf.Cos(delta));
        
        transform.DOKill(false);
        QTEStartpoint = boss.transform.position + rotated;
        transform.DOMove(QTEStartpoint, QTEDashTime).SetEase(Ease.OutQuint);
        if(!isSuccess) {
            float r = Random.Range(0f, 360f * Mathf.Deg2Rad);
            Instantiate(parryEffect, new Vector3(boss.transform.position.x + 0.8f * Mathf.Cos(r), boss.transform.position.y + 0.8f * Mathf.Sin(r), 0), Quaternion.identity);
        }

        boss.Damaged( isSuccess );
    }

    public void AfterParry(){
        ParryNormalize();
    }

    public void StartZoom(){
        playerstate = PlayerState.OnlyAttack;
        zoomed = true;
        zoomedTime = Time.time;
        attackList = new List<Attack>();
    }

    public bool Zoomed(){
        return zoomed;
    }

    public void EndZoom(){
        if(zoomed && playerstate != PlayerState.QTE) {
            GameManager.Instance.CameraSetting.SmoothEndZoom();
            zoomed = false;
            boss.SkipQTE();
            playerstate = PlayerState.Idle;
        }
    }

    public void AttackListAdd( Vector3 attackCenter, Vector3 targetPosition, int damage, float duration, Attacktype attacktype){
        attackList.Add(new Attack(attackCenter, targetPosition, damage, Time.time + duration, attacktype));
        attackList.Sort((a,b) => a.time.CompareTo(b.time));
    }
    
    public void AttackListAdd( Vector3 attackCenter, Vector3 direction, float distance, int damage, float duration, Attacktype attacktype){
        attackList.Add(new Attack(attackCenter, direction, distance, damage, Time.time + duration, attacktype));
        attackList.Sort((a,b) => a.time.CompareTo(b.time));
    }

    public void AttackedBlue( Vector3 attackCenter, Vector3 targetPosition, int damage, float duration){
        AttackListAdd(attackCenter, targetPosition, damage, duration, Attacktype.blue);
    }

    public void PlayerDead(){
        StopAllCoroutines();
        transform.DOKill(false);
        playerstate = PlayerState.Dead;
        //애니메이션 재생
    }

    public void AttackProcess(){
        if(attackList.Count != 0){
            if(attackList[0].time < Time.time){
                Attack a = attackList[0];
                Debug.Log(a.time); 
                if(a.attacktype == Attacktype.white){
                    float angle = Vector2.Angle(mouseVec, -a.attackDirection);
                    if((a.startPoint - transform.position).magnitude < a.attackDistance){
                        if (playerstate == PlayerState.Guard && angle < 75) {
                            DamagedSmall(a.damage / 3, a.attackDirection.normalized);
                            boss.NextPattern(false);
                            GainGuardGauge(0.11f);
                        }
                        else if( DamageAvailiableState() ||  playerstate == PlayerState.Guard){
                            DamagedBig(a.damage, a.attackDirection.normalized);
                            boss.NextPattern(true);
                        }
                        else boss.NextPattern(false);
                    }
                    else boss.NextPattern(false);
                }
                else if(a.attacktype == Attacktype.red){
                    if((DamageAvailiableState() || playerstate == PlayerState.Guard) && (a.startPoint - transform.position).magnitude < a.attackDistance ) {
                        DamagedBig(a.damage, a.attackDirection.normalized);
                        boss.NextPattern(true);
                    }
                    else boss.NextPattern(false);
                }   
                else if(a.attacktype == Attacktype.blue){
                    if(DamageAvailiableState() || playerstate == PlayerState.Guard) {
                    RaycastHit2D hit = Physics2D.Raycast(a.startPoint, a.attackDirection, 100, LayerMask.GetMask("Player"));
                        if(hit.collider != null){
                            GameObject bo = Instantiate(blueObject, boss.transform.position, Quaternion.Euler(0,0,Vector2.SignedAngle(Vector2.down, a.attackDirection)));
                            bo.transform.DOMove(a.attackDirection.normalized * 50f, 15f).SetRelative().SetSpeedBased().OnComplete(() => {
                                Destroy(bo);
                            });
                            DamagedBig(a.damage, a.attackDirection);
                            boss.NextPattern(true); 
                        }
                        else{
                            GameObject bo = Instantiate(blueObject, boss.transform.position, Quaternion.Euler(0,0,Vector2.SignedAngle(Vector2.down, a.attackDirection)));
                            bo.transform.DOMove(a.attackDirection.normalized * 50f, 15f).SetRelative().SetSpeedBased().OnComplete(() => {
                                Destroy(bo);
                            });
                            boss.NextPattern(false);
                        }
                    }
                    else{
                        GameObject bo = Instantiate(blueObject, boss.transform.position, Quaternion.Euler(0,0,Vector2.SignedAngle(Vector2.down, a.attackDirection)));
                        bo.transform.DOMove(a.attackDirection.normalized * 50f, 15f).SetRelative().SetSpeedBased().OnComplete(() => {
                            Destroy(bo);
                        });
                        boss.NextPattern(false);
                    }
                }
                attackList.RemoveAt(0);
            }
        }
        
    }

    // 클린 히트했을때
    public void DamagedBig(int damage, Vector3 vec){
        animator.SetTrigger("Hit");
        GameManager.Instance.DestructChargeParticles.Invoke();
        playerstate = PlayerState.Damaged;
        playerHealth -= damage;
        if(playerHealth <= 0 ){
            PlayerDead();
            return;
        }
        playerRecoveryHealth -= damage/3f;
        lastDamagedTime = Time.time;
        transform.DOKill(false);
        transform.DOMove(transform.position + vec.normalized * knuckbackBig, 1.2f).SetEase(Ease.OutCubic).OnComplete(() => {
            playerstate = PlayerState.Idle;
            animator.SetTrigger("Idle");
        });
    }

    //guard 했을때의 데미지 + 넉백 
    public void DamagedSmall(int damage, Vector3 vec){
        playerHealth -= damage;
        if(playerHealth <= 0 ){
            PlayerDead();
            return;
        }
        lastDamagedTime = Time.time;
        GuardKnuckback(vec);
    }

    //가드또는 패링했을때의 넉백
    public void GuardKnuckback(Vector3 vec, float knuckback = -1f){
        float f = knuckback;
        if (f < 0) {
            f = knuckbackSmall;
        }
        transform.DOKill(false);
        transform.DOMove(transform.position + vec * f, 0.1f).SetEase(Ease.OutCubic);
    }
    public void GainGuardGauge(float f){
        guardGauge += f;
        if(guardGauge > 4) guardGauge = 4;
    }

    public float GetPlayerHP(){
        return playerHealth;
    }
    public float GetPlayerMaxHP(){
        return playerMaxHealth;
    }
    public float GetPlayerStamina(){
        return playerStamina;
    }
    public float GetGuardGauge(){
        return guardGauge;
    }
    public float GetAttackGauge(){
        return attackGauge;
    }
    public float GetMaxHealth(){
        return playerMaxHealth;
    }
    public float GetRecoveryHealth(){
        return playerRecoveryHealth;
    }
    
    //For Debug
    public void PlayerColor(){
        if(playerstate == PlayerState.Damaged){
            sr.color = Color.red;
        }
        else if(playerstate == PlayerState.Guard){
            sr.color = Color.gray;
        }
        else if(playerstate == PlayerState.Roll){
            sr.color = Color.blue;
        }
        else if(playerstate == PlayerState.Attack){
            sr.color = Color.yellow;
        }
        else if(playerstate == PlayerState.QTE){
            sr.color = Color.magenta;
        }
        else{
            sr.color = Color.white;
        }

    }
    
    public Vector3 V2toV3(Vector2 v){
        Vector3 v3 = v;
        return v3;
    }
}
