using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TMPro;
using UnityEngine.Audio;

public class MoveToGoalAgent : Agent
{
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask whatIsPillar;
    [SerializeField] private bool isGrounded;

    [Header("Player Movement")]
    [SerializeField] private float moveSpeed = 1.75f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float jumpForce = 5f;

    private Rigidbody playerRb;

    private int currentEpisode = 1;

    public TextMeshPro episodeText;
    public TextMeshPro stepsText;
    public TextMeshPro scoreText;

    private float episodeTimer = 30f;
    private float totalTime = 30f;

    private List<GameObject> allGoals = new List<GameObject>();
    private List<GameObject> visitedGoals = new List<GameObject>();

    public GameObject gate;
    bool gateOpen = false;
    Vector3 closePosition = new Vector3 (0.5f, 1.3f, 8.5f);

    bool exitPlayMode = false;

    public Light[] lightComponents;

    private bool canPlayLandSound = true;
    private float landSoundCooldown = 1f;

    public override void OnEpisodeBegin()
    {
        playerRb = GetComponent<Rigidbody>();
       
        playerRb.velocity = Vector3.zero; // Reset velocity
        playerRb.angularVelocity = Vector3.zero; // Reset angular velocity

        //transform.localPosition = new Vector3(Random.Range(2.5f, -2.5f), 2.5f, Random.Range(-5f, -4f));
        transform.localPosition = new Vector3(Random.Range(2.5f, -2.5f), 2.5f, -6.5f);
        transform.rotation = Quaternion.identity;

        allGoals = new List<GameObject>();
        visitedGoals = new List<GameObject>();


        Transform goalParent = transform.parent;
        foreach (Transform child in goalParent)
        {
            if (child.CompareTag("Goal"))
            {
                allGoals.Add(child.gameObject);
                child.transform.localScale = new Vector3(1.5f, 0.25f, 1.5f);
                if (child.GetComponent<BoxCollider>() != null)
                {
                    child.GetComponent<BoxCollider>().enabled = true;
                }
            }
        }

        episodeTimer = 30f;

        scoreText.text = "Score: 0";

        episodeText.text = currentEpisode.ToString();
        currentEpisode++;

        StopAllCoroutines();
        gate.transform.localPosition = closePosition;
        gateOpen = false;

        for (int i = 0; i < lightComponents.Length; i++)
        {
            lightComponents[i].color = new Color(1f, 1f, 1f);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        // Add rotation observation
        sensor.AddObservation(transform.localRotation);
        foreach (GameObject target in allGoals)
        {
            sensor.AddObservation(target.transform.localPosition);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            EndEpisode();
        }

        episodeTimer -= Time.deltaTime;

        if (episodeTimer <= 0)
        {
            AddReward(-1f);
            EndEpisode();
        }

        AddReward(-Time.deltaTime / totalTime);

        float angle = Vector3.Angle(Vector3.up, transform.up);
        if (angle >= 60f || angle <= -60f)
        {
            AddReward(-Time.deltaTime / totalTime); // Negative reward for falling on the side or upside down
        }

        stepsText.text = Mathf.RoundToInt(episodeTimer).ToString();
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);

        scoreText.text = "Score: " + GetCumulativeReward().ToString("0.00");
    }

    public void MoveAgent(ActionSegment<int> act)
    {

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        var dirToGoForwardAction = act[0];
        var rotateDirAction = act[1];
        var jumpAction = act[2];

        if (dirToGoForwardAction == 1)
        {
            Vector3 movement = transform.forward * moveSpeed * Time.deltaTime;
            playerRb.MovePosition(playerRb.position + movement);
        }
        /*
        else if (dirToGoForwardAction == 2)
        {
            Vector3 movement = -transform.forward * moveSpeed * Time.deltaTime;
            playerRb.MovePosition(playerRb.position + movement);
        }*/
        if (rotateDirAction == 1)
        {
            Quaternion rotation = Quaternion.Euler(Vector3.up * -1 * rotationSpeed * Time.deltaTime);
            playerRb.MoveRotation(playerRb.rotation * rotation);
        }
        else if (rotateDirAction == 2)
        {
            Quaternion rotation = Quaternion.Euler(Vector3.up * 1 * rotationSpeed * Time.deltaTime);
            playerRb.MoveRotation(playerRb.rotation * rotation);
        }

        if (jumpAction == 1)
        {   
            if ((Physics.CheckSphere(groundCheck.position, groundRadius, (int)whatIsGround)) || (Physics.CheckSphere(groundCheck.position, groundRadius, (int)whatIsPillar))){
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
            if (isGrounded == true)
            {
                playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isGrounded = false;
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {

        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActionsOut[1] = 1;
        }
        /*
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 2;
        }
        */
        if (Input.GetKey(KeyCode.Space))
        {
            discreteActionsOut[2] = 1;
        }
    }


    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Fail"))
        {
            //AddReward(-(4 + ((totalTime - episodeTimer) / totalTime)));
            SetReward(-5f);
            //Debug.Log(GetCumulativeReward().ToString("0.00"));
            EndEpisode();
        }

        if (other.CompareTag("Goal"))
        {
            if (!visitedGoals.Contains(other.gameObject))
            {
                AddReward(1 - ((totalTime - episodeTimer) / totalTime));
                visitedGoals.Add(other.gameObject);
                if (transform.parent != null && transform.parent.CompareTag("Sound"))
                {
                    FindObjectOfType<AudioManager>().Play("button");
                }
            }

            if (visitedGoals.Count == allGoals.Count - 1 && gateOpen == false) // Check if all goals are touched.
            {
                OperateDoor();
                StartCoroutine(TransitionColor());
            }

            if (visitedGoals.Count == allGoals.Count) // Check if all goals are touched.
            {
                AddReward((1 - ((totalTime - episodeTimer) / totalTime)));
                if (transform.parent != null && transform.parent.CompareTag("Sound"))
                {
                    FindObjectOfType<AudioManager>().Play("finish");
                    if (!exitPlayMode)
                    {
                        StartCoroutine(ExitPlayModeDelayed());
                        exitPlayMode = true; // Prevent calling the coroutine multiple times
                    }
                }
                else
                {
                    EndEpisode();
                }

             
            }
        }
    }

    IEnumerator ExitPlayModeDelayed()
    {
        yield return new WaitForSeconds(1.0f); // Wait for one second
        EndEpisode();
        #if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
        #endif
    }

    void OperateDoor()
    {
        if (transform.parent != null && transform.parent.CompareTag("Sound"))
        {
            FindObjectOfType<AudioManager>().Play("gate");
        }
        StopAllCoroutines();
        if (!gateOpen)
        {
            Vector3 openPosition = closePosition + Vector3.up * 2.7f;
            StartCoroutine(MoveDoor(openPosition));
        }
        else
        {
            StartCoroutine(MoveDoor(closePosition));
        }
        gateOpen = !gateOpen;
    }

    IEnumerator MoveDoor(Vector3 targetPosition)
    {        
        float timeElapsed = 0;
        Vector3 startPosition = gate.transform.localPosition;
        while (timeElapsed < 1f)
        {
            gate.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, timeElapsed / 1f);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        gate.transform.localPosition = targetPosition;

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            AddReward(-0.25f); // Penalty for touching a wall
        }
        else if (collision.collider.CompareTag("Ground") && canPlayLandSound)
        {
            if (transform.parent != null && transform.parent.CompareTag("Sound"))
            {
                FindObjectOfType<AudioManager>().Play("land");
            }
            canPlayLandSound = false;
            Invoke("ResetLandSoundCooldown", landSoundCooldown);
            AddReward(-0.05f); // Penalty for touching ground again
        }
    }

    private void ResetLandSoundCooldown()
    {
        canPlayLandSound = true;
    }

    IEnumerator TransitionColor()
    {
        float elapsedTime = 0f;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / 1f);

            for (int i = 0; i < lightComponents.Length; i++)
            {
                lightComponents[i].color = Color.Lerp(new Color(1f, 1f, 1f), new Color (0.6f, 1f, 0.6f), t);
            }

            yield return null;
        }

        for (int i = 0; i < lightComponents.Length; i++)
        {
            lightComponents[i].color = new Color(0.6f, 1f, 0.6f); // Ensure the target color is set precisely
        }
    }
}
