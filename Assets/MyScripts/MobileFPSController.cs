using UnityEngine;

public class MobileFPSController : MonoBehaviour
{
    public FixedJoystick joystick;
    public float moveSpeed = 5f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        float horizontal = joystick.Horizontal;
        float vertical = joystick.Vertical;

        Vector3 moveDirection =
            transform.forward * vertical +
            transform.right * horizontal;

        rb.MovePosition(
            rb.position +
            moveDirection.normalized * moveSpeed * Time.fixedDeltaTime
        );
    }
}