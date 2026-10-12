using UnityEngine;

public class Fighters : MonoBehaviour
{
    [SerializeField] private WheelController wheelController;
    [SerializeField] private float moveSpeed = 5f;

    [Header("Controls")]
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;

    [Header("Properties")]
    public float mass = 2f;
    [HideInInspector] public float walkAngle = 0f;
    private bool moveLeft = false;
    private bool moveRight = false;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
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
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (moveLeft)
        {
            walkAngle -= (moveSpeed / wheelController.radius) * dt;
        }

        if (moveRight)
        {
            walkAngle += (moveSpeed / wheelController.radius) * dt;
        }

        float x = Mathf.Sin(walkAngle) * wheelController.radius;
        float y = Mathf.Cos(walkAngle) * wheelController.radius;

        transform.position = new Vector3(wheelController.transform.position.x + x, wheelController.transform.position.y + y, startPosition.z);
    }
}
