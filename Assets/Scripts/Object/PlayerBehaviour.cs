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
        OnlyAttack
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
    [SerializeField] float parryDurationMax = 30 / 60f; //Player 패링 시간 Max
    [SerializeField] float parryDurationMin = 7 / 60f; //Player 패링 시간 Min
    [SerializeField] int playerHealth = 100; //Player 체력
    [SerializeField] float knuckbackSmall = 0.2f; // 넉백 거리 (작음, 가드, 패리 성공)
    [SerializeField] float knuckbackBig = 1.0f; // 넉백 거리 (큼, 데미지 당함)
    [SerializeField] float attackDashDistance; // 공격 대시 사거리
    [SerializeField] Vector2 border; // 나갈 수 없는 테두리
    [SerializeField] float QTEDashDistance; //QTE 대시 거리
    [SerializeField] float QTEDashTime; //QTE 대시 시간
    [SerializeField] float zoomMaxTime; //zoom max 시간

    [SerializeField] GameObject attackRange;
    
    [SerializeField] Boss1Behaviour boss;

    Collider2D playerCollider;
    Collider2D attackCollider;
    
    [SerializeField] GameObject parryEffect; // parry 했을 때 이펙트
    [SerializeField] GameObject blueObject; // blue parry 했을 때 object
    
    //blue object 관련 값들
    [SerializeField] float boFloatDistanceY;
    [SerializeField] float boFloatDistanceX;
    [SerializeField] float boTime;
    [SerializeField] float boTime2;

    //건드리면 안됨
    float lastGuardTime; //Player가 마지막으로 가드를 끝낸 시간
    float lastRollTime; //Player가 마지막으로 구르기를 한 시간
    float lastStaminaSpendTime; //Player가 마지막으로 구르거나 달린 시간 (스태미나 회복용)
    float parryDuration; //Player 현재 패링 시간
    float guardTime; //Player가 가드하고 있는 시간
    float playerStamina; //Player 스태미나
    [SerializeField] PlayerState playerstate; //PlayerState
    Vector2 moveDirection; // 움직이는 방향
    Vector2 rollDirection; // 롤 방향 평소엔 영벡터
    bool parryable; //패리 가능 상태
    float lastParryTime; // 마지막으로 패리가 된 상태
    [SerializeField] List<Attack> attackList;

    Camera mainCamera;
    SpriteRenderer sr;
    PlayerQTE pQTE; //QTE 관련
    Vector2 mouseVec; //mouse 벡터
    bool zoomed; //줌인 되어 있으면 true
    float zoomedTime; //줌인 된 시간
    Vector3 QTEStartpoint; // QTE 보조용 벡터


    

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        pQTE = GetComponent<PlayerQTE>();
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        playerstate = PlayerState.Idle;
        guardTime = 0;
        lastRollTime = Time.time;
        lastParryTime = Time.time;

        lastStaminaSpendTime = Time.time;
        playerStamina = playerStaminaMax;
        attackCollider = attackRange.GetComponent<Collider2D>();
        attackList = new List<Attack>();
    }


    [SerializeField] float parryableTime;
    // Update is called once per frame
    void Update()
    {
        PlayerControl();
        
        //패링 회복 관련
        if (!parryable)
        {
            if (lastParryTime + parryableTime < Time.time) ParryNormalize();
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
        
        if(zoomed && zoomedTime + zoomMaxTime < Time.time) {
            EndZoom();
            playerstate = PlayerState.Idle;
        }

        mouseVec = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        float angle = Vector2.SignedAngle(Vector2.right, mouseVec);
        attackRange.transform.rotation = Quaternion.Euler(0,0,angle);

        PlayerColor();
    }

    void PlayerControl()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (AttackAvailiableState()) {
                Attack();
                EndZoom();
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            if (playerstate == PlayerState.Idle)
            {
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
            if (playerstate == PlayerState.Guard)
            {
                playerstate = PlayerState.Idle;
            }
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
            transform.Translate(V2toV3(moveDirection).normalized * playerMoveSpeed * Time.deltaTime);
        }
        else if(playerstate == PlayerState.Running){
            transform.Translate(V2toV3(moveDirection).normalized * playerRunningSpeed * Time.deltaTime);
            lastStaminaSpendTime = Time.time;
            playerStamina -= playerRunningStaminaSpendingSpeed * Time.deltaTime;
            if(playerStamina < 0 || (moveDirection.x == 0 && moveDirection.y == 0)) playerstate = PlayerState.Idle;
        }


    }

    IEnumerator Rolling(Vector2 v)
    {
        rollDirection = v;
        lastRollTime = Time.time;
        lastStaminaSpendTime = Time.time;
        playerstate = PlayerState.Roll;
        playerStamina -= 1.0f;
        for(int i = 0 ; i < playerRollTime / Time.deltaTime; i++){
            transform.Translate(V2toV3(v).normalized * playerRollSpeed * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }
        if(playerstate == PlayerState.Roll){
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Space)) playerstate = PlayerState.Running;
            else playerstate = PlayerState.Idle;
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
                boss.StaminaDamaged();
                boss.NextPattern(false);
                attackList.RemoveAt(0);
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
            playerstate == PlayerState.Guard || playerstate == PlayerState.OnlyAttack;
    }

    public bool DamageAvailiableState(){
        return playerstate == PlayerState.Idle || playerstate == PlayerState.Running || 
            playerstate == PlayerState.Attack;
    }

    //공격 + 파랑 공격 패링 판정
    public void Attack(){
        if(attackList.Count > 0 && attackList[0].attacktype == Attacktype.blue && Vector2.Angle(attackList[0].attackDirection, -mouseVec) < 75){
            attackList.RemoveAt(0);
            // TODO : parry 됐을때의 적절한 처리

            Instantiate(parryEffect, transform.position + V2toV3(mouseVec).normalized * 0.4f, Quaternion.identity);
            InstantiateBlueObject2(transform.position + V2toV3(mouseVec).normalized * 0.4f, -mouseVec);
        }
        else{
            playerstate = PlayerState.Attack;
            ContactFilter2D cf2d = new ContactFilter2D();
            cf2d.SetLayerMask(LayerMask.GetMask("Boss"));
            List<Collider2D> res = new List<Collider2D>(); 
            if(attackCollider.OverlapCollider(cf2d, res) > 0) {
                boss.Damaged();
            }
            transform.DOKill(false);
            transform.DOMove(transform.position + V2toV3(mouseVec).normalized * attackDashDistance, 0.4f).OnComplete(() => {
                if(playerstate == PlayerState.Attack) playerstate = PlayerState.Idle;
            });
        }
    }

    public GameObject InstantiateParryEffect(Vector3 v){
        return Instantiate(parryEffect, v, Quaternion.identity);
    }

    //쿠나이 돌리기
    public void InstantiateBlueObject2(Vector3 pos, Vector3 bAttackDirection){
        GameObject bo = Instantiate(blueObject, pos, Quaternion.identity);
        Transform mask = bo.transform.GetChild(0);
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

    public void StartQTE(int num = -1){
        zoomed = false;
        playerstate = PlayerState.QTE;
        int attacknum = num;
        if(attacknum < 0 ) attacknum = Random.Range(6,9);
        
        boss.transform.position = Vector3.zero + (boss.transform.position - transform.position).normalized * 2.0f;
        transform.position = Vector3.zero;
        pQTE.StartQTE(attacknum);
    }

    public void EndQTE(){
        StartCoroutine(EndQTE_());    
        boss.EndQTE();
    }

    IEnumerator EndQTE_(){
        yield return new WaitForSeconds(0.4f);
        playerstate = PlayerState.Idle;
    }

    public void QTEDash(bool firstcall, bool isSuccess){
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
        zoomed = true;
        zoomedTime = Time.time;
    }

    public void EndZoom(){
        if(zoomed && playerstate != PlayerState.QTE) {
            GameManager.Instance.CameraSetting.SmoothEndZoom();
            zoomed = false;
            boss.SkipQTE();
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

    public void AttackProcess(){
        if(attackList.Count != 0){
            if(attackList[0].time < Time.time){
                Attack a = attackList[0];
                if(a.attacktype == Attacktype.white){
                    float angle = Vector2.Angle(mouseVec, -a.attackDirection);
                    if((a.startPoint - transform.position).magnitude < a.attackDistance){
                        if (playerstate == PlayerState.Guard && angle < 75) {
                            DamagedSmall(a.damage / 2, a.attackDirection.normalized);
                            
                            boss.NextPattern(false);
                        }
                        else if( DamageAvailiableState() ||  playerstate == PlayerState.Guard){
                            DamagedBig(a.damage, a.attackDirection.normalized);
                            boss.NextPattern(true);
                        }
                        else {
                            boss.NextPattern(false);
                        }
                    }
                    else{
                        boss.NextPattern(false);
                    }
                }
                else if(a.attacktype == Attacktype.red){
                    if(DamageAvailiableState() ||  playerstate == PlayerState.Guard) {
                    RaycastHit2D hit = Physics2D.Raycast(a.startPoint, a.attackDirection, LayerMask.GetMask("Player"));
                        if(hit.collider != null){
                            DamagedBig(a.damage, -a.attackDirection);
                            boss.NextPattern(true); //보스 쪽에 패턴 멈추라고 신호를 줌
                            // 맞았다는 신호 + 애니메이션 재생 
                            // 이건 중간에 하게 만들어야 되나? 일단 만들자
                        }
                        else{
                            boss.NextPattern(false);
                        }
                    }
                    else{
                        boss.NextPattern(false);
                    }
                }   
                else if(a.attacktype == Attacktype.blue){
                    if(DamageAvailiableState() || playerstate == PlayerState.Guard) {
                    RaycastHit2D hit = Physics2D.Raycast(a.startPoint, a.attackDirection, LayerMask.GetMask("Player"));
                        if(hit.collider != null){
                            DamagedBig(a.damage, a.attackDirection);
                            boss.NextPattern(true); //보스 쪽에 패턴 멈추라고 신호를 줌
                            // 맞았다는 신호 + 애니메이션 재생 
                            // 아마 List에 있는 blue 전부 지워버리고 -> boss 쪽 새 패턴 추출하는 식으로 할 듯?
                        }
                        else{
                            boss.NextPattern(false);
                        }
                    }
                    else{
                        boss.NextPattern(false);
                    }
                }
                attackList.RemoveAt(0);
            }
        }
    }

    // 클린 히트했을때
    public void DamagedBig(int damage, Vector3 vec){
        guardTime = 0;
        playerstate = PlayerState.Damaged;
        playerHealth -= damage;
        if(playerHealth <= 0 ){
            // TODO 게임오버 처리
            return;
        }
        transform.DOKill(false);
        transform.DOMove(transform.position + vec.normalized * knuckbackBig, 1.2f).SetEase(Ease.OutCubic).OnComplete(() => {
            playerstate = PlayerState.Idle;
        });
    }

    //guard 했을때의 데미지 + 넉백 
    public void DamagedSmall(int damage, Vector3 vec){
        playerHealth -= damage;
        GuardKnuckback(vec);
    }

    //가드또는 패링했을때의 넉백, attack 했을 때의 넉백도 있음
    public void GuardKnuckback(Vector3 vec, float knuckback = -1f){
        float f = knuckback;
        if (f < 0) {
            f = knuckbackSmall;
        }
        transform.DOKill(false);
        transform.DOMove(transform.position + vec * f, 0.1f).SetEase(Ease.OutCubic).OnComplete(() => {
            if(playerstate == PlayerState.Attack) playerstate = PlayerState.Idle;
        });
    }

    public int GetPlayerHP(){
        return playerHealth;
    }

    public float GetPlayerStamina(){
        return playerStamina;
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
