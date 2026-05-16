using UnityEngine;

public class LazerPassThroughTrigger : MonoBehaviour
{
    public LazerStateController controller;

    public LazerStateController Controller => controller;

    public void Configure(LazerStateController targetController)
    {
        controller = targetController;
    }

    private void Reset()
    {
        controller = GetComponentInParent<LazerStateController>();

        if (TryGetComponent(out Collider triggerCollider))
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<LazerStateController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (controller == null || !controller.TryGetPlayer(other, out PlayerManager player))
        {
            return;
        }

        controller.NotifyPlayerEnteredPassThroughTrigger(player);
    }

    private void OnTriggerExit(Collider other)
    {
        if (controller == null || !controller.TryGetPlayer(other, out PlayerManager player))
        {
            return;
        }

        controller.NotifyPlayerExitedPassThroughTrigger(player);
    }
}
