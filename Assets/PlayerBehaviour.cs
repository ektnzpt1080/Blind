using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

    [SerializeField] float playerMoveSpeed;
    [SerializeField] PlayerState playerstate;
    
    // Start is called before the first frame update
    void Start()
    {
        playerstate = PlayerState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerControl();
    }

    void PlayerControl()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Debug.Log("attack");
        }
        if(Input.GetMouseButtonDown(1))
        {
            Debug.Log("Parry and Denfense");
        }
        
        int x = 0, y = 0;
        if(Input.GetKey(KeyCode.D)) x = 1;
        else if(Input.GetKey(KeyCode.A)) x = -1;
        if(Input.GetKey(KeyCode.W)) y = 1;
        else if(Input.GetKey(KeyCode.S)) y = -1;
        
        if(Input.GetKeyDown(KeyCode.LeftShift))
        {
            Debug.Log("Roll");
        }
        
        transform.Translate(new Vector3(x, y, 0) * playerMoveSpeed * Time.deltaTime);

        

    }
}
