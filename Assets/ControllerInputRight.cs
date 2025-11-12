using Oculus.Interaction;
using UnityEngine;
using System.Collections;
using System;


public class ControllerInputRight : MonoBehaviour
{
    public GameObject vrCamParent;
    public GameObject vrEye;
    public float moveSpeed = 5.0f;
    public GameObject predictionPoint;
    public LineRenderer lineRenderer;
    public float jumpForce = 10f;

    public CustomLocomotionProvider locomotionProvider;
    
    // Audio
    public AudioClip webShootSound;
    private AudioSource audioSource;

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
    public float groundCheckDistance = 5f;

    public float pullSpeed = 500;
    public float strafeSpeed = 50;
    public float maxAirSpeed = 20f; // Maximum speed in the air
    public float maxSwingReleaseSpeed = 25f; // Maximum speed when releasing from swing


    public float maxDistance = 0;
    public float pullStrength = 10;

    private bool turning = false;
    private float savedAngle = 0f;
    
    private GameObject lastHighlightedCat = null;

    bool speedCap = true;

    private void Start()
    {
        Application.targetFrameRate = 120;
        OVRManager.display.displayFrequency = 120.0f;
        rb = vrCamParent.GetComponent<Rigidbody>();
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource for 3D spatial audio
        audioSource.spatialBlend = 1.0f; // 1.0 = fully 3D
        audioSource.volume = 0.2f; // Adjust volume as needed
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        isGrounded = CheckGrounded();

        // A Button Right (OVRInput.Button.One) - Jump 
        if (OVRInput.GetDown(OVRInput.Button.One) && isGrounded)
        {
            Jump();
        }

        // Trigger
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);

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

            grabCat();
        }
        else
        {
            hasPointed = selectWebPoint();
            stopSwing();
            delLine();
            moveThumb();

            selectCat();
        }

        if (cat != null)
        {
            cat.transform.position = transform.position;
            cat.transform.rotation = transform.rotation;
        }
        
        float grabValue = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);
        if(grabValue > 0.6 && cat != null)
        {
            DropCat();
        }

        if (grabValue > 0.6 && cat == null)
        {
            if (!turning)
            {
                Vector2 contVec = new Vector2(transform.localPosition.x, transform.localPosition.z);
                float newAngle = Vector2.Angle(contVec, new Vector2(1, 0));
                //if (newAngle > 5) newAngle = 0;
                if ((contVec).y < 0) newAngle = -newAngle;
                savedAngle = newAngle;
                turning = true;
                Debug.Log("rotating");
            }
            turnCamera();
        }
        else if (grabValue < 0.6 && cat == null)
        {
            if (turning)
            {
                turning = false;
            }

        }

        if(speedCap)
        {
            Vector3 velocity = rb.linearVelocity;
            if (velocity.magnitude > maxAirSpeed)
            {
                rb.linearVelocity = velocity.normalized * maxAirSpeed;
            }
        }

        // Update locomotion provider state (trigger the tunneling vignette)
        UpdateLocomotionProvider();
    }

    void UpdateLocomotionProvider()
    {
        // Check if moving via thumbstick input
        bool thumbstickMoving = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).magnitude > 0.1f || (turning);
        
        // Check if player has significant velocity (moving from momentum/swing/jump)
        bool hasVelocity = rb.linearVelocity.magnitude > 2f;
        
        // Show vignette if: moving with thumbstick, currently swinging, OR has significant velocity (in air or on ground)
        bool isMovingNow = thumbstickMoving || joint != null || hasVelocity;

        if (isMovingNow != wasMoving)
        {
            locomotionProvider.SetMoving(isMovingNow, false); // false = right controller
            wasMoving = isMovingNow;
        }
    }

    void moveThumb()
    {
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

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

                if (Mathf.Abs(horizontalVelocity.x) < maxAirSpeed)
                {
                    rb.AddForce(new Vector3(direction.x, 0, 0) * strafeSpeed * 10f * Time.deltaTime, ForceMode.Acceleration);
                }
                if (Mathf.Abs(horizontalVelocity.z) < maxAirSpeed)
                {
                    rb.AddForce(new Vector3(0, 0, direction.z) * strafeSpeed * 10f * Time.deltaTime, ForceMode.Acceleration);
                    // Clamp the horizontal velocity to max air speed
                    //Vector3 clampedVelocity = horizontalVelocity.normalized * maxAirSpeed;
                    //rb.linearVelocity = new Vector3(clampedVelocity.x, rb.linearVelocity.y, clampedVelocity.z);
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
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
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
                joint.maxDistance = distance * 1.1f;
                joint.minDistance = 0;//distance * 1.2f;
            }
            // The distance grapple will try to keep from grapple point. 
            
        }
    }

    void grabCat()
    {
        if (cat != null) return; // Already holding a cat
        
        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Cat");
        Vector3 start = transform.position + transform.forward * 0.2f;
        bool hasHit = Physics.Raycast(start, transform.forward, out hit, 100, layerMask);
        if (hasHit)
        {
            // Restore colors of previously highlighted cat
            if (lastHighlightedCat != null)
            {
                var lastCatController = lastHighlightedCat.GetComponent<CatController>();
                if (lastCatController != null)
                {
                    lastCatController.RestoreColors();
                }
                lastHighlightedCat = null;
            }
            
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
                
                // Manually remove from area since kinematic rigidbodies don't trigger OnTriggerExit
                if (catController.area != null)
                {
                    catController.area.RemoveCat(catController);
                }
            }
        }
    }

    // We indicate that the cat is selectable by changing its color
    void selectCat()
    {
        if (cat != null) return; // Already holding a cat
        
        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Cat");
        Vector3 start = transform.position + transform.forward * 0.2f;
        bool hasHit = Physics.Raycast(start, transform.forward, out hit, 100, layerMask);
        
        if (hasHit)
        {
            var catObject = hit.collider.gameObject;
            
            // If this is a different cat than before, restore the previous cat's colors
            if (lastHighlightedCat != null && lastHighlightedCat != catObject)
            {
                var lastCatController = lastHighlightedCat.GetComponent<CatController>();
                if (lastCatController != null)
                {
                    lastCatController.RestoreColors();
                }
            }
            
            // If this is a new cat to highlight, save and brighten its colors
            if (lastHighlightedCat != catObject)
            {
                lastHighlightedCat = catObject;
                
                var catController = catObject.GetComponent<CatController>();
                if (catController != null)
                {
                    catController.HighlightCat();
                }
            }
        }
        else
        {
            // No cat in sight, restore colors if needed
            if (lastHighlightedCat != null)
            {
                var lastCatController = lastHighlightedCat.GetComponent<CatController>();
                if (lastCatController != null)
                {
                    lastCatController.RestoreColors();
                }
                lastHighlightedCat = null;
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
        
        // Get cat controller before checking areas
        var catController = cat.GetComponent<CatController>();
        
        // Check if the cat is in a PetAreaController zone
        if (catController != null)
        {
            // Find all PetAreaControllers and check if cat is inside any FIRST
            Collider[] overlaps = Physics.OverlapSphere(cat.transform.position, 5f);
            
            bool foundArea = false;
            foreach (var overlap in overlaps)
            {
                var petArea = overlap.GetComponent<PetAreaController>();
                if (petArea != null && overlap.bounds.Contains(cat.transform.position))
                {
                    catController.SetInsideArea(petArea);
                    // Manually add to area since kinematic->non-kinematic transition may not trigger OnTriggerEnter
                    petArea.AddCat(catController);
                    foundArea = true;
                    break;
                }
            }
            
            // If not in any area, make sure it's cleared
            if (!foundArea)
            {
                catController.SetInsideArea(null);
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
            predictionPoint.SetActive(true);
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

        // Play web shoot sound
        if (webShootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(webShootSound);
        }

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
        if (joint != null)
        {
            // Clamp velocity when releasing from swing to prevent excessive speed
            Vector3 velocity = rb.linearVelocity;
            if (velocity.magnitude > maxSwingReleaseSpeed)
            {
                //rb.linearVelocity = velocity.normalized * maxSwingReleaseSpeed;
            }
        }
        
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
            
            // Clamp horizontal velocity after jump
            ClampHorizontalVelocity();
        }
        else
        {
            // Normal jump - reset vertical velocity first
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            
            // Clamp horizontal velocity after jump
            ClampHorizontalVelocity();
        }
    }

    void ClampHorizontalVelocity()
    {
        // Clamp horizontal velocity to prevent excessive speed after jumping
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        if (horizontalVelocity.magnitude > maxAirSpeed)
        {
            Vector3 clampedVelocity = horizontalVelocity.normalized * maxAirSpeed;
            rb.linearVelocity = new Vector3(clampedVelocity.x, rb.linearVelocity.y, clampedVelocity.z);
        }
    }
    void turnCamera()
    {
        Vector2 contVec = new Vector2(transform.localPosition.x, transform.localPosition.z);
        float newAngle = Vector2.Angle(contVec, new Vector2(1, 0));
        //if (newAngle > 5) newAngle = 0;
        if ((contVec).y < 0) newAngle = -newAngle;
        float rotationAngle = newAngle - savedAngle;

        Debug.Log(rotationAngle);

        vrCamParent.transform.eulerAngles = vrCamParent.transform.eulerAngles + new Vector3(0, rotationAngle, 0);
        savedAngle = newAngle;
    }

    // IEnumerator AddForwardForceAfterDelay(float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     rb.AddForce(transform.forward * jumpForce * 1.2f, ForceMode.Impulse);
    // }

    public void toggleSpeedCap()
    {
        speedCap = !speedCap;
    }
}
