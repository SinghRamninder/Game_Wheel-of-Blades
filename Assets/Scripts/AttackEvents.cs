using UnityEngine;

public class AttackEvents : MonoBehaviour
{
    [SerializeField] private SwordAttack sword;

    public void HitboxOn() { sword.Open(); }
    public void HitboxOff() { sword.Close(); }
    public void AllowParryBeforeHit() { sword.AllowParryBeforeHit(); }
    public void SwingSound() { sword.PlaySwingSound(); }
}
