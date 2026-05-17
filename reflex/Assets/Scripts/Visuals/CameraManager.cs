using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    private CinemachineBasicMultiChannelPerlin cameraNoise;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        cameraNoise = GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public IEnumerator ShakeCamera(float intensity, float duration)
    {
        cameraNoise.AmplitudeGain = intensity;
        yield return new WaitForSeconds(duration);
        //lerp back to 0
        cameraNoise.AmplitudeGain = 0f;
    }
}
