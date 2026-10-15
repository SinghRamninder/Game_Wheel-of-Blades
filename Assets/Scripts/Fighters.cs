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
    private const string WalkForwardState = "WalkForward";
    private const string WalkBackState = "WalkBack";
    private static readonly string[] AttackStates = { "Attack1", "Attack2" };

    public FighterProperties fighterProperties;
    [SerializeField] private WheelController wheelController;
    [SerializeField] private Fighters opponent;
    [SerializeField] private Animator animator;
    [SerializeField] private bool mirrorAnimations = false;
    [SerializeField] private float walkAnimationSpeed = 0.75f;

    [Header("Effects")]
    [SerializeField] private GameObject parrySparksPrefab;
    [SerializeField] private AudioClip[] parrySounds;

    [Header("Controls")]
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;
    [SerializeField] private KeyCode attackKey = KeyCode.Space;
    [SerializeField] private KeyCode parryKey = KeyCode.S;
    [SerializeField] private KeyCode counterAttackKey = KeyCode.W;

    [Header("Properties")]
    [SerializeField] private float startWalkAngle = 0f;
    private float slide = 0;
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
    private bool canFight = true;
    private int walkDirection = 0;

    private void Start()
    {
        startPosition = transform.position;
        walkAngle = startWalkAngle;
        sword = GetComponentInChildren<SwordAttack>();
        opponentSword = opponent.GetComponentInChildren<SwordAttack>();
        if (animator != null){
            animator.SetBool("Mirror", mirrorAnimations);
            animator.SetFloat("WalkSpeed", fighterProperties.moveSpeed / walkAnimationSpeed);
        }
    }

    private void Update()
    {
        if (State == CombatState.Dead || !canFight) return;

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
        UpdateWalkAnimation();
    }

    private void UpdateWalkAnimation()
    {
        if (State != CombatState.Idle) return;

        float facing = Mathf.Sign(transform.forward.x);
        int direction = (int)Mathf.Sign(GetInput() * facing);
        if (GetInput() == 0f)
        {
            direction = 0;
        }

        if (direction == walkDirection) return;
        walkDirection = direction;

        if (direction > 0)
        {
            PlayState(WalkForwardState, 0.12f);
        }
        else if (direction < 0)
        {
            PlayState(WalkBackState, 0.12f);
        }
        else
        {
            PlayState(IdleState, 0.15f);
        }
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
        sword.BeginAttack(fighterProperties.attackDamage, false, SwordAttack.AttackType.Normal);
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
        sword.BeginAttack(fighterProperties.counterAttackDamage, true, SwordAttack.AttackType.Counter);
        PlayState(CounterState, 0.05f);
        return true;
    }

    private void UpdateParry()
    {
        if (State != CombatState.Parrying || parrySucceeded || opponentSword == null) return;
        if (!opponentSword.IsParryWindowOpen(fighterProperties.parryWindow)) return;

        if (lastParryPressTime < opponentSword.ParryWindowOpenTime - fighterProperties.parryInputBuffer) return;

        if (Mathf.Abs(GapToOpponent()) > fighterProperties.attackRange) return;

        ParrySuccess();
    }

    private void ParrySuccess()
    {
        parrySucceeded = true;
        canCounterAttack = true;
        counterAttackEndTime = Time.time + fighterProperties.counterAttackWindow;

        Vector3 contact = sword != null ? Vector3.Lerp(sword.BladeCenter, opponentSword.BladeCenter, 0.5f) : opponentSword.BladeCenter;

        opponentSword.Parried();
        opponent.OnAttackParried();

        CombatFeedback.Spawn(parrySparksPrefab, contact, Quaternion.LookRotation(Vector3.back));
        CombatFeedback.PlaySound(parrySounds, 0.9f);
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

        // Actions play their own animation; walking is picked again once the fighter is idle.
        if (state != CombatState.Idle)
        {
            walkDirection = 0;
        }
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
            walkAngle -= (fighterProperties.moveSpeed / wheelController.radius) * dt;
        }

        if (moveRight && CanMove)
        {
            walkAngle += (fighterProperties.moveSpeed / wheelController.radius) * dt;
        }

        walkAngle += wheelController.spin * dt;

        slide -= slide * fighterProperties.slideFriction * dt;
        walkAngle += slide * dt;

        float gap = GapToOpponent();
        if (Mathf.Abs(gap) < fighterProperties.minGap)
        {
            walkAngle = opponent.walkAngle + Mathf.Sign(gap) * fighterProperties.minGap / wheelController.radius;
        }

        if (Mathf.Abs(walkAngle) > fighterProperties.fallAngleDegree * Mathf.Deg2Rad)
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

    public void SetCanFight(bool value)
    {
        canFight = value;
        if (!value)
        {
            moveLeft = false;
            moveRight = false;
            if (walkDirection != 0 && State == CombatState.Idle)
            {
                walkDirection = 0;
                PlayState(IdleState, 0.15f);
            }
        }
    }

    public void ResetForRound()
    {
        walkAngle = startWalkAngle;
        hasFallen = false;
        slide = 0f;
        fallVelocity = Vector3.zero;
        moveLeft = false;
        moveRight = false;
        canCounterAttack = false;
        parrySucceeded = false;
        walkDirection = 0;

        if (sword != null) sword.Cancel();
        SetState(CombatState.Idle);
        if (animator != null)
        {
            animator.Play(IdleState, 0, 0f);
        }
    }

    public void Knock(float impulse)
    {
        slide += impulse / (fighterProperties.mass * wheelController.radius);
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
