using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderPosition : MonoBehaviour
{
    [SerializeField]
    private float worldOffSetY = 1.5f;
    private Vector3 distance = Vector3.up * 1;
    private Transform targetTransform;
    private RectTransform rectTransform;
    private Camera mainCam;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
    }
    public void Setup(Transform target,Camera cam)
    {
        targetTransform = target;
        mainCam = cam;
        return;
    }
    private void LateUpdate()
    {
        if(targetTransform == null)
        {
            Destroy(gameObject);
            return;
        }
        if (mainCam == null) return;
        Vector3 worldPos = targetTransform.position+Vector3.down * worldOffSetY;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        rectTransform.position = screenPos + distance;
    }  
}
