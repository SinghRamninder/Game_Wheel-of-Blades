using UnityEngine;

[DefaultExecutionOrder(-10)]
public class WheelController : MonoBehaviour
{
    [SerializeField] private Fighters[] fighters;
    [SerializeField] private float inertia = 6f;
    [SerializeField] private float damping = 0.8f;
    public float radius;
    [HideInInspector] public float spin;
    private float wheelAngle;

    private Quaternion startRotation;
    private Vector3 startPosition;

    private void Start()
    {
        startRotation = transform.rotation;
        startPosition = transform.position;
        radius = transform.lossyScale.x / 2f;
    }

    private void FixedUpdate()
    {
        // Frozen during announcements: the wheel keeps its current rotation and position.
        if (GameState.IsFrozen) return;

        float dt = Time.fixedDeltaTime;

        float torque = 0f;
        foreach (Fighters fighter in fighters)
        {
            if (fighter.hasFallen) continue;

            torque += fighter.fighterProperties.mass * 9.81f * radius * Mathf.Sin(fighter.walkAngle);
            torque += -fighter.GetInput() * fighter.fighterProperties.footPushForce * radius;
        }

        spin += (torque / inertia) * dt;
        spin -= damping * spin * dt;

        wheelAngle += spin * dt;
        transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, -wheelAngle * Mathf.Rad2Deg)) * startRotation;
        transform.position = startPosition + new Vector3(wheelAngle * radius, 0f, 0f);
    }
}
