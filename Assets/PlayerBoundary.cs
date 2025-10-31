using UnityEngine;

public class PlayerBoundary : MonoBehaviour
{
    [Header("Boundary Settings")]
    public Vector3 boundaryCenter = Vector3.zero;
    public Vector3 boundarySize = new Vector3(100f, 50f, 100f); // X, Y, Z limits
    
    [Header("Player Reference")]
    public Transform playerTransform; // Assign OVRCameraRig or player root
    
    void Update()
    {
        if (playerTransform == null) return;
        
        Vector3 clampedPosition = playerTransform.position;
        
        // Clamp position within boundaries
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, 
            boundaryCenter.x - boundarySize.x / 2, 
            boundaryCenter.x + boundarySize.x / 2);
            
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, 
            boundaryCenter.y - boundarySize.y / 2, 
            boundaryCenter.y + boundarySize.y / 2);
            
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, 
            boundaryCenter.z - boundarySize.z / 2, 
            boundaryCenter.z + boundarySize.z / 2);
        
        playerTransform.position = clampedPosition;
    }
    
    // Visualize boundary in Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(boundaryCenter, boundarySize);
    }
}