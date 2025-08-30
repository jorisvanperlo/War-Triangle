using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuCamController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float mouseSensitivity = 3f;
    public float pitchMin = -40f;
    public float pitchMax = 80f;
    public float moveSpeed = 5f;

    private float yaw = 0f;
    private float pitch = 0f;

    public bool canLookAround, camMove;

    public int targetPosInt;
    public Vector3 targetPos;
    public Quaternion targetRot;

    

    public Transform[] camMovePos;

    public TextMeshProUGUI planeName;

    public void Awake()
    {
        //Setresolution
        Screen.SetResolution(398, 224, true);
    }

    void Update()
    {
        if (canLookAround && !camMove && Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        if (camMove)
        {
            planeName.text = camMovePos[targetPosInt].tag;

            targetPos = camMovePos[targetPosInt].position;
            targetRot = camMovePos[targetPosInt].rotation;

            transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, moveSpeed * Time.deltaTime);

            float posThreshold = 0.03f;
            float rotThreshold = 1f;

            if (Vector3.Distance(transform.position, targetPos) < posThreshold && Quaternion.Angle(transform.rotation, targetRot) < rotThreshold)
            {
                transform.position = targetPos;
                transform.rotation = targetRot;
                camMove = false;

                // Sync look angles to prevent jump
                Vector3 angles = transform.eulerAngles;
                yaw = angles.y;
                pitch = angles.x;
            }
        }
    }

    public void FirstPlanePos()
    {
        targetPosInt = 1;
        camMove = true;
    }

    public void MenuPos()
    {
        targetPosInt = 0;
        camMove = true;
    }

    public void NextPlane()
    {
        targetPosInt++;
        if (targetPosInt >= camMovePos.Length)
        {
            targetPosInt = 1;
        }
        camMove = true;
    }

    public void PrevPlane()
    {
        targetPosInt--;
        if (targetPosInt <= 0)
        {
            targetPosInt = camMovePos.Length - 1;
        }
        camMove = true;
    }
}
