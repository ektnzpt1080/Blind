using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DG.Tweening;

public class CameraSetting : MonoBehaviour
{
    [SerializeField] CinemachineVirtualCamera cvc;
    [SerializeField] GameObject player;
    [SerializeField] GameObject boss;
    [SerializeField] float minimum = 5f;
    [SerializeField] float maximum = 7f;
    bool isZoom;

    CinemachineFramingTransposer ct;

    // Start is called before the first frame update
    void Awake(){
        isZoom = false;
        ct = cvc.AddCinemachineComponent<CinemachineFramingTransposer>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!isZoom){
            Vector2 vpb = Abs(player.transform.position - boss.transform.position);
            float propersize;
            if(vpb.x > 1.76 * vpb.y){
                propersize = vpb.x / 1.76f * 1.2f;
            }
            else{
                propersize = vpb.y * 1.2f;
            }
            
            propersize = Mathf.Lerp(cvc.m_Lens.OrthographicSize, propersize, Time.deltaTime * 1f);
            cvc.m_Lens.OrthographicSize = Mathf.Clamp(propersize, minimum, maximum);    
        }

    }

    Vector3 Abs(Vector3 v){
        return new Vector3 (Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    public void SetIsZoom(bool tf){
        isZoom = tf;
    }

    
    public void PreZoomIn(){
        Time.timeScale = 0.4f;
        isZoom = true;
        Vector2 vpb = Abs(player.transform.position - boss.transform.position);
        float propersize;
        if(vpb.x > 1.76 * vpb.y){
            propersize = vpb.x / 1.76f * 2f;
        }
        else{
            propersize = vpb.y * 2f;
        }
        cvc.m_Lens.OrthographicSize = propersize;
    }

    public void QTEZoomIn(){
        Time.timeScale = 1f;
        float propersize = 3.5f;
        ct.m_TrackedObjectOffset = new Vector3(0,-1,0);
        ct.OnTargetObjectWarped(player.transform, -transform.position);
        StartCoroutine(ZoomOut(propersize));
    }

    public float zoomoutspeed;

    IEnumerator ZoomOut(float propersize){
        //cvc.transform.position = new Vector3 (0,0,-10);
        while(cvc.m_Lens.OrthographicSize < propersize){
            cvc.m_Lens.OrthographicSize = Mathf.Lerp(cvc.m_Lens.OrthographicSize, propersize + 0.2f, Time.deltaTime * 0.8f);
            yield return new WaitForFixedUpdate();
        }
        cvc.m_Lens.OrthographicSize = propersize;
    }

    public void EndZoom(){
        Time.timeScale = 1f;
        SetIsZoom(false);
        ct.m_TrackedObjectOffset = new Vector3(0,0,0);
    }

    public void SmoothEndZoom(){
        Time.timeScale = 1f;
        StartCoroutine(SmoothZoomOut(4.5f));
    }

    IEnumerator SmoothZoomOut(float propersize){
        //cvc.transform.position = new Vector3 (0,0,-10);
        while(cvc.m_Lens.OrthographicSize < propersize){
            cvc.m_Lens.OrthographicSize = Mathf.Lerp(cvc.m_Lens.OrthographicSize, propersize + 0.2f, Time.deltaTime * 0.8f);
            yield return new WaitForFixedUpdate();
        }
        cvc.m_Lens.OrthographicSize = propersize;
        SetIsZoom(false);
    }
}
