using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    public WeaponData weaponData; // Drag your asset here

    public void Interact(PlayerManager player)
    {
        // Get the WeaponManager component from the player
        if (player.TryGetComponent<WeaponManager>(out WeaponManager weaponManager))
        {
            weaponManager.EquipWeapon(weaponData);
            Debug.Log($"<color=green>Picked up {weaponData.weaponName}!</color>");
        }
    }

    public string GetInteractionText()
    {
        return $"Equip {weaponData.weaponName}"; // e.g., "Equip Gauntlet"
    }
}