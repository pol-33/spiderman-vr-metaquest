using Oculus.Interaction;
using UnityEngine;
using System.Collections;


public class ControllerInput : MonoBehaviour
{
    public GameObject vrCamParent;
    public GameObject vrEye;
    public float moveSpeed = 15.0f;
    public GameObject predictionPoint;
    public LineRenderer lineRenderer;
    public float jumpForce = 10f;

    public CustomLocomotionProvider locomotionProvider;

    private bool wasMoving = false;

    private Vector3 swingPoint;
    private Rigidbody rb;
    private SpringJoint joint;
    private GameObject cat;
    private Rigidbody catRigidbody;
    private float distance;

    private Vector3 targetPoint; // world-space target point for the web hit
    private bool hasPointed;

    private bool isGrounded;
    public float groundCheckDistance = 4f;

    public float pullSpeed = 500;
    public float strafeSpeed = 10;
    public float maxAirSpeed = 20f; // Maximum speed in the air


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

            selectCat();
        }
        else
        {
            hasPointed = selectWebPoint();
            stopSwing();
            delLine();
            moveThumb();
        }

        if (cat != null)
        {
            cat.transform.position = transform.position;
            cat.transform.rotation = transform.rotation;
        }
        
        float grabValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
        if(grabValue > 0.6 && cat != null)
        {
            DropCat();
        }

        if (OVRInput.GetDown(OVRInput.Button.Three))

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
                // Only add force if we haven't reached max air speed
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                
                if (horizontalVelocity.magnitude < maxAirSpeed)
                {
                    rb.AddForce(direction * strafeSpeed * 10f * Time.deltaTime, ForceMode.Acceleration);
                }
                else
                {
                    // Clamp the horizontal velocity to max air speed
                    Vector3 clampedVelocity = horizontalVelocity.normalized * maxAirSpeed;
                    rb.linearVelocity = new Vector3(clampedVelocity.x, rb.linearVelocity.y, clampedVelocity.z);
                }
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
            rb.MovePosition(rb.position + direction.normalized * Time.deltaTime);
            rb.AddForce(direction.normalized * pullSpeed * thumbstick.y * Time.deltaTime);

            distance = Vector3.Distance(rb.position, swingPoint);
            if (thumbstick.y > 0) {
                joint.maxDistance = distance * 0.8f;
                joint.minDistance = 0;// distance * 0.2f;
            }
            else
            {
                joint.maxDistance = distance * 1.8f;
                joint.minDistance = 0;//distance * 1.2f;
            }
            // The distance grapple will try to keep from grapple point. 
            
        }
    }

    void selectCat()
    {
        if (cat != null) return; // Already holding a cat
        
        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Cat");
        Vector3 start = transform.position + transform.forward * 0.2f;
        bool hasHit = Physics.Raycast(start, transform.forward, out hit, 100, layerMask);
        if (hasHit)
        {
            cat = hit.collider.gameObject;
            catRigidbody = cat.GetComponent<Rigidbody>();
            
            if (catRigidbody != null)
            {
                // Disable physics while holding
                catRigidbody.isKinematic = true;
                catRigidbody.useGravity = false;
            }
            
            cat.transform.localScale = Vector3.one;
            
            // Notify the cat controller that it's been grabbed
            var catController = cat.GetComponent<CatController>();
            if (catController != null)
            {
                catController.OnSelectEnter();
            }
        }
    }
    
    void DropCat()
    {
        if (cat == null) return;
        
        // Reset scale
        cat.transform.localScale = Vector3.one * 6f;
        
        if (catRigidbody != null)
        {
            cat.transform.rotation = Quaternion.identity; // Keep upright
            
            // Re-enable physics
            catRigidbody.isKinematic = false;
            catRigidbody.useGravity = true;
            
            // Reset velocity to prevent swinging
            catRigidbody.linearVelocity = Vector3.zero;
            catRigidbody.angularVelocity = Vector3.zero;
            
            // Don't freeze rotation - let animations control it
            catRigidbody.constraints = RigidbodyConstraints.None;
            
            // Optionally add a small downward force for more natural drop
            catRigidbody.AddForce(Vector3.down * 2f, ForceMode.Impulse);
        }
        
        // Check if the cat is in a PetAreaController zone
        var catController = cat.GetComponent<CatController>();
        if (catController != null)
        {
            // Find all PetAreaControllers and check if cat is inside any FIRST
            Collider[] overlaps = Physics.OverlapSphere(cat.transform.position, 5f);
            
            foreach (var overlap in overlaps)
            {
                var petArea = overlap.GetComponent<PetAreaController>();
                if (petArea != null && overlap.bounds.Contains(cat.transform.position))
                {
                    catController.SetInsideArea(petArea);
                    break;
                }
            }
            
            // Notify the cat controller that it's been released (after setting area)
            catController.OnSelectExit();
        }
        
        // Clear reference
        cat = null;
        catRigidbody = null;
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
            // Jump off wall - limit the upward force to prevent infinite speed
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); // Reset vertical velocity
            rb.AddForce(Vector3.up * jumpForce * 1.2f, ForceMode.Impulse);
        }
        else
        {
            // Normal jump - reset vertical velocity first
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    // IEnumerator AddForwardForceAfterDelay(float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     rb.AddForce(transform.forward * jumpForce * 1.2f, ForceMode.Impulse);
    // }
}
