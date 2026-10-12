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

    private void Start()
    {
        startRotation = transform.rotation;
        radius = transform.lossyScale.x / 2f;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        float torque = 0f;
        foreach (Fighters fighter in fighters)
        {
            torque += fighter.mass * 9.81f * radius * Mathf.Sin(fighter.walkAngle);
        }

        spin += (torque / inertia) * dt;
        spin -= damping * spin * dt;

        wheelAngle += spin * dt;
        transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, -wheelAngle * Mathf.Rad2Deg)) * startRotation;
    }
}
