using UnityEngine;

public class Fighters : MonoBehaviour
{
    [SerializeField] private WheelController wheelController;

    public float mass = 2f;
    [HideInInspector] public float angleFromCenter = 0f;

    private void Update()
    {
        Vector3 relativePosition = transform.position - wheelController.transform.position;
        angleFromCenter = Mathf.Atan2(relativePosition.y, relativePosition.x);
    }
}
