using UnityEngine;

[CreateAssetMenu(fileName = "NewBuffCard", menuName = "Floor Buff/Buff Card")]
public class BuffCardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;

    [Header("Combat Buffs")]
    public float atkBonus;
    public float critBonus;
    public float comboWindowBonus;

    [Header("Fleet Foot (Dash)")]
    public float dashCDReduction;
    public float dashDistanceBonus;

    [Header("Economy & Utility")]
    public float essenceBonus;
    public float vampiricBonus;
    
    [Header("Special")]
    public bool isGlassCannon;
}