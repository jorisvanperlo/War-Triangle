using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The object the camera orbits around

    [Header("Rotation Settings")]
    public float sensitivityX = 4f;
    public float sensitivityY = 2f;
    public float minYAngle = -35f;
    public float maxYAngle = 60f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 10f;

    private float currentZoom = 5f;
    private float yaw = 0f;
    private float pitch = 20f;

    void Start()
    {
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        // Mouse look
        yaw += Input.GetAxis("Mouse X") * sensitivityX;
        pitch -= Input.GetAxis("Mouse Y") * sensitivityY;
        pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);

        // Zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentZoom -= scroll * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        // Calculate position and rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -currentZoom);
        transform.position = target.position + offset;
        transform.LookAt(target);
    }
}
