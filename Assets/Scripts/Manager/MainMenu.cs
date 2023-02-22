using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame(){
        SceneManager.LoadScene("Boss1");
    }

    public void EndGame(){
        Application.Quit();
    }

    void Update(){
        
    }
}
