using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialSelection : MonoBehaviour
{   
    [Range (1, 12)]
    public int numberOfRadialParts;
    public GameObject radialPartPrefab;
    public Transform radialPartCanvas;
    public float angleBetweenParts = 10;

    public Transform handTransform;

    private List<GameObject> spawnedParts = new List<GameObject>();
    private int currentSelectedPartIndex = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SpawnRadialPart();
        GetSelectedPart();
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
        
        currentSelectedPartIndex = (int) angle * numberOfRadialParts / 360;

        for (int i = 0; i < spawnedParts.Count; i++)
        {
            if (i == currentSelectedPartIndex)
            {
                // Highlight selected part
                spawnedParts[i].GetComponent<Image>().color = Color.yellow;
                spawnedParts[i].transform.localScale = Vector3.one * 1.1f;
            }
            else
            {
                // Reset color for non-selected parts
                spawnedParts[i].GetComponent<Image>().color = Color.white;
                spawnedParts[i].transform.localScale = Vector3.one;
            }
        }
    }

    public void SpawnRadialPart()
    {
        foreach (GameObject part in spawnedParts)
        {
            Destroy(part);
        }

        spawnedParts.Clear();

        for (int i = 0; i < numberOfRadialParts; i++)
        {
            float angle = - i * (360f / numberOfRadialParts) - angleBetweenParts / 2;
            Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);

            GameObject spawnedRadialPart = Instantiate(radialPartPrefab, radialPartCanvas);
            spawnedRadialPart.transform.position = radialPartCanvas.position;
            spawnedRadialPart.transform.localEulerAngles = radialPartEulerAngle;

            spawnedRadialPart.GetComponent<Image>().fillAmount = (1 / (float)numberOfRadialParts) - (angleBetweenParts / 360);

            spawnedParts.Add(spawnedRadialPart);
        }
    }
}
