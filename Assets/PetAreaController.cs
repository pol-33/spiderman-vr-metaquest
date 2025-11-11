using System.Collections.Generic;
using UnityEngine;

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
}
