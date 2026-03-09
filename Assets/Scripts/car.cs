using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using Unity.VisualScripting;
using UnityEngine.UI;


public class Engine
{
    public float idleRPM = 2400f;
    public float maxRPM = 7000f;
    public float[] gearRatio = {3.50f, 2.80f, 1.90f, 1.60f, 1.30f, 1.00f, 0.85f};
    public float finalDriveRatio = 4.0f;
    private int currentGear = 0;
    public bool automaticTransmission = false;
    private bool switchingGears = false;
    private float gearChangeTime = 0.18f;
    private float rpm = 0f;
    public void SetRPM(float averageWheelAngularVelocity)
    {
     float averageWheelRPM = (averageWheelAngularVelocity * 60f) / (2f * Mathf.PI);
     float totalRatio = Math.Abs(gearRatio[currentGear] * finalDriveRatio);
     float transmissionRPM = averageWheelRPM * totalRatio;
     float targetRPM = Mathf.Max(idleRPM, transmissionRPM);
     this.rpm = Mathf.Clamp(targetRPM,idleRPM,maxRPM);
    }

    public float GetCurrentPower(MonoBehaviour context)
{
    if (switchingGears) return 0.3f;
    
    // Power based on RPM (power curve)
    float rpmPower = Mathf.Clamp01(rpm / maxRPM);
    
    // Multiply by gear ratio - lower gears have more torque multiplication
    float gearMultiplier = gearRatio[currentGear] / gearRatio[0]; // Normalize to first gear
    
    return rpmPower * gearMultiplier;
}

    public float AngularVelocityToRPM(float angularVelocity)
    {
        return angularVelocity * 60f / (2f * Mathf.PI);
    }

    public void UpGear(MonoBehaviour context)
    {
        if (currentGear < gearRatio.Length - 1 && !switchingGears)
        {
            // Calculate RPM drop when shifting up
            float oldRatio = gearRatio[currentGear];
            currentGear++;
            float newRatio = gearRatio[currentGear];
            
            // Adjust RPM proportionally to the gear ratio change
            rpm = rpm * (newRatio / oldRatio);
            
            switchingGears = true;
            context.StartCoroutine(ResetSwitchingGearsCoroutine());
        }
    }

    public void DownGear(MonoBehaviour context)
    {
        if (currentGear > 0 && !switchingGears)
        {
            // Calculate RPM increase when shifting down
            float oldRatio = gearRatio[currentGear];
            currentGear--;
            float newRatio = gearRatio[currentGear];
            
            // Adjust RPM proportionally to the gear ratio change
            rpm = rpm * (newRatio / oldRatio);
            rpm = Mathf.Min(rpm, maxRPM); // Clamp to max RPM
            
            switchingGears = true;
            context.StartCoroutine(ResetSwitchingGearsCoroutine());
        }
    }

    private System.Collections.IEnumerator ResetSwitchingGearsCoroutine()
    {
        yield return new WaitForSeconds(gearChangeTime);
        switchingGears = false;
    }

    public int GetCurrentGear()
    {
        return currentGear + 1; // maybe good for UI?

    }

    public void checkGearSwitching(MonoBehaviour context)
    {
        if (switchingGears || !automaticTransmission) return;

        if (rpm > maxRPM * 0.80f && currentGear < gearRatio.Length - 1)
        {
            UpGear(context);
        }
        else if (rpm < maxRPM * 0.4f && currentGear > 0)
        {
            DownGear(context);
        }
    }

    public float GetRPM()
    {
        return rpm; // maybe good for UI?
    }

    public bool IsSwithchingGears()
    {
        return switchingGears; // maybe good for UI?
    }
}


[Serializable]
public class WheelProperties
{
    [HideInInspector] public TrailRenderer skidTrail;
    [HideInInspector] public GameObject skidTrailGameObject;

    public Vector3 localPosition;
    public float turnAngle = 30f;
    public float suspensionLength = 0.5f;

    [HideInInspector] public float lastSuspensionLength = 0.0f;

    public float mass = 16f;
    public float size = 0.5f;
    public float engineTorque = 40f;
    public float brakeStrength = 0.5f;
    public bool slidding = false;
    public bool isSteering = false;

    [HideInInspector] public Vector3 worldSlipDirection;
    [HideInInspector] public Vector3 suspensionForceDirection;
    [HideInInspector] public Vector3 wheelWorldPosition;
    [HideInInspector] public float wheelCircumference;
    [HideInInspector] public float torque = 0.0f;
    [HideInInspector] public GameObject wheelObject;
    [HideInInspector] public Vector3 localVelocity;
    [HideInInspector] public float normalForce;
    [HideInInspector] public float angularVelocity;
    [HideInInspector] public float slip;
    [HideInInspector] public Vector2 input = Vector2.zero;
    [HideInInspector] public float braking = 0;
    [HideInInspector] public float slipHistory = 0f;
    [HideInInspector] public float tcsReduction = 0f; // Traction control reduction
    [HideInInspector] public float steeringReduction = 0f; // Steering reduction
    [HideInInspector] public float xSlipAngle = 0f; // Slipping in x in degrees
    [HideInInspector] public float driftGripMultiplier = 1f;


}

public class car : MonoBehaviour
{
    public InputActionAsset inputActions;
    public float steerAssistTarget = 0.75f; // Target slip ratio for steering assist
    public float coefFrictionMultiplier = 1.0f; // Multiplier for friction coefficient
    public Vector3 centerOfDownforce = new Vector3(0, 0, 0);
    public Engine e;
    public GameObject skidMarkPrefab;
    public float smoothTurn = 0.03f;
    float coefStaticFriction = 1.95f;
    float coefKineticFriction = 0.95f;
    public GameObject wheelPrefab;
    public WheelProperties[] wheels;
    public float wheelGripX = 20f;
    public float wheelGripZ = 42f;
    public float suspensionForce = 90f;
    public float dampAmount = 2.5f;
    public float suspensionForceClamp = 200f;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public bool forwards = true;


    public bool steeringAssist = true;
    public bool throttleAssist = true;
    public bool brakeAssist = true;
    [HideInInspector] public Vector2 userInput = Vector2.zero;
    public float downforce = 0.16f;
    [HideInInspector] public float isBraking = 0f;

    public Vector3 COMOffset = new Vector3(0, -0.5f, 0);
    public float Inertia = 1.2f; // Multiplier for inertia tensor
    public Vector2 RawInput = Vector2.zero;
    private InputAction move;
    private InputAction Throttle;
    private InputAction Steer;
    public float carSpeedFactor = 0.03f;

    private InputAction Jump;
    public float jumpForce = 8f;
    private bool canJump = false;

    private CarLights carLights;

    public float carHP = 100f;
    public float carArmor = 0.1f; // smaller number means better armor WAY easier to code

    public float GetHP()
    {
        return carHP; // maybe good for UI?
    }

    public float getSpeed()
    {
        return rb.linearVelocity.magnitude * 3.6f; // returns in km/h
    }

    public void TakeDamage(float amount)
    {
    carHP -= carArmor * amount;
    carHP = Mathf.Clamp(carHP, 0f, 100f);
    }

public void Heal(float amount)
    {
    carHP += amount;
    carHP = Mathf.Clamp(carHP, 0f, 100f);
    }

public bool IsDestroyed()
    {
    return carHP <= 0f;
    }

    void Start()
    {
        //carHP = 100f; // reset HP
        carLights = GetComponent<CarLights>();
        rb = GetComponent<Rigidbody>();
        if(!rb) rb = gameObject.AddComponent<Rigidbody>();

        foreach (var w in wheels)
        {
            w.wheelObject = Instantiate(wheelPrefab, transform);
            w.wheelObject.transform.localPosition = w.localPosition;
            w.wheelObject.transform.eulerAngles = transform.eulerAngles;
            w.wheelObject.transform.localScale = 2f * new Vector3(w.size, w.size, w.size);
            w.wheelCircumference = 2f * Mathf.PI * w.size;

            if (skidMarkPrefab != null)
            {
                w.skidTrailGameObject = Instantiate(skidMarkPrefab, w.wheelObject.transform);
                w.skidTrailGameObject.transform.localPosition = Vector3.zero;
                w.skidTrailGameObject.transform.localRotation = Quaternion.identity;
                w.skidTrailGameObject.transform.parent = null;

                w.skidTrail = w.skidTrailGameObject.GetComponent<TrailRenderer>();
                if (w.skidTrail != null)
                    w.skidTrail.emitting = false;
            }
        }

        foreach (var w in wheels)
        {
            w.tcsReduction = 0f;
            w.slipHistory = 0f;

        }

        rb.centerOfMass += COMOffset;
        rb.inertiaTensor *= Inertia;
    }

    void Awake()
    {
        e = new Engine();
    }

    private void OnEnable()
    {

    if (inputActions == null)
    {
        Debug.LogError("InputActions asset not assigned!");
        return;
    }

    var moveMap = inputActions.FindActionMap("Move");
    if (moveMap == null)
    {
        Debug.LogError("Could not find 'Move' action map! Check your InputActions asset.");
        return;
    }

    // move = moveMap.FindAction("Main");
    // if (move == null)
    // {
    //     Debug.LogError("Could not find 'Main' action in Move map!");
    //     return;
    // }
    // move.Enable();

    Throttle = moveMap.FindAction("Throttle");
    if (Throttle == null)
    {
        Debug.LogError("Could not find 'Throttle' action in Move map!");
        return;
    }
    Throttle.Enable();

    Steer = moveMap.FindAction("Steer");
    if (Steer == null)
    {
        Debug.LogError("Could not find 'Steer' action in Move map!");
        return;
    }
    Steer.Enable();

    Jump = moveMap.FindAction("Jump");
    if (Jump != null) Jump.Enable();


    Debug.Log("All input actions loaded successfully!");
    }

    private void OnDisable()
    {
        if (move != null) move.Disable();
        if (Throttle != null) Throttle.Disable();
        if (Steer != null) Steer.Disable();
        if (Jump != null) Jump.Disable();

    }

    void Update()
    {
        if (Jump != null && Jump.WasPressedThisFrame() && canJump)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.rotation = Quaternion.identity;
            transform.position += Vector3.up * 2f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

         if (Throttle == null || Steer == null)
            return;

        float speedFactor = Mathf.Clamp(rb.linearVelocity.magnitude * 0.005f, 0f, 0.25f);
        userInput.x = Mathf.Lerp(userInput.x, Steer.ReadValue<float>() * (1f - speedFactor), 50f * Time.deltaTime);
        userInput.y = Mathf.Lerp(userInput.y, Throttle.ReadValue<float>(), 50f * Time.deltaTime);
        isBraking = userInput.y < 0 && forwards ? Mathf.Abs(userInput.y) : 0f;

        bool brakingNow = isBraking > 0f;
        carLights.isBackLightsOn = brakingNow;
        carLights.OperateBackLights();

        for (int i = 0; i < wheels.Length; i++)
        {
            var w = wheels[i];

            // no weird non values staying behind
            if (float.IsNaN(w.slip) || float.IsInfinity(w.slip))
                w.slip = 0f;

            // traction control
            if (throttleAssist)
            {
                float TargetSlip = 0.91f; // target amount of slip
                float slipTolerance = 0.02f; // how much slip is okay
                if (w.slip > TargetSlip + slipTolerance)
                {
                    float overshoot = w.slip - TargetSlip; // how much the slip is overshot
                    float reduction = Mathf.Clamp01(overshoot * 2.0f); // how much slip should be reduced
                    w.tcsReduction = Mathf.Lerp(w.tcsReduction, 1, reduction/5f); // adds traction by cutting some power
                }
                else if (w.slip < TargetSlip - slipTolerance)
                {
                    w.tcsReduction = Mathf.Lerp(w.tcsReduction, 0f, 0.6f * Time.deltaTime); // if too little slip add power
                }
                w.tcsReduction = Mathf.Clamp01(w.tcsReduction); // clamps the reduction between 0-1
            }
            if (steeringAssist) // same as above but with steering instead of throttle
            {
                float targetSlip = steerAssistTarget;
                float slipTolerance = 0.02f;
                if (w.slip > targetSlip + slipTolerance)
                {
                    
                    float overshoot = w.slip - targetSlip;
                    float reduction = Mathf.Clamp01(overshoot * 2.0f);
                    w.steeringReduction = Mathf.Lerp(w.steeringReduction, 1, reduction / 5f);
                }
                else if (w.slip < targetSlip - slipTolerance)
                {
                    w.steeringReduction = Mathf.Lerp(w.steeringReduction, 0f, 6f * Time.deltaTime);
                }
                w.steeringReduction = Mathf.Clamp01(w.steeringReduction);
            }
            w.braking = isBraking * (1 - w.tcsReduction);

            w.input.x = Mathf.Lerp(w.input.x, userInput.x * (1f - w.steeringReduction), Time.deltaTime * 60f);
            if (w.slip > 1.0f && steeringAssist) w.input.x = Mathf.Lerp(w.input.x, w.xSlipAngle / w.turnAngle, Time.deltaTime);

            // if traction control is activated it changes throttle here
            float finalThrottle = userInput.y * (1f - w.tcsReduction);
            if (float.IsNaN(finalThrottle) || float.IsInfinity(finalThrottle))
                finalThrottle = 0f;
            if (float.IsNaN(w.steeringReduction) || float.IsInfinity(w.steeringReduction))
                w.steeringReduction = 0f;
            
            if (throttleAssist)
            {
                w.input.y = Mathf.Lerp(w.input.y, finalThrottle, 0.95f * Time.deltaTime * 60f);
            } else w.input.y = userInput.y;
            
            if (float.IsNaN(w.input.y) || float.IsInfinity(w.input.y))
                w.input.y = 0f;
        }

        if (Input.GetKeyDown(KeyCode.E)) e.UpGear(this);
        else if (Input.GetKeyDown(KeyCode.Q)) e.DownGear(this);

        e.checkGearSwitching(this);
        }


    void FixedUpdate()
    {
        bool anyWheelGrounded = false;

        if (Throttle == null || Steer == null)
            return;

        float speedSquared = rb.linearVelocity.sqrMagnitude;
        rb.AddForceAtPosition(-transform.up * speedSquared * downforce / 28f, transform.position + transform.TransformDirection(centerOfDownforce), ForceMode.Acceleration);

        float averageWheelAngularVelocity = 0f;

        

        foreach (var w in wheels)
        {
            RaycastHit hit;
            float rayLen = w.size *2f + w.suspensionLength;
            Transform wheelObj = w.wheelObject.transform;
            Transform wheelVisual = wheelObj.GetChild(0);
            
            wheelObj.localRotation = Quaternion.Euler(0, w.isSteering ? w.turnAngle * w.input.x : 0, 0);
            w.wheelWorldPosition = transform.TransformPoint(w.localPosition);
            Vector3 velocityAtWheel = rb.GetPointVelocity(w.wheelWorldPosition);
            w.localVelocity = wheelObj.InverseTransformDirection(velocityAtWheel);
            forwards = w.localVelocity.z > 0.1f;
            w.torque = w.engineTorque * w.input.y * e.GetCurrentPower(this);

            float inertia = w.mass * w.size * w.size / 2f;
            float lateralVel = w.localVelocity.x;

            bool grounded = Physics.Raycast(w.wheelWorldPosition, -transform.up, out hit, rayLen);
            Vector3 worldVelAtHit = rb.GetPointVelocity(hit.point);
            float lateralHitVel =  wheelObj.InverseTransformDirection(worldVelAtHit).x;

            float lateralFriction = (-wheelGripX * lateralVel - 2f * lateralHitVel) * w.driftGripMultiplier;
            float longitudeFriction = -wheelGripZ * (w.localVelocity.z - w.angularVelocity * w.size);

            w.angularVelocity += (w.torque - longitudeFriction * w.size) / inertia * Time.fixedDeltaTime;
            w.angularVelocity *= 1 - w.braking * w.brakeStrength * Time.fixedDeltaTime;
            if (Input.GetKey(KeyCode.Space))
            {
                w.driftGripMultiplier = Mathf.Lerp(w.driftGripMultiplier, 0.05f, Time.fixedDeltaTime * 25f);
                w.angularVelocity = Mathf.Lerp(w.angularVelocity, 0f, Time.fixedDeltaTime * 0.5f);
            }
            else
            {
                w.driftGripMultiplier = Mathf.Lerp(w.driftGripMultiplier, 1f, Time.fixedDeltaTime * 3f);
            }
            
            Vector3 totalLocalForce = new Vector3(lateralFriction, 0f, longitudeFriction)
                * w.normalForce * coefStaticFriction * coefFrictionMultiplier * Time.fixedDeltaTime;
            float currentMaxFrictionForce = w.normalForce * coefStaticFriction * coefFrictionMultiplier * w.driftGripMultiplier;

            w.slip = currentMaxFrictionForce > 0.01f ? totalLocalForce.magnitude / currentMaxFrictionForce : 1f;
            w.slidding = w.slip > 0.7f; // w.slidding = totalLocalForce.magnitude > currentMaxFrictionForce; also alternative, but it shows less skidmarks
            totalLocalForce = Vector3.ClampMagnitude(totalLocalForce, currentMaxFrictionForce);
            totalLocalForce *= w.slidding ? (coefKineticFriction / coefStaticFriction) : 1;

            Vector3 totalWorldForce = wheelObj.TransformDirection(totalLocalForce);
            w.worldSlipDirection = totalWorldForce;

            if (w.localVelocity.magnitude > 0.5f) // when moving
            {
                float velocityAngle = Mathf.Atan2(w.localVelocity.x, w.localVelocity.z) * Mathf.Rad2Deg;
                float currentWheelAngle = w.turnAngle * w.input.x;
                
                // difference between current angle and future angle
                float rawSlipAngle = velocityAngle - currentWheelAngle;
                
                // Normalize angle
                while (rawSlipAngle > 180f) rawSlipAngle -= 360f;
                while (rawSlipAngle < -180f) rawSlipAngle += 360f;
                
                // smoothing to reduce jitter
                w.xSlipAngle = Mathf.Lerp(w.xSlipAngle, rawSlipAngle, Time.fixedDeltaTime * 10f);
            }
            else
            {
                w.xSlipAngle = Mathf.Lerp(w.xSlipAngle, 0f, Time.fixedDeltaTime * 5f);
            }

            if (grounded) // when on the ground
            {
                anyWheelGrounded = true;

                float compression = rayLen - hit.distance;
                float damping = (w.lastSuspensionLength - hit.distance) * dampAmount;
                w.normalForce = (compression + damping) * suspensionForce;
                w.normalForce = Mathf.Clamp(w.normalForce, 0f, suspensionForceClamp);

                Vector3 springDir = hit.normal * w.normalForce;
                w.suspensionForceDirection = springDir;

                rb.AddForceAtPosition(springDir + totalWorldForce, hit.point);
                w.lastSuspensionLength = hit.distance;
                wheelObj.position = hit.point + transform.up * w.size;

                if (w.slidding) // if sliding start skidmark Logic
                {
                    // If no skid trail exists, create a new one
                    if (w.skidTrail == null && skidMarkPrefab != null)
                    {
                        GameObject skidTrailObj = Instantiate(skidMarkPrefab, transform);
                        skidTrailObj.transform.SetParent(w.wheelObject.transform);
                        skidTrailObj.transform.localPosition = Vector3.zero;
                        w.skidTrail = skidTrailObj.GetComponent<TrailRenderer>();
                        w.skidTrail.time = 3f;
                        w.skidTrail.autodestruct = true;
                        w.skidTrail.emitting = false; // Start with emitting disabled
                        w.skidTrail.transform.position = hit.point;

                        // Set initial rotation
                        Vector3 skidDir = Vector3.ProjectOnPlane(w.worldSlipDirection.normalized, hit.normal);
                        if (skidDir.sqrMagnitude < 0.001f) skidDir = Vector3.ProjectOnPlane(wheelObj.forward, hit.normal).normalized;
                        Quaternion flatRot = Quaternion.LookRotation(skidDir, hit.normal) * Quaternion.Euler(90f, 0f, 0f);
                        w.skidTrail.transform.rotation = flatRot;
                    }
                    else if (w.skidTrail != null)
                    {
                        // Only start emitting after the trail has existed for at least one frame for safety
                        if (!w.skidTrail.emitting)
                        {
                            w.skidTrail.emitting = true;
                        }

                        // Update position and rotation
                        w.skidTrail.transform.position = hit.point;
                        Vector3 skidDir = Vector3.ProjectOnPlane(w.worldSlipDirection.normalized, hit.normal);
                        if (skidDir.sqrMagnitude < 0.001f) skidDir = Vector3.ProjectOnPlane(wheelObj.forward, hit.normal).normalized;
                        Quaternion flatRot = Quaternion.LookRotation(skidDir, hit.normal) * Quaternion.Euler(90f, 0f, 0f);
                        w.skidTrail.transform.rotation = flatRot;
                    }
                }
                else if (w.skidTrail != null)
                {
                    // Only detach and destroy if trail was emitting
                    if (w.skidTrail.emitting)
                    {
                        w.skidTrail.emitting = false;
                        w.skidTrail.transform.parent = null;
                        Destroy(w.skidTrail.gameObject, w.skidTrail.time);
                    }
                    else
                    {
                        // If it never started emitting, destroy it
                        Destroy(w.skidTrail.gameObject);
                    }
                    w.skidTrail = null;
                }
                averageWheelAngularVelocity += w.angularVelocity;
            }
            else
            {
                wheelObj.position = w.wheelWorldPosition + transform.up * (w.size - rayLen);
                if (w.skidTrail != null && w.skidTrail.emitting)
                {
                    w.skidTrail.emitting = false;
                    w.skidTrail.transform.parent = null;
                    Destroy(w.skidTrail.gameObject, w.skidTrail.time);
                    w.skidTrail = null;
                }
            }
            wheelVisual.Rotate(
                Vector3.right,
                w.angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime,
                Space.Self
            );
        }
        canJump = anyWheelGrounded;

                averageWheelAngularVelocity /= wheels.Length;
        e.SetRPM(averageWheelAngularVelocity);
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude * 3.6f;

        if (impactSpeed > 40f)
        {
            float damage = Mathf.Lerp(0f, 40f, (impactSpeed - 40f) / 160f);
            TakeDamage(damage);
        }
    }
}