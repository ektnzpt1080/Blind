using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [SerializeField] PlayerBehaviour player; //Player
    [SerializeField] BanishingObject footPrintObject; //Boss 발자국
    [SerializeField] float walkSpeed; //Boss가 움직이는 속도 (Walk)
    [SerializeField] float walkToPatternTime; //Boss가 Walk -> Pattern 에 들어가는 시간
    [SerializeField] float walkToPatternDistance; //Boss가 Walk -> Pattern 에 들어가는 거리
    [SerializeField] float walkMaxTime; //Boss가 Walk -> Chase에 들어가는 시간
    [SerializeField] float chaseSpeed; //Boss가 움직이는 속도 (Chase)
    [SerializeField] float footprintCoolTime; //Footprint 간격시간
    [SerializeField] float footprintTime; //Footprint 지속시간
    [SerializeField] float defenselessTime; //Defenseless 지속시간
    
    [SerializeField] float lastDefenselessTime; //
    [SerializeField] bool footprintEnable; //Boss Footprint 출력 여부
    Vector2 footprintDirection; //Footprint 방향


    // Start is called before the first frame update
    void Start()
    {
        enemystate = EnemyState.Ready;
        StartCoroutine(FootPrint());
        footprintEnable = true;
        
    }

    // Update is called once per frame
    void Update()
    {
        if(enemystate == EnemyState.Defenseless && Time.time - lastDefenselessTime > defenselessTime){
            enemystate = EnemyState.Ready;
        }
        else if(enemystate == EnemyState.Ready){
            enemystate = EnemyState.Start;
            StartCoroutine(Walk());
        }
        
        //Debug
        if(Input.GetKeyDown(KeyCode.Z)){
            StartCoroutine(Pattern1());
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
                Vector3 v = Vector3.Cross(transform.position - player.transform.position, Vector3.forward * direction);
                footprintDirection = v;
                transform.Translate(new Vector3(v.x, v.y, 0).normalized * walkSpeed * Time.deltaTime);
                yield return new WaitForFixedUpdate();
            }
            
        }
    }

    IEnumerator Chase(){
        FootprintEnable(true);
        //TODO : 패턴을 랜덤으로 뽑는 함수 만들것
        while(true){
            if( VectorBtoP().magnitude < max ){
                StartCoroutine(Pattern1());
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

    [SerializeField] float second;
    [SerializeField] float max;
    [SerializeField] float good;
    [SerializeField] float attackrange;
    IEnumerator Pattern1(){
        FootprintEnable(false);
        //TODO : 공격준비 알림
        yield return new WaitForSeconds(second);
        Vector3 v = VectorBtoP().normalized;
        Vector3 maxV = v * max;
        Vector3 goodV = VectorBtoP() - v * good;
        if(Vector3.Dot(goodV, v) < 0) goodV = Vector3.zero; // 너무 가까우면 제자리에서 패턴 진행
        Vector3 pos = (maxV.magnitude < goodV.magnitude) ? maxV : goodV;
        transform.Translate(pos);
        if (VectorBtoP().magnitude <= attackrange){
            player.Attacked(transform.position, 10, true);
        }
        //TODO : defenseless start
        
    }

    public void FootprintEnable(bool tf){
        footprintEnable = tf;
    }

    public Vector3 VectorBtoP(){
        return (player.transform.position - transform.position);
    }

    public void DefenselessStart(){
        lastDefenselessTime = Time.time;
        enemystate = EnemyState.Defenseless;
    }
}
