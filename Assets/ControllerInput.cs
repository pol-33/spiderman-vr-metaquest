using Oculus.Interaction;
using UnityEngine;
using System.Collections;


public class ControllerInput : MonoBehaviour
{
    public GameObject vrCamParent;
    public GameObject vrEye;
    public float moveSpeed = 3.0f;
    public GameObject predictionPoint;
    public LineRenderer lineRenderer;
    public float jumpForce = 10f;

    public CustomLocomotionProvider locomotionProvider;

    private bool wasMoving = false;

    private Vector3 swingPoint;
    private Rigidbody rb;
    private SpringJoint joint;
    private float distance;

    private Vector3 targetPoint; // world-space target point for the web hit
    private bool hasPointed;

    private bool isGrounded;
    public float groundCheckDistance = 4f;

    public float pullSpeed = 500;
    public float strafeSpeed = 10;


    public float maxDistance = 0;
    public float pullStrength = 10;
    public bool useSpring = false;

    private void Start()
    {
        Application.targetFrameRate = 120;
        OVRManager.display.displayFrequency = 120.0f;
        rb = vrCamParent.GetComponent<Rigidbody>();
    }

    void Update()
    {
        isGrounded = CheckGrounded();

        // A Button Right (OVRInput.Button.One) - Jump 
        if (OVRInput.GetDown(OVRInput.Button.Three) && isGrounded)
        {
            Jump();
        }

        // Trigger
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);

        if (triggerValue > 0.1f)
        {
            // Debug.Log($"Trigger: {triggerValue}");
            if (hasPointed)
            {
                startSwing();
                drawLine();
                moveThumb();
                predictionPoint.SetActive(false);
                moveLine();
            }
            else
            {
                hasPointed = selectWebPoint();
                stopSwing();
                delLine();
                moveThumb();
            }
        }
        else
        {
            hasPointed = selectWebPoint();
            stopSwing();
            delLine();
            moveThumb();
        }

        UpdateLocomotionProvider();

        // Controller position and rotation
        //Vector3 position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        //Debug.Log($"ControllerPosition: {position}");

        //Quaternion rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
        //Debug.Log($"ControllerRotation: {rotation}");
        Vector3 dir = targetPoint - rb.position;
        if (distance > maxDistance)
        {
            //rb.AddForce(dir.normalized * pullStrength, ForceMode.Acceleration);
        }
    }
    void UpdateLocomotionProvider()
    {
        bool isMovingNow = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).magnitude > 0.1f || joint != null;

        if (isMovingNow != wasMoving)
        {
            locomotionProvider.SetMoving(isMovingNow, false); // false = right controller
            wasMoving = isMovingNow;
        }
    }

    void moveThumb()
    {
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        if (thumbstick.magnitude > 0.1f && joint == null)
        {
            Vector3 direction = (vrEye.transform.forward * thumbstick.y + vrEye.transform.right * thumbstick.x).normalized;

            if (isGrounded)
            {
                // Use MovePosition for grounded walking (smooth and controlled)
                Vector3 movement = direction * moveSpeed * Time.deltaTime;
                Vector3 Pos = rb.position + movement;
                rb.MovePosition(Pos);
            }
            else
            {
                // Use AddForce in the air to keep momentum and allow air control
                rb.AddForce(direction * moveSpeed * 10f * Time.deltaTime, ForceMode.Acceleration);
            }
        }

        // Strafing while swinging
        if (joint != null)
        {
            rb.AddForce(thumbstick.x * vrEye.transform.right * strafeSpeed);
        }
    }

    void moveLine()
    {
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        if (thumbstick.magnitude > 0.1f)
        {
            // use stored world-space targetPoint so it doesn't move with the controller
            Vector3 direction = targetPoint - rb.position;
            rb.MovePosition(rb.position + direction  * 0.1f * Time.deltaTime);
            rb.AddForce(direction.normalized * pullSpeed * thumbstick.y * Time.deltaTime);

            distance = Vector3.Distance(rb.position, swingPoint);
            if (thumbstick.y > 0) {
                joint.maxDistance = distance * 0.8f;
                joint.minDistance = distance * 0.2f;
            }
            else
            {
                joint.maxDistance = distance * 1.8f;
                joint.minDistance = distance * 1.2f;
            }
            // The distance grapple will try to keep from grapple point. 
            
        }
    }

    // Get swing point
    bool selectWebPoint()
    {
        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("Player"); // ignore player layer
        Vector3 start = transform.position + transform.forward * 0.2f;

        bool hasHit = Physics.Raycast(start, transform.forward, out hit, 100, layerMask);
        if (hasHit)
        {
            // Debug.Log("Hit: " + hit.collider.name);
            swingPoint = hit.point;

            // store the hit as a world-space target so it won't change when the controller/rig moves
            targetPoint = swingPoint;

            predictionPoint.SetActive(true);
            predictionPoint.transform.position = swingPoint;
            predictionPoint.GetComponent<Renderer>().material.color = Color.yellow;
        }
        else
        {
            // store a fallback world-space point
            targetPoint = transform.position + transform.forward * 100;
            predictionPoint.transform.position = targetPoint;
            predictionPoint.GetComponent<Renderer>().material.color = Color.red;
        }
        return hasHit;
    }

    void startSwing()
    {
        if (joint != null) return;

        joint = rb.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        distance = Vector3.Distance(rb.position, swingPoint);

        // The distance grapple will try to keep from grapple point. 
        joint.maxDistance = distance;

        joint.spring = 4.5f;
        joint.damper = 3f;
        joint.massScale = 2.5f;
    }

    void stopSwing()
    {
        Destroy(joint);
    }

    void drawLine()
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, targetPoint);
    }

    void delLine()
    {
        lineRenderer.enabled = false;
    }

    bool CheckGrounded()
    {
        // Check if there's something below the player
        RaycastHit hit;
        bool hasHitDown = Physics.Raycast(vrCamParent.transform.position, Vector3.down, out hit, groundCheckDistance);
        bool hasHitFront = CheckFrontHit();
        return hasHitDown || hasHitFront;
    }

    bool CheckFrontHit()
    {
        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask("Player"); // ignore player layer
        Vector3 start = vrEye.transform.position + vrEye.transform.forward * 0.2f;

        bool hasHitFront = Physics.Raycast(start, vrEye.transform.forward, out hit, groundCheckDistance * 1.3f, layerMask);
        //Debug.DrawRay(start, vrEye.transform.forward * groundCheckDistance * 1.3f, hasHitFront ? Color.green : Color.red);

        return hasHitFront;
    }



    void Jump()
    {
        if (CheckFrontHit())
        {
            //rb.AddForce(Vector3.back * jumpForce / 8, ForceMode.Impulse);
            rb.AddForce(Vector3.up * jumpForce * 1.2f, ForceMode.Impulse);
            // Start coroutine to add forward force after 1 second
            //StartCoroutine(AddForwardForceAfterDelay(0.5f));
        }
        else
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    IEnumerator AddForwardForceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        rb.AddForce(transform.forward * jumpForce * 1.2f, ForceMode.Impulse);
    }
}
