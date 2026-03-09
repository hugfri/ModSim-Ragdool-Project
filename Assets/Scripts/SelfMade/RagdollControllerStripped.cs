using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using Utils;





public class RagdollControllerStripped : MonoBehaviour
{
    [SerializeField] private UDictionary<string, RagdollJoint> RagdollDict = new UDictionary<string, RagdollJoint>();

    [SerializeField] private Rigidbody rightHand;
    [SerializeField] private Rigidbody leftHand;

    [SerializeField] private Transform centerOfMass;

    [Header("Movement Properties")] public bool forwardIsCameraDirection = true;
    public float moveSpeed = 10f;
    public float turnSpeed = 6f;
    public float jumpForce = 18f;

    public Vector2 MovementAxis { get; set; } = Vector2.zero;
    public Vector2 AimAxis { get; set; }
    public float JumpValue { get; set; } = 0;
    public float GrabLeftValue { get; set; } = 0;
    public float GrabRightValue { get; set; } = 0;

    [Header("Balance Properties")] public bool autoGetUpWhenPossible = true;
    public bool useStepPrediction = true;
    public float balanceHeight = 2.5f;
    public float balanceStrength = 5000f;
    public float coreStrength = 1500f;
    public float limbStrength = 500f;

    public float StepDuration = 0.2f;
    public float StepHeight = 1.7f;
    public float FeetMountForce = 25f;

    [Header("Reach Properties")] public float reachSensitivity = 25f;
    public float armReachStiffness = 2000f;

    [Header("Actions")] public bool canBeKnockoutByImpact = true;
    public float requiredForceToBeKO = 20f;
    public bool canPunch = true;
    public float punchForce = 15f;

    private const string ROOT = "Root";
    private const string BODY = "Body";
    private const string HEAD = "Head";
    private const string UPPER_RIGHT_ARM = "UpperRightArm";
    private const string LOWER_RIGHT_ARM = "LowerRightArm";
    private const string UPPER_LEFT_ARM = "UpperLeftArm";
    private const string LOWER_LEFT_ARM = "LowerLeftArm";
    private const string UPPER_RIGHT_LEG = "UpperRightLeg";
    private const string LOWER_RIGHT_LEG = "LowerRightLeg";
    private const string UPPER_LEFT_LEG = "UpperLeftLeg";
    private const string LOWER_LEFT_LEG = "LowerLeftLeg";
    private const string RIGHT_FOOT = "RightFoot";
    private const string LEFT_FOOT = "LeftFoot";

    //Hidden variables
    private float timer;
    private float Step_R_timer;
    private float Step_L_timer;
    private float MouseYAxisArms;
    private float MouseXAxisArms;
    private float MouseYAxisBody;


    private bool balanced = true;
    private bool GettingUp;
    private bool ResetPose;
    private bool isRagdoll;
    private bool isKeyDown;
    private bool moveAxisUsed;
    private bool jumpAxisUsed;
    private bool reachLeftAxisUsed;
    private bool reachRightAxisUsed;

    [HideInInspector] public bool jumping;
    [HideInInspector] public bool isJumping;
    [HideInInspector] public bool inAir;
    [HideInInspector] public bool punchingRight;
    [HideInInspector] public bool punchingLeft;

    [SerializeField] private Camera cam;
    private Vector3 Direction;
    private Vector3 CenterOfMassPoint;

    private JointDrive BalanceOn;
    private JointDrive PoseOn;
    private JointDrive CoreStiffness;
    private JointDrive ReachStiffness;
    private JointDrive DriveOff;

    private Quaternion HeadTarget;
    private Quaternion BodyTarget;
    private Quaternion UpperRightArmTarget;
    private Quaternion LowerRightArmTarget;
    private Quaternion UpperLeftArmTarget;
    private Quaternion LowerLeftArmTarget;
    private Quaternion UpperRightLegTarget;
    private Quaternion LowerRightLegTarget;
    private Quaternion UpperLeftLegTarget;
    private Quaternion LowerLeftLegTarget;




    ConfigurableJoint leftGrabJoint;
    ConfigurableJoint rightGrabJoint;

    public Rigidbody rightHandRB;
    public Rigidbody leftHandRB;
    public Rigidbody wheelRB;
    public Transform leftHandWheel;
    public Transform rightHandWheel;

    private static int groundLayer;

    void Awake()
    {
        //cam = Camera.main;
        groundLayer = LayerMask.NameToLayer("Ground");
        SetupJointDrives();
        SetupOriginalPose();
        
    }

    private void SetupJointDrives()
    {
        BalanceOn = JointDriveHelper.CreateJointDrive(balanceStrength);
        PoseOn = JointDriveHelper.CreateJointDrive(limbStrength);
        CoreStiffness = JointDriveHelper.CreateJointDrive(coreStrength);
        ReachStiffness = JointDriveHelper.CreateJointDrive(armReachStiffness);
        DriveOff = JointDriveHelper.CreateJointDrive(25);

    }

    private void SetupOriginalPose()
    {
        
        BodyTarget = GetJointTargetRotation(ROOT);
        HeadTarget = GetJointTargetRotation(HEAD);
        //UpperRightArmTarget = GetJointTargetRotation(UPPER_RIGHT_ARM);
        //LowerRightArmTarget = GetJointTargetRotation(LOWER_RIGHT_ARM);
        //UpperLeftArmTarget = GetJointTargetRotation(UPPER_LEFT_ARM);
        //LowerLeftArmTarget = GetJointTargetRotation(LOWER_LEFT_ARM);
        UpperRightLegTarget = GetJointTargetRotation(UPPER_RIGHT_LEG);
        LowerRightLegTarget = GetJointTargetRotation(LOWER_RIGHT_LEG);
        UpperLeftLegTarget = GetJointTargetRotation(UPPER_LEFT_LEG);
        LowerLeftLegTarget = GetJointTargetRotation(LOWER_LEFT_LEG);



        leftGrabJoint = leftHandRB.gameObject.AddComponent<ConfigurableJoint>();
        rightGrabJoint = rightHand.gameObject.AddComponent<ConfigurableJoint>();

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Quaternion GetJointTargetRotation(string jointName)
    {
        return RagdollDict[jointName].Joint.targetRotation;
    }




    void AttachHands()
    {

        leftHandRB.transform.position = leftHandWheel.position;
        rightHand.transform.position = rightHandWheel.position;

        leftGrabJoint.connectedBody = wheelRB;
     

        rightGrabJoint.connectedBody = wheelRB;

    }


    private void Update()
    {

        PlayerReach();

        if (balanced && useStepPrediction)
        {
            UpdateCenterOfMass();
        }

        GroundCheck();
        UpdateCenterOfMass();
    }

    private void FixedUpdate()
    {
        AttachHands();
        //PerformPlayerRotation();
       // ResetPlayerPose();
        //PerformPlayerGetUpJumping();
    }

    private void PerformPlayerRotation()
    {
        ConfigurableJoint rootJoint = RagdollDict[ROOT].Joint;

        if (forwardIsCameraDirection)
        {
            var lookPos = cam.transform.forward.ToX0Z();
            var rotation = Quaternion.LookRotation(lookPos);
            rootJoint.targetRotation = Quaternion.Slerp(rootJoint.targetRotation,
                Quaternion.Inverse(rotation), Time.deltaTime * turnSpeed);
        }
        else
        {
            //buffer all changes to quaternion before applying to memory location
            Quaternion rootJointTargetRotation = rootJoint.targetRotation;

            if (MovementAxis.x != 0)
            {
                rootJointTargetRotation = Quaternion.Lerp(rootJointTargetRotation,
                    rootJointTargetRotation.DisplaceY(-MovementAxis.x * turnSpeed),
                    6 * Time.fixedDeltaTime);
            }

            if (rootJointTargetRotation.y < -0.98f)
            {
                rootJointTargetRotation = rootJointTargetRotation.ModifyY(0.98f);
            }

            else if (rootJointTargetRotation.y > 0.98f)
            {
                rootJointTargetRotation = rootJointTargetRotation.ModifyY(-0.98f);
            }

            rootJoint.targetRotation = rootJointTargetRotation;
        }
    }

    private void ResetPlayerPose()
    {
        if (ResetPose && !jumping)
        {
            RagdollDict[BODY].Joint.targetRotation = BodyTarget;
            //RagdollDict[UPPER_RIGHT_ARM].Joint.targetRotation = UpperRightArmTarget;
            //RagdollDict[LOWER_RIGHT_ARM].Joint.targetRotation = LowerRightArmTarget;
            //RagdollDict[UPPER_LEFT_ARM].Joint.targetRotation = UpperLeftArmTarget;
            //RagdollDict[LOWER_LEFT_ARM].Joint.targetRotation = LowerLeftArmTarget;

            //MouseYAxisArms = 0;
            ResetPose = false;
        }
    }

    public void PlayerLanded()
    {
        if (CanResetPoseAfterLanding())
        {
            inAir = false;
            ResetPose = true;
        }
    }

    private bool CanResetPoseAfterLanding()
    {
        return inAir && !isJumping && !jumping;
    }

    private void PerformPlayerGetUpJumping()
    {
        if (JumpValue > 0)
        {
            if (!jumpAxisUsed)
            {
                if (balanced && !inAir)
                {
                    jumping = true;
                }

                else if (!balanced)
                {
                    DeactivateRagdoll();
                }
            }

            jumpAxisUsed = true;
        }

        else
        {
            jumpAxisUsed = false;
        }


        if (jumping)
        {
            isJumping = true;

            Rigidbody rootRigidbody = RagdollDict[ROOT].Rigidbody;
            rootRigidbody.linearVelocity = rootRigidbody.linearVelocity.ModifyY(rootRigidbody.transform.up.y * jumpForce);
        }

        if (isJumping)
        {
            timer += Time.fixedDeltaTime;

            if (timer > 0.2f)
            {
                timer = 0.0f;
                jumping = false;
                isJumping = false;
                inAir = true;
            }
        }
    }

    private void GroundCheck()
    {
        Transform rootTransform = RagdollDict[ROOT].transform;
        Ray ray = new Ray(rootTransform.position, -rootTransform.up);
        bool isHittingGround = Physics.Raycast(ray, out _, balanceHeight, 1 << groundLayer);

        if (!isHittingGround)
        {
            if (balanced)
            {
                balanced = false;
            }
        }
        else if (ShouldSetBalanced())
        {
            balanced = true;
        }

        bool needsStateChange = (balanced == isRagdoll);

        if (!needsStateChange)
            return;

        if (balanced)
        {
            DeactivateRagdoll();
        }
        else
        {
            ActivateRagdoll();
        }
    }

    private bool ShouldSetBalanced()
    {
        return !inAir &&
               !isJumping &&
               !reachRightAxisUsed &&
               !reachLeftAxisUsed &&
               !balanced &&
               RagdollDict[ROOT].Rigidbody.linearVelocity.magnitude < 1f &&
               autoGetUpWhenPossible;
    }


    private void SetRagdollState(bool shouldRagdoll, ref JointDrive rootJointDrive, ref JointDrive poseJointDrive,
        bool shouldResetPose)
    {
        isRagdoll = shouldRagdoll;
        balanced = !shouldRagdoll;

        SetJointAngularDrives(ROOT, in rootJointDrive);
        SetJointAngularDrives(HEAD, in poseJointDrive);

        if (!reachRightAxisUsed)
        {
            SetJointAngularDrives(UPPER_RIGHT_ARM, in poseJointDrive);
            SetJointAngularDrives(LOWER_RIGHT_ARM, in poseJointDrive);
        }

        if (!reachLeftAxisUsed)
        {
            SetJointAngularDrives(UPPER_LEFT_ARM, in poseJointDrive);
            SetJointAngularDrives(LOWER_LEFT_ARM, in poseJointDrive);
        }

        SetJointAngularDrives(UPPER_RIGHT_LEG, in poseJointDrive);
        SetJointAngularDrives(LOWER_RIGHT_LEG, in poseJointDrive);
        SetJointAngularDrives(UPPER_LEFT_LEG, in poseJointDrive);
        SetJointAngularDrives(LOWER_LEFT_LEG, in poseJointDrive);
        SetJointAngularDrives(RIGHT_FOOT, in poseJointDrive);
        SetJointAngularDrives(LEFT_FOOT, in poseJointDrive);

        if (shouldResetPose)
            ResetPose = true;
    }

    private void DeactivateRagdoll() => SetRagdollState(false, ref BalanceOn, ref PoseOn, true);

    public void ActivateRagdoll() => SetRagdollState(true, ref DriveOff, ref DriveOff, false);

    private void SetJointAngularDrives(string jointName, in JointDrive jointDrive)
    {
        RagdollDict[jointName].Joint.angularXDrive = jointDrive;
        RagdollDict[jointName].Joint.angularYZDrive = jointDrive;
    }


    private void SetJointAngularDrivesForLegs(in JointDrive jointDrive)
    {
        SetJointAngularDrives(UPPER_RIGHT_LEG, in jointDrive);
        SetJointAngularDrives(LOWER_RIGHT_LEG, in jointDrive);
        SetJointAngularDrives(UPPER_LEFT_LEG, in jointDrive);
        SetJointAngularDrives(LOWER_LEFT_LEG, in jointDrive);
        SetJointAngularDrives(RIGHT_FOOT, in jointDrive);
        SetJointAngularDrives(LEFT_FOOT, in jointDrive);
    }

    private void PlayerReach()
    {
        MouseYAxisBody = Mathf.Clamp(MouseYAxisBody += (AimAxis.y / reachSensitivity), -0.2f, 0.1f);
        RagdollDict[BODY].Joint.targetRotation = new Quaternion(MouseYAxisBody, 0, 0, 1);

        if (GrabLeftValue != 0 && !punchingLeft)
        {
            if (!reachLeftAxisUsed)
            {
                SetJointAngularDrives(UPPER_LEFT_ARM, in ReachStiffness);
                SetJointAngularDrives(LOWER_LEFT_ARM, in ReachStiffness);
                SetJointAngularDrives(BODY, in CoreStiffness);
                reachLeftAxisUsed = true;
                reachLeftAxisUsed = true;
            }

            MouseYAxisArms = Mathf.Clamp(MouseYAxisArms += (AimAxis.y / reachSensitivity), -1.2f, 1.2f);
            RagdollDict[UPPER_LEFT_ARM].Joint.targetRotation =
                new Quaternion(-0.58f - (MouseYAxisArms), -0.88f - (MouseYAxisArms), -0.8f, 1);
        }

        if (GrabLeftValue == 0 && !punchingLeft)
        {
            if (reachLeftAxisUsed)
            {
                if (balanced)
                {
                    SetJointAngularDrives(UPPER_LEFT_ARM, in PoseOn);
                    SetJointAngularDrives(LOWER_LEFT_ARM, in PoseOn);
                    SetJointAngularDrives(BODY, in PoseOn);
                }
                else if (!balanced)
                {
                    SetJointAngularDrives(UPPER_LEFT_ARM, in DriveOff);
                    SetJointAngularDrives(LOWER_LEFT_ARM, in DriveOff);
                }

                ResetPose = true;
                reachLeftAxisUsed = false;
            }
        }

        if (GrabRightValue != 0 && !punchingRight)
        {
            if (!reachRightAxisUsed)
            {
                SetJointAngularDrives(UPPER_RIGHT_ARM, in ReachStiffness);
                SetJointAngularDrives(LOWER_RIGHT_ARM, in ReachStiffness);
                SetJointAngularDrives(BODY, in CoreStiffness);
                reachRightAxisUsed = true;
            }

            MouseYAxisArms = Mathf.Clamp(MouseYAxisArms += (AimAxis.y / reachSensitivity), -1.2f, 1.2f);
            RagdollDict[UPPER_RIGHT_ARM].Joint.targetRotation =
                new Quaternion(0.58f + (MouseYAxisArms), -0.88f - (MouseYAxisArms), 0.8f, 1);
        }

        if (GrabRightValue == 0 && !punchingRight)
        {
            if (reachRightAxisUsed)
            {
                if (balanced)
                {
                    SetJointAngularDrives(UPPER_RIGHT_ARM, in PoseOn);
                    SetJointAngularDrives(LOWER_RIGHT_ARM, in PoseOn);
                    SetJointAngularDrives(BODY, in PoseOn);
                }
                else if (!balanced)
                {
                    SetJointAngularDrives(UPPER_RIGHT_ARM, in DriveOff);
                    SetJointAngularDrives(LOWER_RIGHT_ARM, in DriveOff);
                }

                ResetPose = true;
                reachRightAxisUsed = false;
            }
        }
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCenterOfMass()
    {
        //Be wary of this, it's called up to 2 times per frame
        Vector3 massPositionDisplacement = Vector3.zero;
        float totalMass = 0;

        foreach (var element in RagdollDict)
        {
            var joint = element.Value;
            var mass = joint.Rigidbody.mass;
            massPositionDisplacement += mass * joint.transform.position;
            totalMass += mass;
        }

        CenterOfMassPoint = (massPositionDisplacement / totalMass);
        centerOfMass.position = CenterOfMassPoint;
    }

}

