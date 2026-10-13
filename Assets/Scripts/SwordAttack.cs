using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    [SerializeField] private GameObject owner;
    private bool hasHit = false;

    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.enabled = false; 
    }

    public void Open() { hasHit = false; col.enabled = true; }
    public void Close() { col.enabled = false; }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (other.gameObject != owner && other.gameObject.CompareTag("Pirate"))
        {
            HealthAndDamage opponent = other.gameObject.GetComponent<HealthAndDamage>();
            if (opponent != null)
            {
                hasHit = true;
                Debug.Log("Opponent hit: " + opponent.name);
            }
        }
    }
}
