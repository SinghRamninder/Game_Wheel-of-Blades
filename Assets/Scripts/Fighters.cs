using UnityEngine;

public class Fighters : MonoBehaviour
{
    public enum CombatState { Idle, Attacking, Parrying, Staggered, Countering, HitStun, Dead }

    private const string IdleState = "FightIdle";
    private const string ParryState = "Parry";
    private const string ParriedState = "Parried";
    private const string CounterState = "Counter";
    private const string HitReactState = "HitReact";
    private const string DefeatedState = "Defeated";
    private static readonly string[] AttackStates = { "Attack1", "Attack2" };

    [SerializeField] private WheelController wheelController;
    [SerializeField] private Fighters opponent;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minGap = 0.5f;
    [SerializeField] private Animator animator;
    [SerializeField] private bool mirrorAnimations = false;

    [Header("Attack and Defense")]
    [SerializeField] private float attackRange = 0.8f;
    [Tooltip("How long the parry window stays open after the attacker's AllowParryBeforeHit event.")]
    [SerializeField] private float parryWindow = 0.3f;
    [Tooltip("Pressing parry this much before the window opens still counts.")]
    [SerializeField] private float parryInputBuffer = 0.12f;
    [SerializeField] private float counterAttackWindow = 0.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float counterAttackDamage = 25f;

    [Header("Effects")]
    [SerializeField] private GameObject parrySparksPrefab;

    [Header("Controls")]
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;
    [SerializeField] private KeyCode attackKey = KeyCode.Space;
    [SerializeField] private KeyCode parryKey = KeyCode.S;
    [SerializeField] private KeyCode counterAttackKey = KeyCode.W;

    [Header("Properties")]
    public float mass = 2f;
    public float footPushForce = 5f;
    [SerializeField] private float startWalkAngle = 0f;
    [SerializeField] private float fallAngleDegree = 70f;
    [HideInInspector] public float walkAngle = 0.3f;
    [HideInInspector] public bool hasFallen = false;
    private bool moveLeft = false;
    private bool moveRight = false;
    private Vector3 startPosition;
    private Vector3 fallVelocity;

    public CombatState State { get; private set; } = CombatState.Idle;
    public bool IsAttacking => State == CombatState.Attacking || State == CombatState.Countering;
    public Fighters Opponent => opponent;

    private SwordAttack sword;
    private SwordAttack opponentSword;
    private int nextAttack = 0;
    private float actionStartTime;
    private float lastParryPressTime = -10f;
    private bool parrySucceeded = false;
    private bool canCounterAttack = false;
    private float counterAttackEndTime;

    private void Start()
    {
        startPosition = transform.position;
        walkAngle = startWalkAngle;
        sword = GetComponentInChildren<SwordAttack>();
        opponentSword = opponent.GetComponentInChildren<SwordAttack>();
        if (animator != null){
            animator.SetBool("Mirror", mirrorAnimations);
        }
    }

    private void Update()
    {
        if (State == CombatState.Dead) return;

        ReadMovementInput();

        if (Input.GetKeyDown(attackKey) && !TryCounterAttack())
        {
            TryAttack();
        }

        if (Input.GetKeyDown(counterAttackKey))
        {
            TryCounterAttack();
        }

        if (Input.GetKeyDown(parryKey))
        {
            TryParry();
        }

        UpdateParry();
        UpdateCounterAttackWindow();
        UpdateActionEnd();
    }

    private void ReadMovementInput()
    {
        if (Input.GetKey(moveLeftKey))
        {
            moveLeft = true;
            moveRight = false;
        }

        if (Input.GetKey(moveRightKey))
        {
            moveRight = true;
            moveLeft = false;
        }

        if (Input.GetKeyUp(moveLeftKey))
        {
            moveLeft = false;
        }

        if (Input.GetKeyUp(moveRightKey))
        {
            moveRight = false;
        }
    }

    public bool TryAttack()
    {
        if (State != CombatState.Idle || sword == null) return false;

        SetState(CombatState.Attacking);
        sword.BeginAttack(attackDamage, false);
        PlayState(AttackStates[nextAttack], 0.12f);
        nextAttack = (nextAttack + 1) % AttackStates.Length;
        return true;
    }

    public bool TryParry()
    {
        if (State != CombatState.Idle) return false;

        SetState(CombatState.Parrying);
        parrySucceeded = false;
        lastParryPressTime = Time.time;
        PlayState(ParryState, 0.06f);
        return true;
    }

    public bool TryCounterAttack()
    {
        if (!canCounterAttack || Time.time > counterAttackEndTime) return false;
        if (State != CombatState.Idle && State != CombatState.Parrying) return false;

        canCounterAttack = false;
        SetState(CombatState.Countering);
        sword.BeginAttack(counterAttackDamage, true);
        PlayState(CounterState, 0.05f);
        return true;
    }

    private void UpdateParry()
    {
        if (State != CombatState.Parrying || parrySucceeded || opponentSword == null) return;
        if (!opponentSword.IsParryWindowOpen(parryWindow)) return;

        if (lastParryPressTime < opponentSword.ParryWindowOpenTime - parryInputBuffer) return;

        if (Mathf.Abs(GapToOpponent()) > attackRange) return;

        ParrySuccess();
    }

    private void ParrySuccess()
    {
        parrySucceeded = true;
        canCounterAttack = true;
        counterAttackEndTime = Time.time + counterAttackWindow;

        Vector3 contact = sword != null ? Vector3.Lerp(sword.BladeCenter, opponentSword.BladeCenter, 0.5f) : opponentSword.BladeCenter;

        opponentSword.Parried();
        opponent.OnAttackParried();

        CombatFeedback.Spawn(parrySparksPrefab, contact, Quaternion.LookRotation(Vector3.back));
        CombatFeedback.HitStop(0.07f);
        CombatFeedback.Shake(0.015f, 0.12f);
    }

    private void UpdateCounterAttackWindow()
    {
        if (canCounterAttack && Time.time > counterAttackEndTime)
        {
            canCounterAttack = false;
        }
    }

    private void UpdateActionEnd()
    {
        if (State == CombatState.Idle || animator == null) return;
        if (Time.time - actionStartTime < 0.15f) return;

        bool backToIdle = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0).IsName(IdleState) : animator.GetCurrentAnimatorStateInfo(0).IsName(IdleState);

        if (backToIdle)
        {
            if (sword != null) sword.Cancel();
            SetState(CombatState.Idle);
        }
    }

    public void OnAttackParried()
    {
        if (State == CombatState.Dead) return;

        canCounterAttack = false;
        if (sword != null) sword.Cancel();
        SetState(CombatState.Staggered);
        PlayState(ParriedState, 0.05f);
    }

    public void OnDamaged(bool killed)
    {
        if (State == CombatState.Dead) return;

        canCounterAttack = false;
        if (sword != null) sword.Cancel();

        if (killed)
        {
            Die();
            return;
        }

        SetState(CombatState.HitStun);
        PlayState(HitReactState, 0.04f);
    }

    private void Die()
    {
        SetState(CombatState.Dead);
        PlayState(DefeatedState, 0.08f);
    }

    private void SetState(CombatState state)
    {
        State = state;
        actionStartTime = Time.time;
    }

    private void PlayState(string stateName, float fade)
    {
        if (animator != null)
        {
            animator.CrossFadeInFixedTime(stateName, fade, 0);
        }
    }

    private float GapToOpponent()
    {
        return (walkAngle - opponent.walkAngle) * wheelController.radius;
    }

    private bool CanMove => State == CombatState.Idle;

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (hasFallen)
        {
            fallVelocity.y -= 9.81f * dt;
            transform.position += fallVelocity * dt;
            return;
        }

        if (moveLeft && CanMove)
        {
            walkAngle -= (moveSpeed / wheelController.radius) * dt;
        }

        if (moveRight && CanMove)
        {
            walkAngle += (moveSpeed / wheelController.radius) * dt;
        }

        walkAngle += wheelController.spin * dt;

        float gap = GapToOpponent();
        if (Mathf.Abs(gap) < minGap)
        {
            walkAngle = opponent.walkAngle + Mathf.Sign(gap) * minGap / wheelController.radius;
        }

        if (Mathf.Abs(walkAngle) > fallAngleDegree * Mathf.Deg2Rad)
        {
            fallVelocity = new Vector3(Mathf.Sign(walkAngle) * 2f, 1f, 0f);
            hasFallen = true;
            if (sword != null) sword.Cancel();
            if (State != CombatState.Dead)
            {
                SetState(CombatState.Dead);
                PlayState(DefeatedState, 0.1f);
            }
        }

        float x = Mathf.Sin(walkAngle) * wheelController.radius;
        float y = Mathf.Cos(walkAngle) * wheelController.radius;

        transform.position = new Vector3(wheelController.transform.position.x + x, wheelController.transform.position.y + y, startPosition.z);
    }

    public float GetInput()
    {
        if (!CanMove) return 0f;

        float input = 0f;
        if (moveLeft)
        {
            input -= 1f;
        }
        if (moveRight)
        {
            input += 1f;
        }

        return input;
    }
}
