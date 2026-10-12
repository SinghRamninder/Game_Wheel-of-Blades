using UnityEngine;

public class Fighters : MonoBehaviour
{
    [SerializeField] private WheelController wheelController;
    [SerializeField] private Fighters opponent;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minGap = 0.5f;
    [SerializeField] private Animator animator;

    [Header("Controls")]
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;
    [SerializeField] private KeyCode attackKey = KeyCode.Space;

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

    private void Start()
    {
        startPosition = transform.position;
        walkAngle = startWalkAngle;
    }

    private void Update()
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

        if (animator != null && Input.GetKeyDown(attackKey))
        {
            animator.SetTrigger("Attack");
        }
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (hasFallen)
        {
            fallVelocity.y -= 9.81f * dt;
            transform.position += fallVelocity * dt;
            return;
        }

        if (moveLeft)
        {
            walkAngle -= (moveSpeed / wheelController.radius) * dt;
        }

        if (moveRight)
        {
            walkAngle += (moveSpeed / wheelController.radius) * dt;
        }

        walkAngle += wheelController.spin * dt;

        float gap = (walkAngle - opponent.walkAngle) * wheelController.radius;
        if (Mathf.Abs(gap) < minGap)
        {
            walkAngle = opponent.walkAngle + Mathf.Sign(gap) * minGap / wheelController.radius;
        }

        if (Mathf.Abs(walkAngle) > fallAngleDegree * Mathf.Deg2Rad)
        {
            hasFallen = true;
            fallVelocity = new Vector3(Mathf.Sign(walkAngle) * 2f, 1f, 0f);
        }

        float x = Mathf.Sin(walkAngle) * wheelController.radius;
        float y = Mathf.Cos(walkAngle) * wheelController.radius;

        transform.position = new Vector3(wheelController.transform.position.x + x, wheelController.transform.position.y + y, startPosition.z);
    }

    public float GetInput()
    {
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
