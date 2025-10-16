using UnityEngine;

public class ControllerInputRight : MonoBehaviour
{
    public GameObject vrCamParent;
    public GameObject vrEye;
    public float moveSpeed = 2.0f;
    public GameObject prediction;
    public LineRenderer lineRenderer;

    private bool hasPointed;
    void Update()
    {
        moveThumb();

        // Trigger
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);

        if (triggerValue > 0.1f)
        {
            Debug.Log($"Trigger: {triggerValue}");
            if (hasPointed)
            {
                drawLine();
                prediction.SetActive(false);
            }
            else
            {
                selectWebPoint();
            }
        }
        else
        {
            hasPointed = selectWebPoint();
            delLine();
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
            vrCamParent.transform.Translate(movement);
        }
    }
    bool selectWebPoint()
    {
        RaycastHit hit;
        bool hasHit = Physics.Raycast(transform.position, transform.forward, out hit, 50);
        if (hasHit)
        {
            Debug.Log("Hit: " + hit.collider.name);
            Vector3 swingPoint = hit.point;
            prediction.SetActive(true);
            prediction.transform.position = swingPoint;
            prediction.GetComponent<Renderer>().material.color = Color.yellow;
        }
        else
        {
            prediction.transform.position = transform.position + transform.forward * 50;
            prediction.GetComponent<Renderer>().material.color = Color.red;
        }
        return hasHit;
    }
    void drawLine()
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, prediction.transform.position);
    }
    void delLine()
    {
        lineRenderer.enabled = false;
    }
}
