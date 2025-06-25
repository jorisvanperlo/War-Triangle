using UnityEngine;
using TMPro;
using NaughtyAttributes;
using System.Linq;
using System.Collections.Generic;

public class Flightcontroller : MonoBehaviour
{
    // General info and input
    [Foldout("General Stats")]
    public float mass_Kg;

    private Rigidbody rb;

    // Controll Surfaces
    [Foldout("Control Surfaces")]
    public List<Transform> elevators_LeftFirst = new(), rudders_LeftFirst = new();
    [Foldout("Control Surfaces")]
    public Transform aileronL, aileronR;
    [Foldout("Control Surfaces")]
    public float aileronMaxRot, elevatorMaxRot, rudderMaxRot, controlSurfRotSpeed;
    [Foldout("Control Surfaces")]
    public float rollResponsiveness, pitchResponsiveness, yawResponsiveness;

    private float currentAileronAngle, currentElevatorAngle, currentRudderAngle;
    private float roll, pitch, yaw;
    private float controlSurfLerpSpeed = 5f;

    private Quaternion aileronLStartRot;
    private Quaternion aileronRStartRot;
    private Quaternion elevatorLStartRot;
    private Quaternion elevatorRStartRot;
    private Quaternion rudderLStartRot;
    private Quaternion rudderRStartRot;

    // Flaps
    [Foldout("Flaps")]
    public GameObject[] flaps;
    [Foldout("Flaps")]
    public float flapDeploySpeed, flapDeployAngle;
    [Foldout("Flaps")]
    public float deployedFlapsLiftModifier = 1.1f;

    private float flapLiftModefier;
    private float flapFoldedAngle;
    private bool isFlapsDeployed = false, isFlapTransitionComplete = false;
    private float currentTargetAngle;

    // Engine Force
    [Foldout("Engine")]
    public float enginePower_Hp = 200f, throttleIncrement = 30f;
    [Foldout("Engine")]
    public float accelerationRate = 2f, decelerationRate = 2f;

    private float thrustForce;
    private float throttle;
    private float currentThrust = 0f;

    // Aerodynamics
    [Foldout("Aerodynamics")]
    public float dragOverSpeedMod = 0.0005f;
    [Foldout("Aerodynamics")]
    public float liftMultiplier = 100f;

    private float drag;
    private float lowSpeedAccelDamp;
    private float lowSpeedAccelDampMod = 0.01f;

    // Proplers
    [Foldout("Propellers")]
    public GameObject[] propHolders, proplers, fakeProplers;
    [Foldout("Propellers")]
    public float propSpinSpeed = 13;
    [Foldout("Propellers")]
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

        // Flap start angle
        currentTargetAngle = flapFoldedAngle;

        // get aileron local rot
        aileronLStartRot = aileronL.localRotation;
        aileronRStartRot = aileronR.localRotation;
        
        // get elevator local rot
        elevatorLStartRot = elevators_LeftFirst[0].localRotation;
        if (elevators_LeftFirst.Count > 1)
            elevatorRStartRot = elevators_LeftFirst[1].localRotation;

        // get rudder local rot
        rudderLStartRot = rudders_LeftFirst[0].localRotation;
        if (rudders_LeftFirst.Count > 1)
            rudderRStartRot = rudders_LeftFirst[1].localRotation;
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
        roll = Input.GetAxis("Roll");
        pitch = Input.GetAxis("Pitch");
        yaw = Input.GetAxis("Yaw");

        // Throttle input
        if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle += throttleIncrement * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            throttle -= throttleIncrement * Time.deltaTime;
        }
        throttle = Mathf.Clamp(throttle, 0f, 100f);

        // Flap input
        if (Input.GetKeyDown(KeyCode.F))
        {
            isFlapsDeployed = !isFlapsDeployed;
            currentTargetAngle = isFlapsDeployed ? flapDeployAngle : flapFoldedAngle;
            isFlapTransitionComplete = false;
        }
        if (!isFlapTransitionComplete)
        {
            ChangeFlaps();
        }
    }
    public void RotateControlSurfaces()
    {
        // Aileron rotation
        float ailTarget = roll * aileronMaxRot;
        // Pick smooth speed depending on direction
        float ailRotSpeed = Mathf.Abs(ailTarget) > Mathf.Abs(currentAileronAngle)
            ? controlSurfRotSpeed
            : controlSurfLerpSpeed;

        currentAileronAngle = Mathf.Lerp(currentAileronAngle, ailTarget, Time.deltaTime * ailRotSpeed);

        // Apply local X rotation relative to rest rotation
        Quaternion aileronLRot = Quaternion.AngleAxis(-currentAileronAngle, Vector3.right);
        Quaternion aileronRRot = Quaternion.AngleAxis(currentAileronAngle, Vector3.right);

        aileronL.localRotation = aileronLStartRot * aileronLRot;
        aileronR.localRotation = aileronRStartRot * aileronRRot;


        // Elevator rotation
        float elevTarget = pitch * elevatorMaxRot;
        // Pick smooth speed depending on direction
        float elevatorRotSpeed = Mathf.Abs(elevTarget) > Mathf.Abs(currentElevatorAngle)
            ? controlSurfRotSpeed
            : controlSurfLerpSpeed;

        currentElevatorAngle = Mathf.Lerp(currentElevatorAngle, elevTarget, Time.deltaTime * elevatorRotSpeed);

        // Apply local X rotation relative to rest rotation
        Quaternion elevatorLRot = Quaternion.AngleAxis(-currentElevatorAngle, Vector3.right);
        Quaternion elevatorRRot = Quaternion.AngleAxis(-currentElevatorAngle, Vector3.right);

        elevators_LeftFirst[0].localRotation = elevatorLStartRot * elevatorLRot;
        // check if there is a second elevator
        if (elevators_LeftFirst.Count > 1)
            elevators_LeftFirst[1].localRotation = elevatorRStartRot * elevatorRRot;


        // Rudder rotation
        float rudTarget = yaw * rudderMaxRot;
        // Pick smooth speed depending on direction
        float rudderRotSpeed = Mathf.Abs(rudTarget) > Mathf.Abs(currentRudderAngle)
            ? controlSurfRotSpeed
            : controlSurfLerpSpeed;

        currentRudderAngle = Mathf.Lerp(currentRudderAngle, rudTarget, Time.deltaTime * rudderRotSpeed);

        // Apply local X rotation relative to rest rotation
        Quaternion rudderLRot = Quaternion.AngleAxis(-currentRudderAngle, Vector3.up);
        Quaternion rudderRRot = Quaternion.AngleAxis(-currentRudderAngle, Vector3.up);

        rudders_LeftFirst[0].localRotation = rudderLStartRot * rudderLRot;
        // check if there is a second elevator
        if (rudders_LeftFirst.Count > 1)
            rudders_LeftFirst[1].localRotation = rudderRStartRot * rudderRRot;
    }

    public void ChangeFlaps()
    {
        bool allReachedTarget = true;

        foreach (GameObject flap in flaps)
        {
            Vector3 currentEuler = flap.transform.localEulerAngles;
            float currentX = NormalizeAngle(currentEuler.x);
            float newX = Mathf.LerpAngle(currentX, currentTargetAngle, Time.deltaTime * flapDeploySpeed);

            flap.transform.localEulerAngles = new Vector3(newX, currentEuler.y, currentEuler.z);

            if (Mathf.Abs(NormalizeAngle(newX) - currentTargetAngle) > 0.5f)
                allReachedTarget = false;
        }

        if (allReachedTarget)
            isFlapTransitionComplete = true;

    }
    float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
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

        lowSpeedAccelDamp = 0.1f + rb.linearVelocity.magnitude * lowSpeedAccelDampMod;
        lowSpeedAccelDamp = Mathf.Clamp01(lowSpeedAccelDamp);

        // Flap lift
        if (isFlapsDeployed)
        flapLiftModefier = deployedFlapsLiftModifier;  
        else
        flapLiftModefier = 1.0f;
        
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
        rb.AddForce(transform.up * rb.linearVelocity.magnitude * liftMultiplier * flapLiftModefier);
        rb.AddForce(transform.forward * thrustForce * currentThrust * lowSpeedAccelDamp);
        rb.AddTorque(transform.up * yaw * yawResponsiveness * 10000f);
        rb.AddTorque(transform.right * pitch * pitchResponsiveness * 10000f);
        rb.AddTorque(-transform.forward * roll * rollResponsiveness * 10000f);

        // Simulate realistic drag by dynamicly increasing drag over speed
        float flapDrag = 1;
        if (isFlapsDeployed)
            flapDrag = 1.3f;

        drag = 1.0f + rb.linearVelocity.magnitude * dragOverSpeedMod * flapDrag;
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
