using UnityEngine;
using UnityEngine.UI;

public class HealthAndDamage : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private GameObject bloodEffectPrefab;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private float damageWobbleSpeed = 5f;
    [SerializeField] private float damageWobbleAmount = 5f;
    private Slider healthSlider;
    private float currentHealth;
    private Fighters fighter;

    private RectTransform rectTransform;
    private Quaternion startRotation;
    private bool applyDamage = false;
    private float wobbleTimer = 0f;
    private float wobbleDuration = 0.5f;


    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Start()
    {
        currentHealth = maxHealth;
        healthSlider = healthBar.GetComponent<Slider>();
        fighter = GetComponent<Fighters>();
        rectTransform = healthBar.GetComponent<RectTransform>();
        startRotation = rectTransform.localRotation;
    }

    private void LateUpdate()
    {
        if (applyDamage)
        {
            if (Mathf.Abs(healthSlider.value - (currentHealth / maxHealth)) > 0.001f)
            {
                healthSlider.value = Mathf.MoveTowards(healthSlider.value, currentHealth / maxHealth, Time.deltaTime * damageWobbleSpeed);

                wobbleTimer += Time.deltaTime;

                float progress = Mathf.Clamp01(wobbleTimer / wobbleDuration);

                float amplitude = (1f - progress) * damageWobbleAmount;

                float zAngle = Mathf.Sin(wobbleTimer * damageWobbleSpeed) * amplitude;

                rectTransform.localRotation = startRotation * Quaternion.Euler(0f, 0f, zAngle);

            }
            else
            {
                healthSlider.value = currentHealth / maxHealth;
                rectTransform.localRotation = startRotation;
                applyDamage = false;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position + Vector3.up * 0.5f, Vector3.zero);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (IsDead) return;

        applyDamage = true;
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
