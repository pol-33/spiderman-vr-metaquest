using UnityEngine;

public class DayNightToggle : MonoBehaviour
{
    public Light directionalLight; // Assign your main directional light in Inspector
    
    [Header("Day Settings")]
    public Color dayAmbientLight = new Color(0.7f, 0.7f, 0.7f);
    public float dayLightIntensity = 1f;
    public Color dayLightColor = Color.white;
    public Material daySkybox; // Assign your day skybox material
    
    [Header("Night Settings")]
    public Color nightAmbientLight = new Color(0.1f, 0.1f, 0.15f); // Slight blue tint
    public float nightLightIntensity = 0.5f;
    public Color nightLightColor = new Color(0.6f, 0.6f, 0.8f); // Moonlight color
    public Material nightSkybox; // Assign your night skybox material
    
    private bool isDay = true;

    void Start()
    {
        // If no light assigned, try to find the main directional light
        if (directionalLight == null)
        {
            directionalLight = FindFirstObjectByType<Light>();
        }

        // Initialize to day settings
        isDay = !isDay;
        ToggleDayNight();
    }

    public void ToggleDayNight()
    {
        isDay = !isDay;
        
        if (isDay)
        {
            // Switch to day
            RenderSettings.ambientLight = dayAmbientLight;
            if (directionalLight != null)
            {
                directionalLight.intensity = dayLightIntensity;
                directionalLight.color = dayLightColor;
            }
            if (daySkybox != null)
            {
                RenderSettings.skybox = daySkybox;
            }
        }
        else
        {
            // Switch to night
            RenderSettings.ambientLight = nightAmbientLight;
            if (directionalLight != null)
            {
                directionalLight.intensity = nightLightIntensity;
                directionalLight.color = nightLightColor;
            }
            if (nightSkybox != null)
            {
                RenderSettings.skybox = nightSkybox;
            }
        }
        
        // Update the skybox reflection
        DynamicGI.UpdateEnvironment();
    }
}