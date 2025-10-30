using Oculus.Interaction;
using UnityEngine;

public class ControllerInputRight : MonoBehaviour
{
    public GameObject vrCamParent;
    public GameObject vrEye;
    public float moveSpeed = 8.0f;
    public GameObject predictionPoint;
    public LineRenderer lineRenderer;

    private Vector3 swingPoint;
    private Rigidbody rb;
    private SpringJoint joint;
    private float distance;

    private Vector3 targetPoint; // world-space target point for the web hit
    private bool hasPointed;

    private void Start()
    {
        rb = vrCamParent.GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Trigger
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);

        if (triggerValue > 0.1f)
        {
            // Debug.Log($"Trigger: {triggerValue}");
            if (hasPointed)
            {
                startSwing();
                drawLine();
                predictionPoint.SetActive(false);
                moveLine();
            }
            else
            {
                hasPointed = selectWebPoint();
            }
        }
        else
        {
            hasPointed = selectWebPoint();
            stopSwing();
            delLine();
            moveThumb();
        }


        // Controller position and rotation
        //Vector3 position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        //Debug.Log($"ControllerPosition: {position}");

        //Quaternion rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
        //Debug.Log($"ControllerRotation: {rotation}");
    }
    void moveThumb()
    {
        // Thumbstick
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (thumbstick.magnitude > 0.1f)
        {
            Vector3 movement = (thumbstick.y * vrEye.transform.forward + thumbstick.x * vrEye.transform.right) * moveSpeed * Time.deltaTime;
            //vrCamParent.transform.Translate(movement);
            Vector3 Pos = rb.position + movement;
            rb.MovePosition(Pos);  

        }
    }
    void moveLine()
    {
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (thumbstick.magnitude > 0.1f)
        {
            // use stored world-space targetPoint so it doesn't move with the controller
            Vector3 direction = targetPoint - rb.position;
            rb.MovePosition(rb.position + direction * thumbstick.y * 1 * Time.deltaTime);

            distance = Vector3.Distance(rb.position, swingPoint);

            // The distance grapple will try to keep from grapple point. 
            joint.maxDistance = distance;
        }
    }

    // Get swing point
    bool selectWebPoint()
    {
        RaycastHit hit;
        bool hasHit = Physics.Raycast(transform.position, transform.forward, out hit, 50);
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
            targetPoint = transform.position + transform.forward * 50;
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
        joint.damper = 7f;
        joint.massScale = 4.5f;
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
}
