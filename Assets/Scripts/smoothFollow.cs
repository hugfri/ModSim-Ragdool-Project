using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    public Transform target; // The actual Rigidbody transform
    
    void LateUpdate()
    {
        // Smoothly follow the physics object
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}