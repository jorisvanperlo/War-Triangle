using UnityEngine;
using TMPro;

public class Flightcontroller : MonoBehaviour
{
    // General info and input

    public float throttleIncrement = 0.1f;
    public float maxThrust = 200f;
    public float responsiveness = 10f;
    public float liftMultiplier = 1.0f;

    private float throttle;
    private float currentThrust = 0f;
    public float accelerationRate = 1f;
    public float decelerationRate = 1f;

    private float roll;
    private float pitch;
    private float yaw;

    public float mass;
    private Rigidbody rb;

    // Wing lift
    private bool canGenLift;
    public float AOAStallAngle;

    // Proplers
    private bool propState;
    private bool propSwap;
    public GameObject[] propHolders;
    public GameObject[] proplers;
    public GameObject[] fakeProplers;
    private bool previousPropState;

    public float maxRPM;
    private float currentRPM = 0f;
    public float fakePropThreshold = 0.2f;

    // UI
    public TextMeshProUGUI throttleInd;
    public TextMeshProUGUI AirspeedInd;


    public float responseModifier
    {
        get
        {
            rb.mass = mass;
            return (rb.mass / 10f * responsiveness);
        }
    }
    public void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    public void Update()
    {
        HandleInputs();
    }
    public void HandleInputs()
    {
        roll = Input.GetAxis("Roll");
        pitch = Input.GetAxis("Pitch");
        yaw = Input.GetAxis("Yaw");

        if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle += throttleIncrement * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            throttle -= throttleIncrement * Time.deltaTime;
        }
        throttle = Mathf.Clamp(throttle, 0f, 100f);

        // UI
        throttleInd.text = throttle.ToString("F0") + "%" ;
        AirspeedInd.text = (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + "KM/H";
    }
    public void FixedUpdate()
    {
        // Check AOA
        Quaternion minQ = Quaternion.Euler(AOAStallAngle, 0, 0);
        Quaternion maxQ = Quaternion.Euler(-AOAStallAngle, 0, 0);
        Quaternion currentQ = transform.localRotation;
        float minToCurrent = Quaternion.Angle(minQ, currentQ);
        float currentToMax = Quaternion.Angle(currentQ, maxQ);
        float minToMax = Quaternion.Angle(minQ, maxQ);
        canGenLift = minToCurrent + currentToMax <= minToMax + 0.01f;
        if (canGenLift)
        {
            rb.AddForce(transform.up * rb.linearVelocity.magnitude * liftMultiplier);
        }

        // Add input force
        float targetThrust = maxThrust * throttle;
        float rate = (targetThrust > currentThrust) ? accelerationRate * 1000f : decelerationRate * 1000f;
        currentThrust = Mathf.MoveTowards(currentThrust, targetThrust, rate * Time.fixedDeltaTime);

        rb.AddForce(transform.forward * currentThrust);
        rb.AddTorque(transform.up * yaw * responseModifier);
        rb.AddTorque(transform.right * pitch * responseModifier);
        rb.AddTorque(-transform.forward * roll * responseModifier);

        // Proplers 
        if (throttle >= 100f * fakePropThreshold)
        {
            propState = true;
        }
        else
        {
            propState = false;
        }

        if (propState != previousPropState)
        {
            foreach (GameObject obj in fakeProplers)
            {
                obj.SetActive(propState);
            }
            foreach (GameObject obj in proplers)
            {
                obj.SetActive(!propState);
            }
            previousPropState = propState;
        }

        currentRPM = Mathf.Lerp(currentRPM, throttle * maxRPM, Time.deltaTime * 10f);
        float rotationPerFrame = (currentRPM / 60f) * 360f * Time.deltaTime;

        foreach (GameObject obj in propHolders)
        {
            obj.transform.Rotate(Vector3.forward, rotationPerFrame);
        }
    }
}
