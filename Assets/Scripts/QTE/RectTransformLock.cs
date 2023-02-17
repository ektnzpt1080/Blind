using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectTransformLock : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    private Vector3 startPosition;

    protected void Awake()
    {
        startPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (startPosition != null)
        {
            rectTransform.position = startPosition;
        }
    }
}
