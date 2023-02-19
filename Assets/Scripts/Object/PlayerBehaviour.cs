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
        Parrying,
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
    List<BlueAttack> blueList; // blue attack 처리
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
        parryDuration = parryDurationMax;
        guardTime = 0;
        lastGuardTime = Time.time;
        lastRollTime = Time.time;
        lastStaminaSpendTime = Time.time;
        playerStamina = playerStaminaMax;
        attackCollider = attackRange.GetComponent<Collider2D>();
        blueList = new List<BlueAttack>();
    }

    // Update is called once per frame
    void Update()
    {
        PlayerControl();
        if (parryDuration < parryDurationMax)
        {
            if (lastGuardTime + 30 / 60f < Time.time) ParryNormalize();
        }
        if (playerStamina < playerStaminaMax)
        {
            if (lastStaminaSpendTime + staminaRecoveryTime < Time.time){
                playerStamina += staminaRecoverySpeed * Time.deltaTime;
            }
        }
        
        float x = Mathf.Clamp(transform.position.x, -border.x, border.x);
        float y = Mathf.Clamp(transform.position.y, -border.y, border.y);
        transform.position = new Vector3(x,y,0);

        BlueAttackProcess();
        
        PlayerColor();

        if(zoomed && zoomedTime + zoomMaxTime < Time.time) {
            EndZoom();
            playerstate = PlayerState.Idle;
        }

        if(Input.GetKeyDown(KeyCode.F1)){
            SceneManager.LoadScene("MainMenu");
        }
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
                playerstate = PlayerState.Parrying;
            }
        }
        else if (Input.GetMouseButton(1))
        {
            if (playerstate == PlayerState.Parrying) guardTime += Time.deltaTime;
            if (playerstate == PlayerState.Parrying && guardTime > parryDuration) playerstate = PlayerState.Guard;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            if (playerstate == PlayerState.Parrying || playerstate == PlayerState.Guard)
            {
                playerstate = PlayerState.Idle;
                guardTime = 0;
                lastGuardTime = Time.time;
                parryDuration = Mathf.Max(parryDuration - 5 / 60f, parryDurationMin);
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

        mouseVec = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        float angle = Vector2.SignedAngle(Vector2.right, mouseVec);
        attackRange.transform.rotation = Quaternion.Euler(0,0,angle);
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

    public void ParryNormalize()
    {
        //튕겨내기에 성공했을 때도 호출 할 것
        parryDuration = parryDurationMax;
    }

    public bool AttackAvailiableState()
    {
        return playerstate == PlayerState.Idle || playerstate == PlayerState.Running || 
            playerstate == PlayerState.Guard || playerstate == PlayerState.Parrying || playerstate == PlayerState.OnlyAttack;
    }

    public bool DamageAvailiableState(){
        return playerstate == PlayerState.Idle || playerstate == PlayerState.Running || 
            playerstate == PlayerState.Attack;
    }

    //공격 + 파랑 공격 패링 판정
    public void Attack(){
        if(blueList.Count > 0 && Vector2.Angle(blueList[0].attackDirection, -mouseVec) < 75){
            blueList.RemoveAt(0);
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
        playerstate = PlayerState.Guard;
        ParryNormalize();
    }

    // 빨강, 하양공격의 패리와 가드 판정
    public void Attacked( Vector3 attackCenter, int damage, Attacktype attacktype, float guardKnuckback = -1f){
        Vector3 attackVec =  attackCenter - transform.position;
        float angle = Vector2.Angle(mouseVec, attackVec);
        
        if(attacktype == Attacktype.white){
            if(playerstate == PlayerState.Parrying && angle < 75){
                Vector3 v3 = attackVec;
                Instantiate(parryEffect, transform.position + v3.normalized * 0.5f , Quaternion.identity);
                AfterParry();
                boss.StaminaDamaged();
                GuardKnuckback(-attackVec);
            }
            else if(playerstate == PlayerState.Guard && angle < 75){
                DamagedSmall(damage / 2, - attackVec.normalized);
                GuardKnuckback(-attackVec);
            }

            else if( DamageAvailiableState() || playerstate == PlayerState.Parrying || playerstate == PlayerState.Guard){
                DamagedBig(damage, -attackVec.normalized);
                
            }
        }
        else if(attacktype == Attacktype.red){
            if(playerstate == PlayerState.Roll && Vector2.Angle(attackVec, rollDirection) < 75){
                StopAllCoroutines();
                transform.DOMove(transform.position + V2toV3(rollDirection).normalized * 1.2f, zoomMaxTime);
                boss.StaminaDamaged(30, true);
                playerstate = PlayerState.OnlyAttack;
            }
            else if(DamageAvailiableState() || playerstate == PlayerState.Parrying || playerstate == PlayerState.Guard) {
                DamagedBig(damage, -attackVec.normalized);                
            }
        }
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

    public void AttackedBlue( Vector3 attackCenter, Vector3 targetPosition, int damage, float duration){
        blueList.Add(new BlueAttack(attackCenter, targetPosition, damage, Time.time + duration));
        blueList.Sort((a,b) => a.time.CompareTo(b.time));
    }

    public void BlueAttackProcess(){
        if(blueList.Count != 0){
            if(blueList[0].time < Time.time){
                BlueAttack ba = blueList[0];
                if(DamageAvailiableState() || playerstate == PlayerState.Parrying || playerstate == PlayerState.Guard) {
                    RaycastHit2D hit = Physics2D.Raycast(ba.startPoint, ba.attackDirection, LayerMask.GetMask("Player"));
                    if(hit.collider != null){
                        DamagedBig(ba.damage, ba.attackDirection);
                        // 맞았다는 신호 + 애니메이션 재생 
                        // 아마 List에 있는 blue 전부 지워버리고 -> boss 쪽 새 패턴 추출하는 식으로 할 듯?
                    }
                }
                blueList.RemoveAt(0);
                //맞지는 않음 + 애니메이션 재생
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
        boss.CeasePattern(true);

    }

    //guard 했을때의 데미지
    public void DamagedSmall(int damage, Vector3 vec){
        playerHealth -= damage;
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

    public Vector3 V2toV3(Vector2 v){
        Vector3 v3 = v;
        return v3;
    }
    
    //For Debug
    public void PlayerColor(){
        if(playerstate == PlayerState.Damaged){
            sr.color = Color.red;
        }
        else if(playerstate == PlayerState.Parrying){
            sr.color = Color.green;
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
    private void OnDrawGizmos() {
        Gizmos.color = Color.blue;
    }
}
