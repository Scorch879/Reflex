using System;
using UnityEngine;

public class Billboarding : MonoBehaviour
{
    [SerializeField] private Camera cameraObj;
    [SerializeField] private float xRotate;

    public void SetCamera(Camera sceneCamera)
    {
        cameraObj = sceneCamera;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Billboard();
    }

    private void Billboard()
    {
        if (cameraObj == null || !cameraObj.isActiveAndEnabled)
        {
            cameraObj = Camera.main;
        }

        if (cameraObj == null)
        {
            return;
        }

        Vector3 camPos = cameraObj.transform.position;
        camPos.y = transform.position.y;
        transform.LookAt(camPos);
        transform.Rotate(xRotate, 180f, 0f);
    }
}
