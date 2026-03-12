using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public float attackRate = 0.5f;
    public float damage = 10f;
    public float range = 3f;
}