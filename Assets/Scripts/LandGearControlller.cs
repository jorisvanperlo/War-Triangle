using UnityEngine;

public class LandGearControlller : MonoBehaviour
{
    [Header("Landing Gear Wheels")]
    public Transform[] landingGearWheels;

    [Header("Raycast Settings")]
    public Vector3 rayOriginOffset = Vector3.zero;
    public float raycastDistance = 2f;

    [Header("Spin Settings")]
    public float spinMultiplier = 100f;

    private Rigidbody rb;

    //landGear
    public GameObject[] landGear;
    public bool gearDeployed = true;
    public bool changeDeploy;
    private float t;
    public float deploySpeed;

    private Vector3 startAngle;
    private Vector3 endAngle;    
    private Vector3 deployAngle;
    public Vector3 foldAngle;
    public float gearDragMulitplier;

    public void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        //WheelsRotate
        if (rb == null || landingGearWheels == null || landingGearWheels.Length == 0) return;

        Vector3 origin = transform.position + rayOriginOffset;
        Vector3 direction = Vector3.down;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, raycastDistance))
        {
            if (hit.collider.CompareTag("Floor"))
            {
                float speed = rb.linearVelocity.magnitude;

                foreach (Transform wheel in landingGearWheels)
                {
                    if (wheel != null)
                    {
                        float spinSpeed = speed * spinMultiplier * Time.deltaTime;
                        wheel.Rotate(Vector3.right, spinSpeed, Space.Self);
                    }
                }
            }
        }

        //Landgear
        if (Input.GetKeyDown(KeyCode.G))
        {
            foreach(GameObject LG in landGear)
            {
                //Fold gear
                if (!gearDeployed)
                {
                    startAngle = deployAngle;
                    endAngle = foldAngle;

                    rb.linearDamping /= gearDragMulitplier;
                }
                //opens gear
                else
                {
                    startAngle = foldAngle;
                    endAngle = deployAngle;
                    //Set gear active for deploy
                    foreach (GameObject LGForDeploy in landGear)
                    {
                        LG.SetActive(true);
                    }

                    rb.linearDamping *= gearDragMulitplier;
}
                gearDeployed = !gearDeployed;
                changeDeploy = true;
                t = 0f;
            }
        }

        if (changeDeploy)
        {
            t += Time.deltaTime * deploySpeed;
            Quaternion from = Quaternion.Euler(startAngle);
            Quaternion to = Quaternion.Euler(endAngle);

            //rotates gears between the points
            foreach (GameObject LG in landGear)
            {
                LG.transform.localRotation = Quaternion.Lerp(from, to, t);
            }
            //turn gear off if folded in
            if (t >= 1f)
            {
                changeDeploy = false;
                foreach (GameObject LG in landGear)
                {
                    LG.SetActive(!gearDeployed);
                }

            }
        }
    }
}
