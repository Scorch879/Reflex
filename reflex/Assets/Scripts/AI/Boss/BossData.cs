using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "Enemy/BossData")]
public class BossData : ScriptableObject
{
    public float maxHealth;
    public float animationSpeed = 1f;
    public float attack1Damage = 15f;
    public float attack2Damage = 20f;
    public float laserDamage = 25f;
    public float stunDuration;
    public int phaseCount = 1;

    public float minAttackInterval = 1f;
    public float maxAttackInterval = 3f;
}
