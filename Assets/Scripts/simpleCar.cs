using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleCar : MonoBehaviour
{
    public float moveForce = 20f;
    public float turnSpeed = 120f;

    Rigidbody rb;

    Collider playerCollider;


    void Start()
    {
        rb = GetComponent<Rigidbody>();


    }


    void FixedUpdate()
    {
        float move = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");

        rb.AddForce(transform.forward * move * moveForce);

        transform.Rotate(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f);
    }
}