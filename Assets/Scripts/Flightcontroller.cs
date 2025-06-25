using UnityEngine;
using TMPro;
using NaughtyAttributes;

public class Flightcontroller : MonoBehaviour
{
    // General info and input
    [Foldout("GeneralStats")]
    public float mass_Kg;
    [Foldout("GeneralStats")]
    public float liftMultiplier = 100f;

    private Rigidbody rb;

    // Controll Surfaces
    [Foldout("Control Surfaces")]
    public GameObject[] elevators, rudder;
    [Foldout("Control Surfaces")]
    public GameObject aileronL, aileronR;
    [Foldout("Control Surfaces")]
    public float aileronMaxRot, elevatorMaxRot, rudderMaxRot, controlSurfRotSpeed;
    [Foldout("Control Surfaces")]
    public float rollResponsiveness, pitchResponsiveness, yawResponsiveness;

    private float currentAileronAngle, currentElevatorAngle, currentRudderAngle;
    private float roll, pitch, yaw;

    // Flaps
    [Foldout("Flaps")]
    public GameObject[] flaps;
    [Foldout("Flaps")]
    public float flapDeploySpeed;
    [Foldout("Flaps")]
    public Vector3 flapDeployAngle, flapFoldedAngle;

    // Engine Force
    [Foldout("Engine")]
    public float accelerationRate = 2f, decelerationRate = 2f;
    [Foldout("Engine")]
    public float throttleIncrement = 30f, enginePower_Hp = 200f;

    private float thrustForce;
    private float throttle;
    private float currentThrust = 0f;

    // Aerodynamics
    [Foldout("Drag")]
    public float dragOverSpeedMod = 0.0005f;

    private float drag;
    private float lowSpeedAccelDamp;
    private float lowSpeedAccelDampMod = 0.01f;

    // Proplers
    [Foldout("Propelor")]
    public GameObject[] propHolders, proplers, fakeProplers;
    [Foldout("Propelor")]
    public float propSpinSpeed = 13;
    [Foldout("Propelor")]
    public float propSwapThreshold_Perc = 20f;

    private float currentSpinSpeed = 0f;
    private bool propState;
    private bool previousPropState;

    // UI
    [Foldout("UI")]
    public TextMeshProUGUI throttleInd, AirspeedInd;
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
        if (throttle >= propSwapThreshold_Perc)
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
