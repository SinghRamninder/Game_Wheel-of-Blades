using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    public enum AttackType
    {
        Normal,
        Counter,
        Parry
    }

    [SerializeField] private GameObject owner;
    [SerializeField] private GameObject opponent;
    [Tooltip("Thickness of the blade used for the swept hit test.")]
    [SerializeField] private float bladeRadius = 0.035f;
    [SerializeField] private TrailRenderer trail;
    [Tooltip("Small glint on the blade when the parry window opens, so the defender can read it.")]
    [SerializeField] private GameObject parryCuePrefab;

    [HideInInspector] public bool canParryBeforeHit = false;
    [HideInInspector] public bool onGoingParry = false;

    public float ParryWindowOpenTime { get; private set; }
    public bool IsCounterAttack { get; private set; }
    public Vector3 BladeTip => transform.TransformPoint(bladeEndLocal);
    public Vector3 BladeCenter => transform.TransformPoint((bladeStartLocal + bladeEndLocal) * 0.5f);

    private Collider col;
    private Fighters ownerFighter;
    private Fighters opponentFighter;

    private bool hasHit = false;
    private bool isActive = false;
    private float damage;
    private AttackType attackType = AttackType.Normal;
    private Vector3 bladeStartLocal;
    private Vector3 bladeEndLocal;
    private Vector3 prevStart;
    private Vector3 prevEnd;

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.enabled = false;
        ownerFighter = owner != null ? owner.GetComponent<Fighters>() : GetComponentInParent<Fighters>();
        opponentFighter = opponent.GetComponent<Fighters>();
        if (trail != null) trail.emitting = false;
        CalculateBlade();
    }

    private void CalculateBlade()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            bladeStartLocal = Vector3.zero;
            bladeEndLocal = Vector3.up;
            return;
        }

        Bounds b = mf.sharedMesh.bounds;
        int axis = 0;
        if (b.size.y > b.size[axis]) axis = 1;
        if (b.size.z > b.size[axis]) axis = 2;

        Vector3 min = b.center;
        Vector3 max = b.center;
        min[axis] = b.min[axis];
        max[axis] = b.max[axis];

        if (min.sqrMagnitude > max.sqrMagnitude)
        {
            (min, max) = (max, min);
        }
        bladeStartLocal = Vector3.Lerp(min, max, 0.25f);
        bladeEndLocal = max;
    }

    public void BeginAttack(float attackDamage, bool counterAttack, AttackType attackTypeOption)
    {
        damage = attackDamage;
        attackType = attackTypeOption;
        IsCounterAttack = counterAttack;
        hasHit = false;
        onGoingParry = false;
        canParryBeforeHit = false;
        Close();
    }

    public void AllowParryBeforeHit()
    {
        if (ownerFighter == null || ownerFighter.State != Fighters.CombatState.Attacking) return;
        if (IsCounterAttack || onGoingParry) return;

        canParryBeforeHit = true;
        ParryWindowOpenTime = Time.time;

        if (parryCuePrefab != null)
        {
            Instantiate(parryCuePrefab, BladeTip, Quaternion.identity, transform);
        }
    }

    public bool IsParryWindowOpen(float window)
    {
        return canParryBeforeHit && !hasHit && !onGoingParry && Time.time - ParryWindowOpenTime <= window;
    }

    public void Open()
    {
        if (ownerFighter == null || !ownerFighter.IsAttacking || onGoingParry) return;

        isActive = true;
        col.enabled = true;
        prevStart = transform.TransformPoint(bladeStartLocal);
        prevEnd = BladeTip;
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }
    }

    public void Close()
    {
        isActive = false;
        col.enabled = false;
        canParryBeforeHit = false;
        if (trail != null) trail.emitting = false;
    }

    public void Parried()
    {
        onGoingParry = true;
        attackType = AttackType.Parry;
        SetKnock();
        Close();
    }

    public void Cancel()
    {
        Close();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            TryHit(other, other.ClosestPoint(BladeCenter));
        }
    }

    private void LateUpdate()
    {
        if (!isActive || hasHit || ownerFighter == null || ownerFighter.Opponent == null) return;

        Collider target = ownerFighter.Opponent.GetComponent<Collider>();
        Vector3 start = transform.TransformPoint(bladeStartLocal);
        Vector3 end = BladeTip;

        if (target != null && target.enabled)
        {
            const int sweepSteps = 4;
            const int bladeSteps = 5;
            for (int i = 1; i <= sweepSteps && !hasHit; i++)
            {
                float t = (float)i / sweepSteps;
                Vector3 a = Vector3.Lerp(prevStart, start, t);
                Vector3 b = Vector3.Lerp(prevEnd, end, t);
                for (int j = 0; j <= bladeSteps; j++)
                {
                    Vector3 p = Vector3.Lerp(a, b, (float)j / bladeSteps);
                    Vector3 closest = target.ClosestPoint(p);
                    if ((closest - p).sqrMagnitude <= bladeRadius * bladeRadius)
                    {
                        TryHit(target, closest);
                        break;
                    }
                }
            }
        }

        prevStart = start;
        prevEnd = end;
    }

    private void TryHit(Collider other, Vector3 hitPoint)
    {
        if (hasHit || onGoingParry) return;
        if (other.gameObject == owner || !other.CompareTag("Pirate")) return;

        HealthAndDamage opponentHealth = other.GetComponent<HealthAndDamage>();
        if (opponentHealth == null || opponentHealth.IsDead) return;

        hasHit = true;
        canParryBeforeHit = false;

        Vector3 swing = BladeCenter - Vector3.Lerp(prevStart, prevEnd, 0.5f);
        Vector3 away = other.transform.position - owner.transform.position;
        away.y = 0f;
        Vector3 direction = (swing.normalized + away.normalized).normalized;

        opponentHealth.TakeDamage(damage, hitPoint, direction);
        SetKnock();

        CombatFeedback.HitStop(IsCounterAttack ? 0.12f : 0.08f);
        CombatFeedback.Shake(IsCounterAttack ? 0.035f : 0.02f, 0.18f);
    }

    private void SetKnock()
    {
        float dir = Mathf.Sign(opponentFighter.walkAngle - ownerFighter.walkAngle);
        switch (attackType)
        {
            case AttackType.Normal:
                opponentFighter.Knock(dir * ownerFighter.fighterProperties.attackImpusle);
                ownerFighter.Knock(-dir * ownerFighter.fighterProperties.attackImpusle * ownerFighter.fighterProperties.recoil);
                Debug.Log("Normal attack knock");
                break;
            case AttackType.Counter:
                opponentFighter.Knock(dir * ownerFighter.fighterProperties.counterAttackImpusle);
                ownerFighter.Knock(-dir * ownerFighter.fighterProperties.counterAttackImpusle * ownerFighter.fighterProperties.recoil);
                Debug.Log("Counter attack knock");
                break;
            case AttackType.Parry:
                opponentFighter.Knock(dir * ownerFighter.fighterProperties.parryImpusle);
                ownerFighter.Knock(-dir * ownerFighter.fighterProperties.parryImpusle * ownerFighter.fighterProperties.recoil);
                Debug.Log("Parry knock");
                break;
        }
    }
}
