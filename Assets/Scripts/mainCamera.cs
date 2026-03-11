using UnityEngine;


// TODO
// camera shake on Acceleration
public class mainCamera : MonoBehaviour {

    public Transform targetTransform;
    public Rigidbody targetRigidbody;
    
    public Vector3 offsetSlow = new Vector3(0f, 2f, -5f); // Offset at low speed
    public Vector3 offsetFast = new Vector3(0f, 3f, -8f); // Offset at high speed
    
    public float maxSpeed = 30f; 
    
    public float positionSmoothSpeed = 10f;
    public float rotationSmoothSpeed = 5f;

    public bool enableMouseOrbit = true;
    public float mouseSensitivity = 3f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;
    private float orbitHorizontal = 0f;
    private float orbitVertical = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void LateUpdate()
    {
        orbitHorizontal += Input.GetAxis("Mouse X") * mouseSensitivity;
        orbitVertical -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        orbitVertical = Mathf.Clamp(orbitVertical, minVerticalAngle, maxVerticalAngle);

        float currentSpeed = targetRigidbody.linearVelocity.magnitude;
        float speedPercent = Mathf.Clamp01(currentSpeed / maxSpeed);
        Vector3 currentOffset = Vector3.Lerp(offsetSlow, offsetFast, speedPercent);
        
        Quaternion orbitRotation = Quaternion.Euler(orbitVertical, orbitHorizontal, 0f);
        Vector3 rotatedOffset = orbitRotation * currentOffset;


        Vector3 desiredPosition = targetTransform.position + targetTransform.rotation * rotatedOffset;
        
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * Time.deltaTime);

        Vector3 lookAtPoint = targetTransform.position;
        Quaternion desiredRot = Quaternion.LookRotation(lookAtPoint - transform.position);
        
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSmoothSpeed * Time.deltaTime);

    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

}