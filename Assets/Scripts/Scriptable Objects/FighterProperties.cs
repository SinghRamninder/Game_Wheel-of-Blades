using UnityEngine;

[CreateAssetMenu(fileName = "FighterProperties", menuName = "ScriptableObjects/FighterProperties")]
public class FighterProperties : ScriptableObject
{
    [Header("Movement Properties")]
    public float moveSpeed = 1f;
    public float minGap = 0.5f;
    public float mass = 2f;
    public float footPushForce = 5f;
    public float slideFriction = 4f;
    public float fallAngleDegree = 70f;

    [Header("Attack and Defense")]
    public float attackRange = 0.8f;
    [Tooltip("How long the parry window stays open after the attacker's AllowParryBeforeHit event.")]
    public float parryWindow = 0.3f;
    [Tooltip("Parry window against a counter attack (shorter than the normal one).")]
    public float counterParryWindow = 0.15f;
    [Tooltip("Pressing parry this much before the window opens still counts.")]
    public float parryInputBuffer = 0.12f;
    public float parryImpusle = 1f;
    public float counterAttackWindow = 0.5f;
    public float attackDamage = 10f;
    public float attackImpusle = 3f;
    public float counterAttackDamage = 25f;
    public float counterAttackImpusle = 5f;
    public float recoil = 0.3f;
}
