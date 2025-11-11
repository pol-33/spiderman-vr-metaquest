using UnityEngine;

public class CatController : MonoBehaviour
{
    public PetAreaController area;
    private Rigidbody rb;
    private Animator animator;
    
    public float walkMoveSpeed = 1f;
    public float runMoveSpeed = 3f;
    private float currentSpeed = 1f;
    private Vector3 moveDir = Vector3.zero;
    
    // Speed switching variables
    private float speedChangeTimer = 0f;
    private float nextSpeedChangeTime = 5f; // Change speed every 5 seconds
    private bool isRunning = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Try to find Animator on this GameObject first, then in children
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // Ensure rigidbody is properly configured for triggers
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        // Start with idle animation (State = 0)
        if (animator != null)
        {
            animator.SetFloat("State", 0f);
            animator.SetFloat("Vert", 0f);
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
            
            // Start walking animation (State = walk/run value)
            if (animator != null && !isGrabbed)
            {
                animator.SetFloat("State", 0.5f);
                animator.SetFloat("Vert", 0.5f);
            }
        }
        else
        {
            // Stop moving when leaving the area
            moveDir = Vector3.zero;
            
            // Idle animation (State = 0)
            if (animator != null)
            {
                animator.SetFloat("State", 0f);
                animator.SetFloat("Vert", 0f);
            }
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
        
        // Play idle animation when grabbed (State = 0)
        if (animator != null)
        {
            animator.SetFloat("State", 0f);
            animator.SetFloat("Vert", 0f);
        }
    }

    public void OnSelectExit()
    {
        isGrabbed = false;
        
        // If we're inside an area, reinitialize movement
        if (area != null)
        {
            Vector3 randomTarget = area.GetRandomPointInside();
            moveDir = (randomTarget - transform.position).normalized;
            
            // Play walking animation when released in area (State = 0.5)
            if (animator != null)
            {
                animator.SetFloat("State", 0.5f);
                animator.SetFloat("Vert", 0.5f);
            }
        }
    }

    void StayInsideArea()
    {
        var bounds = area.GetComponent<Collider>().bounds;
        Vector3 pos = transform.position;
        
        // Randomly switch between walking and running
        speedChangeTimer += Time.deltaTime;
        if (speedChangeTimer >= nextSpeedChangeTime)
        {
            isRunning = !isRunning; // Toggle between walk and run
            currentSpeed = isRunning ? runMoveSpeed : walkMoveSpeed;
            nextSpeedChangeTime = Random.Range(3f, 8f); // Random interval between 3-8 seconds
            speedChangeTimer = 0f;
        }

        // If outside bounds, gently move it back toward center
        if (!bounds.Contains(pos))
        {
            Vector3 center = bounds.center;
            Vector3 dir = (center - pos).normalized;
            moveDir = dir; // Update move direction
            rb.MovePosition(pos + dir * currentSpeed * Time.deltaTime);
            
            // Rotate to face movement direction (Y-axis only to keep cat upright)
            if (moveDir != Vector3.zero)
            {
                Vector3 horizontalDir = new Vector3(moveDir.x, 0, moveDir.z);
                if (horizontalDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(horizontalDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }
            }
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
            rb.MovePosition(transform.position + moveDir * currentSpeed * Time.deltaTime);
            
            // Rotate to face movement direction (Y-axis only to keep cat upright)
            if (moveDir != Vector3.zero)
            {
                Vector3 horizontalDir = new Vector3(moveDir.x, 0, moveDir.z);
                if (horizontalDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(horizontalDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }
            }
        }
        
        // Play walking/running animation based on current speed
        // Animation states based on Blend Tree thresholds:
        // - State/Vert = 0.0  -> Idle (Kitty_001_Idle)
        // - State/Vert = 0.5  -> Walk (Kitty_001_walk) - uses walkMoveSpeed (default 1.0)
        // - State/Vert = 1.0  -> Run  (Kitty_001_run)  - uses runMoveSpeed (default 3.0)
        if (animator != null)
        {
            float animState = isRunning ? 1.0f : 0.5f;
            animator.SetFloat("State", animState);
            animator.SetFloat("Vert", animState);
        }
    }
}
