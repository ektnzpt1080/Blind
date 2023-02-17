using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BanishingObject : MonoBehaviour
{

    SpriteRenderer sr;
    public float duration;

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        sr.color -= new Color(0, 0, 0, 1 / (duration / Time.deltaTime));
        if(sr.color.a <= 0) Destroy(this.gameObject);
    }

    public void SetDuration(float _duration){
        duration = _duration;
    }
}
