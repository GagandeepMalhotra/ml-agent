using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalTouched : MonoBehaviour
{
    public Transform targetObject;
    public Vector3 newScale = new Vector3(0.01f, 0.01f, 0.01f);
    private BoxCollider boxCollider;

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

        private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            targetObject.localScale = newScale;
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }
        }
    }
}