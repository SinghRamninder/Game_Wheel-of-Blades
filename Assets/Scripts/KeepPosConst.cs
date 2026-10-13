using UnityEngine;

public class KeepPosConst : MonoBehaviour
{
    private Vector3 initialPosition;
    private Vector3 initialRotation;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation.eulerAngles;
    }

    private void Update()
    {
        transform.localPosition = initialPosition;
        transform.localRotation = Quaternion.Euler(initialRotation);
    }
}
