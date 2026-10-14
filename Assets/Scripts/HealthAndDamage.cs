using UnityEngine;

public class HealthAndDamage : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private GameObject bloodEffectPrefab;
    private float currentHealth;
    private Fighters fighter;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Start()
    {
        currentHealth = maxHealth;
        fighter = GetComponent<Fighters>();
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position + Vector3.up * 0.5f, Vector3.zero);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (bloodEffectPrefab != null)
        {
            Quaternion rotation = hitDirection.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(hitDirection) : Quaternion.identity;
            Instantiate(bloodEffectPrefab, hitPoint, rotation);
        }

        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " has been defeated!");
        }

        if (fighter != null)
        {
            fighter.OnDamaged(IsDead);
        }
    }
}
