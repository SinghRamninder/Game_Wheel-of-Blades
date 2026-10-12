using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WheelController : MonoBehaviour
{
    [SerializeField] private Fighters[] fighters;
    private Rigidbody rb;
    public float radius;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        radius = transform.lossyScale.x / 2f;
    }

    private void FixedUpdate()
    {
        float torque = 0f;

        foreach (Fighters fighter in fighters)
        {
            float fighterTorque = fighter.mass * 9.81f * radius * Mathf.Sin(fighter.walkAngle);
            torque += fighterTorque;
        }

        rb.AddTorque(new Vector3(0f, 0f, torque), ForceMode.Force);
    }
}
