using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public UIManager UIManager;
    public CameraSetting CameraSetting;
    public UnityEvent DestructAttackParticles;
    public UnityEvent DestructAfterimage;

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(this);
            return;
        }
        Instance = this;
        UIManager = GetComponentInChildren<UIManager>();
        CameraSetting = GameObject.Find("CM vcam1").GetComponent<CameraSetting>();
    }
    
    
}
