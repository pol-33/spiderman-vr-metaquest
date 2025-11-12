using UnityEngine;

public class AllCatsController : MonoBehaviour
{
    private CatController[] allCats;
    private bool beaconsEnabled = false;

    void Start()
    {
        // Get all cat controllers (children of this GameObject)
        allCats = GetComponentsInChildren<CatController>();
        Debug.Log($"Found {allCats.Length} cats");
    }

    // Toggle beacons for all cats
    public void ToggleBeacons()
    {
        beaconsEnabled = !beaconsEnabled;
        UpdateBeacons(beaconsEnabled);
        Debug.Log($"Beacons {(beaconsEnabled ? "enabled" : "disabled")}");
    }
    
    // Enable beacons
    public void EnableBeacons()
    {
        beaconsEnabled = true;
        UpdateBeacons(true);
    }
    
    // Disable beacons
    public void DisableBeacons()
    {
        beaconsEnabled = false;
        UpdateBeacons(false);
    }
    
    private void UpdateBeacons(bool enabled)
    {
        foreach (var cat in allCats)
        {
            if (cat != null)
            {
                cat.SetBeaconState(enabled);
            }
        }
    }
}
