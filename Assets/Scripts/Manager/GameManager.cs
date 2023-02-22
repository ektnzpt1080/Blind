using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public UIManager UIManager;
    public CameraSetting CameraSetting;
    public AudioManager AudioManager;
    public UnityEvent DestructAttackParticles;
    public UnityEvent DestructAfterimage;
    public UnityEvent DestructChargeParticles;
    

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(this);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(this.gameObject);
        if(SceneManager.GetActiveScene().name == "Boss1"){
            UIManager = GameObject.Find("UIManager").GetComponentInChildren<UIManager>();
            CameraSetting = GameObject.Find("CM vcam1").GetComponent<CameraSetting>();
        }
    }
    
    void Update(){
        if(Input.GetKeyDown(KeyCode.F1)){
            SceneManager.LoadScene("MainMenu");
        }
        if(Input.GetKeyDown(KeyCode.R) && SceneManager.GetActiveScene().name == "Boss1"){
            SceneManager.LoadScene("Boss1");
        }
    }
    
}
