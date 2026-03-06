using UnityEngine;

public class Billboarding : MonoBehaviour
{
    [SerializeField] private Camera cameraObj;


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
        transform.Rotate(20f, 180f, 0f);
    }
}
