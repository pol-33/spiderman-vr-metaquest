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

    private Color[] originalCatColors = null;
    private Renderer[] catRenderers = null;
    private bool isHighlighted = false;
    
    // Location beacon
    public GameObject locationBeacon; // Assign a cylinder prefab in Inspector
    private GameObject activeBeacon;
    private bool beaconActive = false;

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
        
        // Save original colors
        SaveOriginalColors();
    }
    
    private void SaveOriginalColors()
    {
        catRenderers = GetComponentsInChildren<Renderer>();
        originalCatColors = new Color[catRenderers.Length];
        
        for (int i = 0; i < catRenderers.Length; i++)
        {
            originalCatColors[i] = catRenderers[i].material.color;
        }
    }
    
    public void HighlightCat()
    {
        if (isHighlighted || catRenderers == null) return;
        
        isHighlighted = true;
        for (int i = 0; i < catRenderers.Length; i++)
        {
            catRenderers[i].material.color = originalCatColors[i] * 2.8f; // Brighten by 180%
        }
    }
    
    public void RestoreColors()
    {
        if (!isHighlighted || catRenderers == null || originalCatColors == null) return;
        
        isHighlighted = false;
        for (int i = 0; i < catRenderers.Length && i < originalCatColors.Length; i++)
        {
            catRenderers[i].material.color = originalCatColors[i];
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
            
            // Hide beacon when entering area
            HideBeacon();
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
            
            // Show beacon when outside area (if beacons are enabled)
            if (beaconActive)
            {
                ShowBeacon();
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
    
    // Public method to set beacon state (called by AllCatsController)
    public void SetBeaconState(bool enabled)
    {
        beaconActive = enabled;
        
        // Only show beacon if cat is NOT in an area and not grabbed
        if (enabled && area == null && !isGrabbed)
        {
            ShowBeacon();
        }
        else
        {
            HideBeacon();
        }
    }
    
    private void ShowBeacon()
    {
        if (locationBeacon != null && activeBeacon == null)
        {
            // Instantiate beacon at cat's position, offset upward
            Vector3 beaconPosition = transform.position + Vector3.up * 10f; // 10 units above cat
            activeBeacon = Instantiate(locationBeacon, beaconPosition, Quaternion.identity);
            activeBeacon.transform.SetParent(transform); // Parent to cat so it follows
            activeBeacon.transform.localPosition = new Vector3(0, 10f, 0); // Local offset
        }
        else if (activeBeacon != null)
        {
            activeBeacon.SetActive(true);
        }
    }
    
    private void HideBeacon()
    {
        if (activeBeacon != null)
        {
            Destroy(activeBeacon);
            activeBeacon = null;
        }
    }

    
}
