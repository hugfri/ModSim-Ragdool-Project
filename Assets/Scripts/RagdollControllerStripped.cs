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

    public float wheelGripStrength;



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


    private JointDrive DriveOff;






    public float handTranslateLimit;
    public float wheelSpring;
    public float wheelDamping;


    FixedJoint rootGrabJoint;

    public Rigidbody rightHandRB;
    public Rigidbody leftHandRB;
    public Rigidbody wheelRB;
    public Transform leftHandWheel;
    public Transform rightHandWheel;


    public Rigidbody rootRB;
    public Rigidbody seatRB;
    public Transform seatLocation;



    public float seatGripStrength;



    void Awake()
    {
        SetupJointDrives();

    }

    private void SetupJointDrives()
    {

        DriveOff = JointDriveHelper.CreateJointDrive(25);

    }


    private void Start()
    {
        ChangeSolver();
        AttachHands();
    }


    void ChangeSolver()
    {
        wheelRB.solverIterations = 20;
        wheelRB.solverVelocityIterations = 20;

        leftHandRB.solverIterations = 20;
        leftHandRB.solverVelocityIterations = 20;

        rightHandRB.solverIterations = 20;
        rightHandRB.solverVelocityIterations = 20;
    }




    void AttachHands()
    {
        SoftJointLimit limit = new SoftJointLimit();
        limit.limit = handTranslateLimit;

        JointDrive drive = new JointDrive();
        drive.positionSpring = wheelSpring;
        drive.positionDamper = wheelDamping;
        drive.maximumForce = Mathf.Infinity;

        ConfigurableJoint leftJoint = leftHandRB.gameObject.AddComponent<ConfigurableJoint>();

        leftJoint.connectedBody = wheelRB;

        leftJoint.autoConfigureConnectedAnchor = false;

        leftJoint.anchor = Vector3.zero;
        leftJoint.connectedAnchor = wheelRB.transform.InverseTransformPoint(leftHandWheel.position);

        leftJoint.angularXMotion = ConfigurableJointMotion.Locked;
        leftJoint.angularYMotion = ConfigurableJointMotion.Locked;
        leftJoint.angularZMotion = ConfigurableJointMotion.Locked;

        leftJoint.xMotion = ConfigurableJointMotion.Limited;
        leftJoint.yMotion = ConfigurableJointMotion.Limited;
        leftJoint.zMotion = ConfigurableJointMotion.Limited;

        leftJoint.linearLimit = limit;

        leftJoint.xDrive = drive;
        leftJoint.yDrive = drive;
        leftJoint.zDrive = drive;

        leftJoint.enableCollision = false;
        leftJoint.projectionMode = JointProjectionMode.PositionAndRotation;
        leftJoint.projectionDistance = 0.02f;
        leftJoint.projectionAngle = 1f;



        ConfigurableJoint rightJoint = rightHandRB.gameObject.AddComponent<ConfigurableJoint>();

        rightJoint.connectedBody = wheelRB;

        rightJoint.autoConfigureConnectedAnchor = false;

        rightJoint.anchor = Vector3.zero;
        rightJoint.connectedAnchor = wheelRB.transform.InverseTransformPoint(rightHandWheel.position);

        rightJoint.angularXMotion = ConfigurableJointMotion.Locked;
        rightJoint.angularYMotion = ConfigurableJointMotion.Locked;
        rightJoint.angularZMotion = ConfigurableJointMotion.Locked;

        rightJoint.xMotion = ConfigurableJointMotion.Limited;
        rightJoint.yMotion = ConfigurableJointMotion.Limited;
        rightJoint.zMotion = ConfigurableJointMotion.Limited;

        rightJoint.linearLimit = limit;

        rightJoint.xDrive = drive;
        rightJoint.yDrive = drive;
        rightJoint.zDrive = drive;

        rightJoint.enableCollision = false;
        rightJoint.projectionMode = JointProjectionMode.PositionAndRotation;
        rightJoint.projectionDistance = 0.02f;
        rightJoint.projectionAngle = 1f;



        SetJointAngularDrives(UPPER_LEFT_ARM, in DriveOff);
        SetJointAngularDrives(LOWER_LEFT_ARM, in DriveOff);
        SetJointAngularDrives(UPPER_RIGHT_ARM, in DriveOff);
        SetJointAngularDrives(LOWER_RIGHT_ARM, in DriveOff);
        SetJointAngularDrives(BODY, in DriveOff);



        rootGrabJoint = rootRB.gameObject.AddComponent<FixedJoint>();
        rootGrabJoint.connectedBody = seatRB;

        rootGrabJoint.breakForce = seatGripStrength;
        rootGrabJoint.breakTorque = seatGripStrength;
        rootGrabJoint.enableCollision = false;
    }




    private void SetJointAngularDrives(string jointName, in JointDrive jointDrive)
    {
        RagdollDict[jointName].Joint.angularXDrive = jointDrive;
        RagdollDict[jointName].Joint.angularYZDrive = jointDrive;
    }


}

