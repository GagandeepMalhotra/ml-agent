using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void FixedUpdate()
    {
        float moveInput = Input.GetAxis("Vertical");
        float rotateInput = Input.GetAxis("Horizontal");

        // Move the cube forward or backward
        Vector3 movement = transform.forward * moveInput * moveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);

        // Rotate the cube left or right
        Quaternion rotation = Quaternion.Euler(Vector3.up * rotateInput * rotationSpeed * Time.deltaTime);
        rb.MoveRotation(rb.rotation * rotation);
    }
}