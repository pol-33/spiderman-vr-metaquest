using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class RadialSelectionSubmenu : MonoBehaviour
{
    public OVRInput.Button spawnButton;

    [Range(1, 12)]
    public int numberOfRadialParts;
    public GameObject radialPartPrefab;
    public Transform radialPartCanvas;
    public float angleBetweenParts = 10;

    public Transform handTransform;

    public List<UnityEvent> OnPartSelectedEvents = new List<UnityEvent>();

    public string[] partLabels;
    public RadialSelection parentMenu; // Reference to the main menu
    public TextMeshPro helperText; // Text to show instruction

    private List<GameObject> spawnedParts = new List<GameObject>();
    private int currentSelectedPartIndex = -1;
    private bool isActive = false;

    void Start()
    {
        // Start disabled
        if (radialPartCanvas != null)
        {
            radialPartCanvas.gameObject.SetActive(false);
        }
        
        // Hide helper text initially
        if (helperText != null)
        {
            helperText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Only respond to input when active
        if (!isActive)
        {
            return;
        }

        // Check if button is held
        if (OVRInput.Get(spawnButton))
        {
            GetSelectedPart();
        }

        // Check if button is released
        if (OVRInput.GetUp(spawnButton))
        {
            HideAndTriggerSelected();
        }
    }

    // Called by the main RadialSelection menu to show this submenu
    public void ShowSubmenu()
    {
        isActive = true;
        
        // Notify parent menu that submenu is now active
        if (parentMenu != null)
        {
            parentMenu.SetSubmenuActive(true);
        }
        
        // Show helper text
        if (helperText != null)
        {
            helperText.gameObject.SetActive(true);
            helperText.text = "Manten premut el botó de menú per a continuar amb el procés de selecció";
        }
        
        SpawnRadialPart();
    }

    public void HideAndTriggerSelected()
    {
        // Only invoke if a valid part was selected
        if (currentSelectedPartIndex >= 0 && currentSelectedPartIndex < numberOfRadialParts)
        {
            // Invoke only the specific event for the selected part
            if (currentSelectedPartIndex < OnPartSelectedEvents.Count && OnPartSelectedEvents[currentSelectedPartIndex] != null)
            {
                OnPartSelectedEvents[currentSelectedPartIndex].Invoke();
            }
        }

        radialPartCanvas.gameObject.SetActive(false);
        currentSelectedPartIndex = -1; // Reset selection
        isActive = false; // Deactivate submenu
        
        // Hide helper text
        if (helperText != null)
        {
            helperText.gameObject.SetActive(false);
        }
        
        // Notify parent menu that submenu is now inactive
        if (parentMenu != null)
        {
            parentMenu.SetSubmenuActive(false);
        }
    }

    public void GetSelectedPart()
    {
        Vector3 centerToHand = handTransform.position - radialPartCanvas.position;
        Vector3 centerToHandProjected = Vector3.ProjectOnPlane(centerToHand, radialPartCanvas.forward);

        float angle = Vector3.SignedAngle(radialPartCanvas.up, centerToHandProjected, -radialPartCanvas.forward);

        if (angle < 0)
        {
            angle += 360;
        }

        currentSelectedPartIndex = (int)angle * numberOfRadialParts / 360;

        for (int i = 0; i < spawnedParts.Count; i++)
        {
            if (i == currentSelectedPartIndex)
            {
                // Highlight selected part
                spawnedParts[i].GetComponent<Image>().color = Color.yellow;
                spawnedParts[i].transform.localScale = Vector3.one * 1.1f;

                // Highlight text
                TextMeshProUGUI textComponent = spawnedParts[i].GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.color = Color.yellow;
                }
            }
            else
            {
                // Reset color for non-selected parts
                spawnedParts[i].GetComponent<Image>().color = Color.white;
                spawnedParts[i].transform.localScale = Vector3.one;

                // Reset text color
                TextMeshProUGUI textComponent = spawnedParts[i].GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.color = Color.white;
                }
            }
        }
    }

    public void SpawnRadialPart()
    {
        radialPartCanvas.gameObject.SetActive(true);
        radialPartCanvas.position = handTransform.position + handTransform.forward * 5;
        radialPartCanvas.rotation = handTransform.rotation;

        foreach (GameObject part in spawnedParts)
        {
            Destroy(part);
        }

        spawnedParts.Clear();

        for (int i = 0; i < numberOfRadialParts; i++)
        {
            float angle = -i * (360f / numberOfRadialParts) - angleBetweenParts / 2;
            Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);

            GameObject spawnedRadialPart = Instantiate(radialPartPrefab, radialPartCanvas);
            spawnedRadialPart.transform.position = radialPartCanvas.position;
            spawnedRadialPart.transform.localEulerAngles = radialPartEulerAngle;

            spawnedRadialPart.GetComponent<Image>().fillAmount = (1 / (float)numberOfRadialParts) - (angleBetweenParts / 360);

            // Add text label to the radial part
            TextMeshProUGUI textComponent = spawnedRadialPart.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                if (partLabels != null && i < partLabels.Length && !string.IsNullOrEmpty(partLabels[i]))
                {
                    textComponent.text = partLabels[i];
                }
                else
                {
                    textComponent.text = $"Option {i + 1}"; // Fallback text
                }
                // Configure text alignment and positioning
                textComponent.alignment = TextAlignmentOptions.Center;

                // Position text at a consistent distance from center
                RectTransform textRect = textComponent.GetComponent<RectTransform>();
                textRect.anchoredPosition = new Vector2(130, 130); // Adjust the Y value to position text radially
                textRect.sizeDelta = new Vector2(200, 30); // Set consistent size

                // Reset text rotation to keep it upright
                textComponent.transform.localRotation = Quaternion.Euler(0, 0, -angle);
            }
            spawnedParts.Add(spawnedRadialPart);
        }
    }
}
