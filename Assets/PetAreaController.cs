using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PetAreaController : MonoBehaviour
{
    private HashSet<CatController> catsInArea = new HashSet<CatController>();

    private void OnTriggerEnter(Collider other)
    {
        // Try to get CatController from the collider or its parent
        var cat = other.GetComponent<CatController>();
        if (cat == null)
        {
            cat = other.GetComponentInParent<CatController>();
        }
        
        if (cat != null)
        {
            catsInArea.Add(cat);
            cat.SetInsideArea(this);
            UpdateCatCounter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Try to get CatController from the collider or its parent
        var cat = other.GetComponent<CatController>();
        if (cat == null)
        {
            cat = other.GetComponentInParent<CatController>();
        }
        
        if (cat != null && catsInArea.Contains(cat))
        {
            catsInArea.Remove(cat);
            cat.SetInsideArea(null);
            UpdateCatCounter();
        }
    }

    public Vector3 GetRandomPointInside()
    {
        // Return a random point within the area bounds
        var bounds = GetComponent<Collider>().bounds;
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.center.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    // Manually add a cat to the area (used when dropping a grabbed cat)
    public void AddCat(CatController cat)
    {
        if (cat != null && !catsInArea.Contains(cat))
        {
            catsInArea.Add(cat);
            UpdateCatCounter();
        }
    }

    // Manually remove a cat from the area (used when grabbing a cat)
    public void RemoveCat(CatController cat)
    {
        if (cat != null && catsInArea.Contains(cat))
        {
            catsInArea.Remove(cat);
            UpdateCatCounter();
        }
    }

    // Update the cat counter text in the PetArea
    private void UpdateCatCounter()
    {
        var textMesh = GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = $"Cats Rescued: {catsInArea.Count}/10";
        }
    }
}
