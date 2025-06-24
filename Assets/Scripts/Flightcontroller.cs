using UnityEngine;
using TMPro;
using UnityEngine.Windows;

public class Flightcontroller : MonoBehaviour
{
    // General info and input
    private Rigidbody rb;
    public float mass_Kg;
    public float throttleIncrement = 0.1f;
    public float enginePower_Hp = 200f;
    public float rollResponsiveness, pitchResponsiveness, yawResponsiveness;
    public float liftMultiplier = 1.0f;
    private float roll, pitch, yaw;

    // Controll Surfaces
    public GameObject[] elevators, rudder;
    public GameObject aileronL, aileronR;
    public float controlSurfRotSpeed;
    public float aileronMaxRot, elevatorMaxRot, rudderMaxRot;
    private float currentAileronAngle, currentElevatorAngle, currentRudderAngle;

    // Flaps
    public GameObject[] flaps;
    public float flapDeploySpeed;
    public Vector3 flapDeployAngle, flapFoldedAngle;

    // Engine Force
    private float throttle;
    private float currentThrust = 0f;
    public float accelerationRate = 1f;
    public float decelerationRate = 1f; 

    private float thrustForce;

    // Aerodynamics
    public float dragOverSpeedMod = 0.0005f;
    private float drag;
    private float lowSpeedAccelDamp;
    private float lowSpeedAccelDampMod = 0.01f;


    // Proplers
    private bool propState;
    public GameObject[] propHolders;
    public GameObject[] proplers;
    public GameObject[] fakeProplers;
    private bool previousPropState;

    public float propSpinSpeed = 13;
    private float currentSpinSpeed = 0f;
    public float fakePropThreshold = 0.2f;

    // UI
    public TextMeshProUGUI throttleInd;
    public TextMeshProUGUI AirspeedInd;
    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass_Kg;
        rb.automaticCenterOfMass = false;
    }
    public void Update()
    {
        HandleInputs();
        RotateControlSurfaces();
        UpdateUI();
    }
    public void HandleInputs()
    {
        // WASDQE input
        roll = UnityEngine.Input.GetAxis("Roll");
        pitch = UnityEngine.Input.GetAxis("Pitch");
        yaw = UnityEngine.Input.GetAxis("Yaw");

        // Throttle input
        if (UnityEngine.Input.GetKey(KeyCode.LeftShift))
        {
            throttle += throttleIncrement * Time.deltaTime;
        }
        if (UnityEngine.Input.GetKey(KeyCode.LeftControl))
        {
            throttle -= throttleIncrement * Time.deltaTime;
        }
        throttle = Mathf.Clamp(throttle, 0f, 100f);

        // Flap input
        if (UnityEngine.Input.GetKeyDown(KeyCode.F))
        {
            ChangeFlaps();
        }
    }
    public void RotateControlSurfaces()
    {
        float aileronDelta = roll * controlSurfRotSpeed * Time.deltaTime;

        currentAileronAngle = Mathf.Clamp(currentAileronAngle + aileronDelta, -aileronMaxRot, aileronMaxRot);

        Vector3 changeAngles = aileronR.transform.localEulerAngles;
        changeAngles.x = -currentAileronAngle;
        aileronR.transform.localEulerAngles = changeAngles;
        aileronL.transform.localEulerAngles = -changeAngles;
    }
    public void ChangeFlaps()
    {

    }
    public void FixedUpdate()
    {
        CalculateForce();

        ApplyForces();

        RotatePropellors();
    }

    public void CalculateForce()
    {
        // HP to Newtons
        float speed = rb.linearVelocity.magnitude;
        float powerWatts = enginePower_Hp * 745.69f;

        // Avoid divide-by-zero with a small clamp
        speed = Mathf.Max(speed, 0.1f);

        thrustForce = powerWatts / speed;

        lowSpeedAccelDamp = 0.2f + rb.linearVelocity.magnitude * lowSpeedAccelDampMod;
        lowSpeedAccelDamp = Mathf.Clamp01(lowSpeedAccelDamp);
    }
    public void ApplyForces()
    {
        // Float that converts Thrust to Thrust with Accel and Decel
        if (throttle > currentThrust)
        {
            currentThrust += accelerationRate * Time.deltaTime;
            if (currentThrust > throttle) currentThrust = throttle; 
        }
        else if (throttle < currentThrust)
        {
            currentThrust -= decelerationRate * Time.deltaTime;
            if (currentThrust < throttle) currentThrust = throttle; 
        }

        // Apply forces
        rb.AddForce(transform.up * rb.linearVelocity.magnitude * liftMultiplier);
        rb.AddForce(transform.forward * thrustForce * currentThrust * lowSpeedAccelDamp);
        rb.AddTorque(transform.up * yaw * yawResponsiveness * 10000f);
        rb.AddTorque(transform.right * pitch * pitchResponsiveness * 10000f);
        rb.AddTorque(-transform.forward * roll * rollResponsiveness * 10000f);

        // Simulate realistic drag by dynamicly increasing drag over speed
        drag = 1.0f + rb.linearVelocity.magnitude * dragOverSpeedMod;
        rb.linearDamping = drag;
        rb.angularDamping = drag;
    }
    public void RotatePropellors()
    {
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

        currentSpinSpeed = Mathf.Lerp(currentSpinSpeed, throttle * propSpinSpeed, Time.deltaTime * 10f);
        float rotationPerFrame = (currentSpinSpeed / 60f) * 360f * Time.deltaTime;

        foreach (GameObject obj in propHolders)
        {
            obj.transform.Rotate(Vector3.forward, rotationPerFrame);
        }
    }

    public void UpdateUI()
    {
        throttleInd.text = throttle.ToString("F0") + "%";

        AirspeedInd.text = (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + "KM/H";
    }
}
