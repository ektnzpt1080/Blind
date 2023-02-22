using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerQTE : MonoBehaviour
{
    bool isQTEEnable;
    [SerializeField] QTEBoard board;
    PlayerBehaviour pb;
    [SerializeField] Canvas canvas;
    QTEBoard playingBoard;
    CameraSetting camerasetting;


    void Start(){
        pb = GetComponent<PlayerBehaviour>();
        camerasetting = GameManager.Instance.CameraSetting;
    }

    public void StartQTE( int noteNum ){
        playingBoard = Instantiate(board, canvas.transform);
        playingBoard.StartQTEBoard(noteNum, pb, this);
        camerasetting.QTEZoomIn();
    }

    public void EndQTE(){
        camerasetting.EndZoom();
        pb.EndQTE();
    }

    public void BreakQTE(){
        if(playingBoard is not null){
            Destroy(playingBoard.gameObject);
        }
        camerasetting.EndZoom();
    }

    public PlayerBehaviour GetPlayerBehaviour(){
        return pb;
    }
}
