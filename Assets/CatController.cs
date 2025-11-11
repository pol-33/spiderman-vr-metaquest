using UnityEngine;

public class CatController : MonoBehaviour
{
    public PetAreaController area;
    private Rigidbody rb;
    public float moveSpeed = 1.5f;
    private Vector3 moveDir = Vector3.zero;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Ensure rigidbody is properly configured for triggers
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public void SetInsideArea(PetAreaController petArea)
    {
        area = petArea;
        
        // Only set initial direction if entering an area
        if (area != null)
        {
            Vector3 randomTarget = area.GetRandomPointInside();
            moveDir = (randomTarget - transform.position).normalized;
        }
        else
        {
            // Stop moving when leaving the area
            moveDir = Vector3.zero;
        }
    }

    private void Update()
    {
        if (area != null && !isGrabbed)
        {
            StayInsideArea();
        }
        else if (area != null && isGrabbed)
        {
            // Check if we've been taken outside the area while grabbed
            var bounds = area.GetComponent<Collider>().bounds;
            if (!bounds.Contains(transform.position))
            {
                // We've left the area while being grabbed
                area = null;
                moveDir = Vector3.zero;
            }
        }
    }

    bool isGrabbed = false;

    public void OnSelectEnter()
    {
        isGrabbed = true;
        // Stop movement while grabbed
        moveDir = Vector3.zero;
    }

    public void OnSelectExit()
    {
        isGrabbed = false;
        
        // If we're inside an area, reinitialize movement
        if (area != null)
        {
            Vector3 randomTarget = area.GetRandomPointInside();
            moveDir = (randomTarget - transform.position).normalized;
        }
    }

    void StayInsideArea()
    {
        var bounds = area.GetComponent<Collider>().bounds;
        Vector3 pos = transform.position;

        // If outside bounds, gently move it back toward center
        if (!bounds.Contains(pos))
        {
            Vector3 center = bounds.center;
            Vector3 dir = (center - pos).normalized;
            moveDir = dir; // Update move direction
            rb.MovePosition(pos + dir * moveSpeed * Time.deltaTime);
        }
        else
        {
            // Random wandering inside
            if (Random.value < 0.01f) // 1% chance per frame to pick new direction
            {
                Vector3 randomTarget = area.GetRandomPointInside();
                moveDir = (randomTarget - transform.position).normalized;
            }
            
            // Always move in the current direction
            rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
        }
    }
}
