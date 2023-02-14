using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Attacktype = Pattern.AttackType;

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
        Special
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

    Collider2D playerCollider;
    [SerializeField] GameObject attackRange;
    Collider2D attackCollider;

    [SerializeField] Boss1Behaviour boss;

    public GameObject parryEffect; // parry 했을 때 이펙트


    //건드리면 안됨
    float lastGuardTime; //Player가 마지막으로 가드를 끝낸 시간
    float lastRollTime; //Player가 마지막으로 구르기를 한 시간
    float lastStaminaSpendTime; //Player가 마지막으로 구르거나 달린 시간 (스태미나 회복용)
    float parryDuration; //Player 현재 패링 시간
    float guardTime; //Player가 가드하고 있는 시간
    float playerStamina; //Player 스태미나
    [SerializeField] PlayerState playerstate; //PlayerState
    Vector2 moveDirection;
    List<BlueAttack> blueList;
    Camera mainCamera;
    SpriteRenderer sr;

    

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
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
    }

    void PlayerControl()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (AttackAvailiableState()) {
                Attack();
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

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (playerstate == PlayerState.Idle && (moveDirection.x != 0 || moveDirection.y != 0) && lastRollTime + playerRollCooltime < Time.time && playerStamina > 0.95f)
            {
                StartCoroutine(Rolling(new Vector2(moveDirection.x,moveDirection.y)));
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
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
        lastRollTime = Time.time;
        lastStaminaSpendTime = Time.time;
        playerstate = PlayerState.Roll;
        playerStamina -= 1.0f;
        for(int i = 0 ; i < playerRollTime / Time.deltaTime; i++){
            transform.Translate(V2toV3(v).normalized * playerRollSpeed * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }
        if(playerstate == PlayerState.Roll){
            if (Input.GetKey(KeyCode.LeftShift)) playerstate = PlayerState.Running;
            else playerstate = PlayerState.Idle;
        }

    }

    public void ParryNormalize()
    {
        //튕겨내기에 성공했을 때도 호출 할 것
        parryDuration = parryDurationMax;
    }

    public bool AttackAvailiableState()
    {
        return playerstate == PlayerState.Idle || playerstate == PlayerState.Running || 
            playerstate == PlayerState.Guard || playerstate == PlayerState.Parrying;
    }

    public bool DamageAvailiableState(){
        return playerstate == PlayerState.Idle || playerstate == PlayerState.Running || 
            playerstate == PlayerState.Attack;
    }

    //공격 + 파랑 공격 패링 판정
    public void Attack(){
        Vector2 mouseVec = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        //애니메이션 넣기
        if(blueList.Count > 0 && Vector2.Angle(blueList[0].attackDirection, -mouseVec) < 75){
            Debug.Log("blue attack parried");
            blueList.RemoveAt(0);
        }
        else{
            playerstate = PlayerState.Attack;
            float angle = Vector2.SignedAngle(Vector2.right, mouseVec);
            attackRange.transform.rotation = Quaternion.Euler(0,0,angle);
            ContactFilter2D cf2d = new ContactFilter2D();
            cf2d.SetLayerMask(LayerMask.GetMask("Boss"));
            List<Collider2D> res = new List<Collider2D>(); 
            if(attackCollider.OverlapCollider(cf2d, res) > 0) {
                Debug.Log("attack Success");
                boss.damaged();
            }
            transform.DOMove(transform.position + V2toV3(mouseVec).normalized * attackDashDistance, 0.2f).OnComplete(() => {
                if(playerstate == PlayerState.Attack) playerstate = PlayerState.Idle;
            });
        }
    }

    public void AfterParry(){
        playerstate = PlayerState.Guard;
        ParryNormalize();
    }

    // 빨강, 하양공격의 패리와 가드 판정
    public void Attacked( Vector3 attackCenter, int damage, Attacktype attacktype, float guardKnuckback = -1f){
        Vector3 mouseVec = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        Vector3 attackVec =  attackCenter - transform.position;
        float angle = Vector2.Angle(mouseVec, attackVec);
        
        if(attacktype == Attacktype.white){
            if(playerstate == PlayerState.Parrying && angle < 75){
                Vector3 v3 = attackVec;
                Debug.Log("Attack Parried");
                Instantiate(parryEffect, transform.position + v3.normalized * 0.5f , Quaternion.identity);
                AfterParry();
                GuardKnuckback(-attackVec);
            }
            else if(playerstate == PlayerState.Guard && angle < 75){
                DamagedSmall(damage / 3, - attackVec.normalized);
                GuardKnuckback(-attackVec);
            }

            else if( DamageAvailiableState() || playerstate == PlayerState.Parrying || playerstate == PlayerState.Guard){
                DamagedBig(damage, -attackVec.normalized);
                // TODO : 보스한테 캐릭터 맞았다는 신호 보내기
            }
        }
        else if(attacktype == Attacktype.red){
            if(playerstate == PlayerState.Roll){
                return;
            }
            else if(DamageAvailiableState() || playerstate == PlayerState.Parrying || playerstate == PlayerState.Guard) {
                DamagedBig(damage, -attackVec.normalized);
            }
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

    }

    //guard 했을때의 데미지
    public void DamagedSmall(int damage, Vector3 vec){
        playerHealth -= damage;
    }

    //가드또는 패링했을때의 넉백
    public void GuardKnuckback(Vector3 vec, float knuckback = -1f){
        float f = knuckback;
        if (f < 0) {
            f = knuckbackSmall;
        }
        transform.DOMove(transform.position + vec * f, 0.1f).SetEase(Ease.OutCubic);
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
        else{
            sr.color = Color.white;
        }
        
    }
    private void OnDrawGizmos() {
        Gizmos.color = Color.blue;
    }
}
