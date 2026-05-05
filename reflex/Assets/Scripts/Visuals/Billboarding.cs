using System;
using UnityEngine;

public class Billboarding : MonoBehaviour
{
    [SerializeField] private Camera cameraObj;
    [SerializeField] private float xRotate;


    // Update is called once per frame
    void LateUpdate()
    {
        Billboard();
    }

    private void Billboard()
    {
        Vector3 camPos = cameraObj.transform.position;
        camPos.y = transform.position.y;
        transform.LookAt(camPos);
        transform.Rotate(xRotate, 180f, 0f);
    }
}
