using UnityEngine;

public class MobileLookController : MonoBehaviour
{
    public FixedTouchField touchField;
    public Transform playerBody;
    public float sensitivity = 0.15f;
    public float maxDeltaPerFrame = 80f;   // safety clamp against any stray spike

    private float xRotation;

    private void Update()
    {
        // Read and consume the delta so it can never be reused on a later frame
        Vector2 d = Vector2.ClampMagnitude(touchField.TouchDist, maxDeltaPerFrame);
        touchField.TouchDist = Vector2.zero;

        float touchX = d.x * sensitivity;
        float touchY = d.y * sensitivity;

        xRotation -= touchY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * touchX);
    }
}