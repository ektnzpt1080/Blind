using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


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

    Collider2D playerCollider;
    [SerializeField] GameObject attackRange;
    Collider2D attackCollider;

    [SerializeField] Boss1Behaviour boss;

    public GameObject parryEffect; // parry 했을 때 이펙트


    //건드리면 안됨
    [SerializeField] float lastGuardTime; //Player가 마지막으로 가드를 끝낸 시간
    [SerializeField] float lastRollTime; //Player가 마지막으로 구르기를 한 시간
    [SerializeField] float lastStaminaSpendTime; //Player가 마지막으로 구르거나 달린 시간 (스태미나 회복용)
    [SerializeField] float parryDuration; //Player 현재 패링 시간
    [SerializeField] float guardTime; //Player가 가드하고 있는 시간
    [SerializeField] float playerStamina; //Player 스태미나
    [SerializeField] PlayerState playerstate; //PlayerState


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

        PlayerColor();
    }

    void PlayerControl()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (AttackAvailiableState()) {
                Debug.Log("attack");
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
            if (guardTime > parryDuration) playerstate = PlayerState.Guard;
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

        int x = 0, y = 0;
        if (Input.GetKey(KeyCode.D)) x = 1;
        else if (Input.GetKey(KeyCode.A)) x = -1;
        if (Input.GetKey(KeyCode.W)) y = 1;
        else if (Input.GetKey(KeyCode.S)) y = -1;

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (playerstate == PlayerState.Idle && (x != 0 || y != 0) && lastRollTime + playerRollCooltime < Time.time && playerStamina > 0.95f)
            {
                StartCoroutine(Rolling(new Vector2(x,y)));
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            if (playerstate == PlayerState.Running) playerstate = PlayerState.Idle;
        }

        if (playerstate == PlayerState.Idle)
        {
            transform.Translate(new Vector3(x, y, 0).normalized * playerMoveSpeed * Time.deltaTime);
        }
        else if(playerstate == PlayerState.Running){
            transform.Translate(new Vector3(x, y, 0).normalized * playerRunningSpeed * Time.deltaTime);
            lastStaminaSpendTime = Time.time;
            playerStamina -= playerRunningStaminaSpendingSpeed * Time.deltaTime;
            if(playerStamina < 0 || (x == 0 && y == 0)) playerstate = PlayerState.Idle;
        }

    }

    IEnumerator Rolling(Vector2 v)
    {
        lastRollTime = Time.time;
        lastStaminaSpendTime = Time.time;
        playerstate = PlayerState.Roll;
        playerStamina -= 1.0f;
        for(int i = 0 ; i < playerRollTime / Time.deltaTime; i++){
            transform.Translate(new Vector3(v.x, v.y, 0).normalized * playerRollSpeed * Time.deltaTime);
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

    public void AfterParry(){
        playerstate = PlayerState.Guard;
        ParryNormalize();
    }

    public void Attacked( Vector3 attackCenter, int damage, bool isParryable ){
        Vector3 mouseVec = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        Vector3 attackVec =  attackCenter - transform.position;
        float angle = Vector2.Angle(mouseVec, attackVec);
        if(playerstate == PlayerState.Parrying && isParryable && angle < 75){
            Vector3 v3 = attackVec;
            Debug.Log("Attack Parried");
            Instantiate(parryEffect, transform.position + v3.normalized * 0.5f , Quaternion.identity);
            AfterParry();
            GuardKnuckback(-attackVec);
            //TODO 패리 상황
        }
        else if(playerstate == PlayerState.Guard && isParryable && angle < 75){
            DamagedSmall(damage / 3, - attackVec.normalized);
            GuardKnuckback(-attackVec);

            //TODO 가드 상황
        }
        else{
            DamagedBig(damage, - attackVec.normalized);
            //TODO Damaged 상황
        }
    }
    
    public void DamagedBig(int damage, Vector3 vec){
        playerstate = PlayerState.Damaged;
        playerHealth -= damage;
        if(playerHealth <= 0 ){
            // TODO 게임오버 처리
            return;
        }
        transform.DOMove(transform.position + vec.normalized * knuckbackBig, 1.2f).SetEase(Ease.OutCubic).OnComplete(() => {
            playerstate = PlayerState.Idle;
        });

    }

    public void DamagedSmall(int damage, Vector3 vec){
        playerstate = PlayerState.Damaged;
        playerHealth -= damage;
    }

    public void GuardKnuckback(Vector3 vec, float knuckback = -1f){
        float f = knuckback;
        if (f < 0) {
            f = knuckbackSmall;
        }
        transform.DOMove(transform.position + vec * f, 0.1f).SetEase(Ease.OutCubic);
    }

    public void Attack(){
        
        //애니메이션 넣기
        playerstate = PlayerState.Attack;
        Vector2 mouseVec = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
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
        else{
            sr.color = Color.white;
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.blue;

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
}
