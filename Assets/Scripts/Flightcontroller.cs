using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEngine.UI.Image;

public class Flightcontroller : MonoBehaviour
{
    // General info and input
    [Foldout("General Stats")]
    public float mass_Kg;

    private float flySpeed;
    private Rigidbody rb;
    

    // Controll Surfaces
    [Foldout("Control Surfaces")]
    public float aileronMaxRot = 35f, elevatorMaxRot = 35f, rudderMaxRot = 15f, controlSurfRotSpeed = 10f;
    [Foldout("Control Surfaces")]
    public float rollResponsiveness = 6f, pitchResponsiveness = 6f, yawResponsiveness = 6f;
    [Foldout("Control Surfaces")]
    public List<Transform> elevators_LeftFirst = new(), rudders_LeftFirst = new();
    [Foldout("Control Surfaces")]
    public Transform aileronL, aileronR;

    private float currentAileronAngle, currentElevatorAngle, currentRudderAngle;
    private float roll, pitch, yaw;
    private float controlSurfLerpSpeed = 5f;
    private float yawGroundMul = 1f;

    private Quaternion aileronLStartRot, aileronRStartRot;
    private Quaternion elevatorLStartRot, elevatorRStartRot;
    private Quaternion rudderLStartRot, rudderRStartRot;

    // Flaps
    [Foldout("Flaps")]
    public float flapDeploySpeed = 12f, flapDeployAngle = -40f;
    [Foldout("Flaps")]
    public float deployedFlapsLiftModifier = 2.5f;
    [Foldout("Flaps")]
    public GameObject[] flaps;

    private float flapLiftModifier, flapFoldedAngle, currentTargetAngle;
    private bool isFlapsDeployed = false;

    private float flapDragMul;
    private Quaternion[] flapTargetRotations;

    // Engine Force
    [Foldout("Engine")]
    public float enginePower_Hp, throttleIncrement = 30f;
    [Foldout("Engine")]
    public float accelerationRate = 2f, decelerationRate = 3f;

    private float thrustForce, throttle, currentThrust, thrustReduceOverAngle = 1, glideMultiplier;

    // Aerodynamics
    [Foldout("Aerodynamics")]
    public float dragOverSpeedMod = 0.005f;
    [Foldout("Aerodynamics")]
    public float liftMultiplier = 500f;

    private float drag, targetDrag, dragChangeSpeed = 0.1f, lowSpeedAccelDamp, lowSpeedAccelDampMod = 0.01f;
    private float lookUpAmount;

    // Proplers
    [Foldout("Propellers")]
    public float propSpinSpeed = 13;
    [Foldout("Propellers")]
    public float propSwapThreshold_Perc = 25f;
    [Foldout("Propellers")]
    public GameObject[] propHolders, proplers, fakeProplers;

    private float currentSpinSpeed;
    private bool propState, previousPropState;

    // Landing gear
    [Foldout("Landing Gear")]
    public float gearFoldedAngle = 90f, gearDeploySpeed = 30f, groundCheckRayLength = 2f, gearWheelTurnSpeed = 10;
    [Foldout("Landing Gear")]
    public bool hideGearWhenFolded, gearAlsoRotatesInYAxis;
    [Foldout("Landing Gear")]
    public GameObject[] landingGear, landingGearWheels;

    private bool isGearDeployed = true, gearTouchGround, isBraking;
    private float gearDeployAngle, gearDragMul;
    private Quaternion[] deployedGearRotations, foldedGearRotations, gearTargetRotations;

    // UI
    [Foldout("UI")]
    public TMP_Text throttleInd, airspeedInd, altitudeInd ,radarAltitudeInd , climbAngleInd;

    private float prevThrottle, prevSpeed, prevAlt, prevClimbAngle;

    // Animation Graphs
    [Foldout("Don't Change")]
    public AnimationCurve dragOverAngle, thrustOverThrottle, glideOverAngle;
    [Foldout("Don't Change")]
    public PhysicsMaterial phyMat;
    [Foldout("Don't Change")]
    public Camera cam;


    public void Start()
    {
        //getting UI elements
        throttleInd = GameObject.Find("Throttle").GetComponent<TMP_Text>();
        airspeedInd = GameObject.Find("Airspeed").GetComponent<TMP_Text>();
        altitudeInd = GameObject.Find("Altitude").GetComponent<TMP_Text>();
        radarAltitudeInd = GameObject.Find("Radar Altitude").GetComponent<TMP_Text>();
        climbAngleInd = GameObject.Find("ClimbAngle").GetComponent<TMP_Text>();

        //getting in game components
        cam = Camera.main;
        rb = GetComponent<Rigidbody>();
        rb.mass = mass_Kg;
        rb.automaticCenterOfMass = false;

        //set camera to this plane
        cam.GetComponent<CameraController>().target = transform;

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

        // set wheel friction
        phyMat.dynamicFriction = 0.6f;
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
        // Brake input
        if (Input.GetKeyDown(KeyCode.B))
        {
            phyMat.dynamicFriction = 0.8f;
            isBraking = true;
        }
        if (Input.GetKeyUp(KeyCode.B))
        {
            phyMat.dynamicFriction = 0.6f;
            isBraking = false;
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

                Quaternion targetRotation = gearTargetRotations[i];

                if (gearAlsoRotatesInYAxis)
                {
                    // Add or remove 90 degrees on Y axis based on deploy state
                    Quaternion yOffset = Quaternion.Euler(0f, isGearDeployed ? 0f : 90f, 0f);
                    targetRotation *= yOffset;
                }

                landingGear[i].transform.localRotation = Quaternion.RotateTowards(
                    landingGear[i].transform.localRotation,
                    targetRotation,
                    gearDeploySpeed * Time.deltaTime
                );

                if (Quaternion.Angle(landingGear[i].transform.localRotation, targetRotation) > 0.1f)
                {
                    allComplete = false;
                }
            }

            if (allComplete)
            {
                if (hideGearWhenFolded && !isGearDeployed)
                {
                    foreach (GameObject Gear in landingGear)
                    {
                        Gear.gameObject.SetActive(false);
                    }
                }
                yield break;
            }

            yield return null;
        }
    }

    public void FixedUpdate()
    {
        CalculateForces();

        ApplyForces();

        RotatePropellors();

        CheckForGround();
    }

    public void CalculateForces()
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
            flapDragMul = 4f;
        else flapDragMul = 1;

        if (isGearDeployed)
            gearDragMul = 3f;
        else gearDragMul = 1f;


        // Get how much the object is looking "up" (dot with world up)
        lookUpAmount = Vector3.Dot(transform.forward.normalized, Vector3.up);
        float thrustReduceLerp = dragOverAngle.Evaluate(lookUpAmount);
        thrustReduceOverAngle = Mathf.Lerp(thrustReduceOverAngle, thrustReduceLerp, 0.15f * Time.deltaTime);


        // Calculate glide force
        float glideaLerp = glideOverAngle.Evaluate(lookUpAmount);
        glideMultiplier = Mathf.Lerp(glideMultiplier, glideaLerp, 0.1f * Time.deltaTime);
    }
    public void ApplyForces()
    {
        // Float that converts Thrust to Thrust with Accel and Decel
        float curvedThrottle = thrustOverThrottle.Evaluate(throttle);
        if (curvedThrottle > currentThrust)
        {
            currentThrust += accelerationRate * Time.deltaTime;
            if (currentThrust > curvedThrottle) currentThrust = curvedThrottle; 
        }
        else if (curvedThrottle < currentThrust)
        {
            currentThrust -= decelerationRate * Time.deltaTime;
            if (currentThrust < curvedThrottle) currentThrust = curvedThrottle; 
        }

        // Apply forces
        rb.AddForce(transform.forward * thrustForce * thrustReduceOverAngle * currentThrust * lowSpeedAccelDamp);

        rb.AddTorque(transform.up * yaw * (flySpeed * 0.5f) * yawResponsiveness * 200f * yawGroundMul);
        rb.AddTorque(transform.right * pitch * (flySpeed * 0.5f) * pitchResponsiveness * 50f);
        rb.AddTorque(-transform.forward * roll * (flySpeed * 0.5f) * rollResponsiveness * 350f);

        // Apply lift
        rb.AddForce(transform.up * flySpeed * liftMultiplier * flapLiftModifier);

        // Apply glide force
        rb.AddForce(transform.forward * glideMultiplier);


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
        int playerLayer = LayerMask.NameToLayer("Player");
        int ignorePlayerMask = ~(1 << playerLayer);
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckRayLength, ignorePlayerMask) && isGearDeployed)
        {
            if (hit.collider.tag == "Floor")
            {
                RotateWheels();
                if (!gearTouchGround)
                {
                    gearTouchGround = true;
                }
            }
            else
            {
                if (gearTouchGround)
                {
                    gearTouchGround = false;
                }
            }
        }
        else
        {
            if (gearTouchGround)
            {
                gearTouchGround = false;
            }
        }

        //increase ground yaw strenght if raycast hit th ground
        if (gearTouchGround)
        {
            yawGroundMul = 6f;
            
        }
        else
        {
            if (yawGroundMul > 1.1f)
            {
                yawGroundMul = 1f;
            }
        }
    }
    public void RotateWheels()
    {
        if (!isBraking)
        foreach(GameObject W in landingGearWheels)
        {
            W.transform.Rotate(Vector3.right * flySpeed * gearWheelTurnSpeed * 10f * Time.deltaTime);
        }
    }

    public void UpdateUI()
    {
        if (Mathf.Abs(prevThrottle - throttle) > 0.1f)
        {
            throttleInd.text = ("THR ") + throttle.ToString("F0") + "%";
            prevThrottle = throttle;
        }

        if (Mathf.Abs(prevSpeed - flySpeed) > 0.3f)
        {
            airspeedInd.text = ("SPD ") + (flySpeed * 3.6f).ToString("F0") + "KM/H";
            prevSpeed = flySpeed;
        }

        if (Mathf.Abs(prevAlt - transform.position.y) > 0.3f)
        {
            altitudeInd.text = ("ALT ") + (transform.position.y).ToString("F0") + "M";
            prevAlt = transform.position.y;

            // Radar altitude calculation
            RaycastHit hit;
            int playerLayer = LayerMask.NameToLayer("Player");
            int ignorePlayerMask = ~(1 << playerLayer);

            if (Physics.Raycast(transform.position, Vector3.down, out hit, 20000f, ignorePlayerMask))
            {
                float rAlt;
                if (hit.distance > transform.position.y)
                    rAlt = transform.position.y;
                else
                    rAlt = hit.distance;

                radarAltitudeInd.text = ("RALT ") + (rAlt).ToString("F0") + "M";
            }
        }
        if (Mathf.Abs(prevClimbAngle - lookUpAmount) > 0.008f)
        {
            float climbAngle = Mathf.Asin(lookUpAmount) * Mathf.Rad2Deg; // in degrees
            climbAngleInd.text = "ANG " + climbAngle.ToString("F0") + "°";
            prevClimbAngle = lookUpAmount;
        }
    }
}
