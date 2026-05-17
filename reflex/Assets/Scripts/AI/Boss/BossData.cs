using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "Enemy/BossData")]
public class BossData : ScriptableObject
{
    public float maxHealth;
    public float animationSpeed = 1f;
    public float attackDamage;
    public float stunDuration;
    public int phaseCount = 1;
}
