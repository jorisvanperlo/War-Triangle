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

    private float flySpeed;
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

    private float flapLiftModifier;
    private float flapFoldedAngle;
    private bool isFlapsDeployed = false;
    private float currentTargetAngle;

    private float flapDragMul;
    private Quaternion[] flapTargetRotations;
    // Engine Force
    [Foldout("Engine")]
    public float enginePower_Hp = 200f, throttleIncrement = 30f;
    [Foldout("Engine")]
    public float accelerationRate = 2f, decelerationRate = 2f;

    private float thrustForce;
    private float throttle;
    private float currentThrust = 0f;
    private float thrustReduceOverAngle = 1;

    // Aerodynamics
    [Foldout("Aerodynamics")]
    public float dragOverSpeedMod = 0.0005f;
    [Foldout("Aerodynamics")]
    public float liftMultiplier = 100f;
    [Foldout("Aerodynamics")]
    public AnimationCurve dragOverAngle;

    private float drag;
    private float targetDrag;
    private float dragChangeSpeed = 0.1f;
    private float lowSpeedAccelDamp;
    private float lowSpeedAccelDampMod = 0.01f;

    // Proplers
    [Foldout("Propellers")]
    public GameObject[] propHolders, proplers, fakeProplers;
    [Foldout("Propellers")]
    public float propSpinSpeed = 13;
    [Foldout("Propellers")]
    public float propSwapThreshold_Perc = 25f;

    private float currentSpinSpeed;
    private bool propState;
    private bool previousPropState;

    // Landing gear
    [Foldout("Landing Gear")]
    public float gearFoldedAngle, gearDeploySpeed = 0.5f, groundCheckRayLenght = 1f, gearWheelTurnSpeed = 5;
    [Foldout("Landing Gear")]
    public bool hideGearWhenFolded;
    [Foldout("Landing Gear")]
    public GameObject[] landingGear, landingGearWheels;


    private bool isGearDeployed = true;
    private float gearDeployAngle, gearDragMul;
    private Quaternion[] deployedGearRotations;
    private Quaternion[] foldedGearRotations;
    private Quaternion[] gearTargetRotations;

    // UI
    [Foldout("UI")]
    public TextMeshProUGUI throttleInd, AirspeedInd;

    private float prevThrottle, prevSpeed;
    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass_Kg;
        rb.automaticCenterOfMass = false;

        // get aileron local rot
        aileronLStartRot = aileronL.localRotation;
        aileronRStartRot = aileronR.localRotation;

        // get flap angle
        flapTargetRotations = new Quaternion[flaps.Length];

        // get gear angle 
        gearTargetRotations = new Quaternion[landingGear.Length];
        deployedGearRotations = new Quaternion[landingGear.Length];
        foldedGearRotations = new Quaternion[landingGear.Length];

        for (int i = 0; i < landingGear.Length; i++)
        {
            if (landingGear[i] == null) continue;

            deployedGearRotations[i] = Quaternion.Euler(gearDeployAngle,
                landingGear[i].transform.localEulerAngles.y,
                landingGear[i].transform.localEulerAngles.z);

            foldedGearRotations[i] = Quaternion.Euler(gearFoldedAngle,
                landingGear[i].transform.localEulerAngles.y,
                landingGear[i].transform.localEulerAngles.z);
        }

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
            ChangeFlaps();
        }
        // Landing gear input
        if (Input.GetKeyDown(KeyCode.G))
        {
            ChangeGears();
            if (hideGearWhenFolded && !isGearDeployed)
                foreach (GameObject Gear in landingGear)
                {
                    Gear.gameObject.SetActive(true);
                }
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
        isFlapsDeployed = !isFlapsDeployed;
        currentTargetAngle = isFlapsDeployed ? flapDeployAngle : flapFoldedAngle;

        for (int i = 0; i < flaps.Length; i++)
        {
            if (flaps[i] == null) continue;

            Vector3 currentEuler = flaps[i].transform.localEulerAngles;
            Vector3 targetEuler = new Vector3(currentTargetAngle, currentEuler.y, currentEuler.z);
            flapTargetRotations[i] = Quaternion.Euler(targetEuler);
        }
        StartCoroutine(SmoothRotateFlaps());
    }
    private System.Collections.IEnumerator SmoothRotateFlaps()
    {
        while (true)
        {
            bool allComplete = true;

            for (int i = 0; i < flaps.Length; i++)
            {
                if (flaps[i] == null) continue;

                flaps[i].transform.localRotation = Quaternion.RotateTowards(
                    flaps[i].transform.localRotation,
                    flapTargetRotations[i],
                    flapDeploySpeed * Time.deltaTime
                );

                if (Quaternion.Angle(flaps[i].transform.localRotation, flapTargetRotations[i]) > 0.1f)
                {
                    allComplete = false;
                }
            }
            if (allComplete)
            {
                yield break;
            }
            yield return null;
        }
    }
    public void ChangeGears()
    {
        isGearDeployed = !isGearDeployed;

        for (int i = 0; i < landingGear.Length; i++)
        {
            if (landingGear[i] == null) continue;

            gearTargetRotations[i] = isGearDeployed ? deployedGearRotations[i] : foldedGearRotations[i];

            // Re-enable gear in case it's hidden
            if (!landingGear[i].activeSelf)
                landingGear[i].SetActive(true);
        }

        StartCoroutine(SmoothRotateGears());
    }
    private System.Collections.IEnumerator SmoothRotateGears()
    {
        while (true)
        {
            bool allComplete = true;

            for (int i = 0; i < landingGear.Length; i++)
            {
                if (landingGear[i] == null) continue;

                landingGear[i].transform.localRotation = Quaternion.RotateTowards(
                    landingGear[i].transform.localRotation,
                    gearTargetRotations[i],
                    gearDeploySpeed * Time.deltaTime
                );

                if (Quaternion.Angle(landingGear[i].transform.localRotation, gearTargetRotations[i]) > 0.1f)
                {
                    allComplete = false;
                }
            }
            if (allComplete)
            {
                if (hideGearWhenFolded && !isGearDeployed)
                    foreach (GameObject Gear in landingGear)
                    {
                        Gear.gameObject.SetActive(false);
                    }
                yield break;
            }
            yield return null;
        }
    }

    public void FixedUpdate()
    {
        CalculateForce();

        ApplyForces();

        RotatePropellors();

        CheckForGround();
    }

    public void CalculateForce()
    {
        // Get flying speed
        flySpeed = rb.linearVelocity.magnitude;

        // HP to Newtons (temp speed float for this caculation)
        float speed = rb.linearVelocity.magnitude;
        float powerWatts = enginePower_Hp * 745.69f;

        // Avoid divide-by-zero with a small clamp
        speed = Mathf.Max(speed, 0.1f);

        thrustForce = powerWatts / speed;

        lowSpeedAccelDamp = 0.1f + flySpeed * lowSpeedAccelDampMod;
        lowSpeedAccelDamp = Mathf.Clamp01(lowSpeedAccelDamp);

        // Flap lift
        if (isFlapsDeployed)
        flapLiftModifier = deployedFlapsLiftModifier;  
        else
        flapLiftModifier = 1.0f;

        // Claculate Drag
        if (isFlapsDeployed)
            flapDragMul = 3f;
        else flapDragMul = 1;

        if (isGearDeployed)
            gearDragMul = 2f;
        else gearDragMul = 1f;


        // Get how much the object is looking "up" (dot with world up)
        float lookUpAmount = Vector3.Dot(transform.forward.normalized, Vector3.up);
        float thrustReduceLerp = dragOverAngle.Evaluate(lookUpAmount);
        thrustReduceOverAngle = Mathf.Lerp(thrustReduceOverAngle, thrustReduceLerp, 0.2f * Time.deltaTime);

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
        rb.AddForce(transform.up * flySpeed * liftMultiplier * flapLiftModifier);
        rb.AddForce(transform.forward * thrustForce * thrustReduceOverAngle * currentThrust * lowSpeedAccelDamp);
        rb.AddTorque(transform.up * yaw * (flySpeed * 0.5f) * yawResponsiveness * 200f);
        rb.AddTorque(transform.right * pitch * (flySpeed * 0.5f) * pitchResponsiveness * 50f);
        rb.AddTorque(-transform.forward * roll * (flySpeed * 0.5f) * rollResponsiveness * 350f);

        // Calculate Target drag and lerp to drag
        targetDrag = 1.0f + flySpeed * dragOverSpeedMod * flapDragMul * gearDragMul; 
        drag = Mathf.Lerp(drag, targetDrag, dragChangeSpeed * Time.deltaTime);
        rb.linearDamping = drag;
        rb.angularDamping = 1.0f + flySpeed * dragOverSpeedMod;
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
    public void CheckForGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckRayLenght) && isGearDeployed)
        {
            if (hit.collider.tag == "Floor")
            {
                RotateWheels();

            }
        }
    }
    public void RotateWheels()
    {
        foreach(GameObject W in landingGearWheels)
        {
            W.transform.Rotate(Vector3.right * flySpeed * gearWheelTurnSpeed * 10 * Time.deltaTime);
        }
    }

    public void UpdateUI()
    {
        if (Mathf.Abs(prevThrottle - throttle) > 0.1f)
        {
            throttleInd.text = throttle.ToString("F0") + "%";
            prevThrottle = throttle;
        }

        if (Mathf.Abs(prevSpeed - flySpeed) > 0.1f)
        {
            AirspeedInd.text = (flySpeed * 3.6f).ToString("F0") + "KM/H";
            prevSpeed = flySpeed;
        }
    }
}
