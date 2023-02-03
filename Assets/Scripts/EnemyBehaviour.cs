using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    enum EnemyState{
        Ready,
        Action,
        Damaged
    }

    EnemyState enemyState;
    Animator animator;
    
    
    [SerializeField] GameObject footPrintObject;
    // Start is called before the first frame update
    void Start()
    {
        enemyState = EnemyState.Ready;
    }

    // Update is called once per frame
    void Update()
    {
        if(enemyState == EnemyState.Ready){
            int i = Random.Range(0,4);
            int x = (i == 0) ? -1 : (i == 1) ? 1 : 0;
            int y = (i == 2) ? -1 : (i == 3) ? 1 : 0;
            enemyState = EnemyState.Action;
            
            x = 1;
            y = 0;
            StartCoroutine(Moving(new Vector2 (x,y)));
        }
    }

    IEnumerator Moving(Vector2 v){
        Coroutine coroutine = StartCoroutine(FootPrint(0.5f));
        for(int i = 0 ; i < 7.0f / Time.deltaTime; i++){
            transform.Translate(new Vector3(v.x, v.y, 0).normalized * 2.0f * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }
        StopCoroutine(coroutine);
        yield return new WaitForSeconds(1.0f);
        enemyState = EnemyState.Ready;
    }

    IEnumerator FootPrint(float f){
        Vector3 offset = new Vector3(0, 0.2f, 0);
        while(true){
            Instantiate(footPrintObject, this.transform.position + offset, this.transform.rotation);
            yield return new WaitForSeconds(f);
            GameObject g = Instantiate(footPrintObject, this.transform.position - offset, this.transform.rotation);
            g.transform.localScale = Vector3.Scale(g.transform.localScale, new Vector3(1f, -1f, 1f));
            yield return new WaitForSeconds(f);
        }
    }

    

}
