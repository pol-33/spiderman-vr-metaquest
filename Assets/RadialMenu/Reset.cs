using TMPro;
using UnityEngine;

public class Reset : MonoBehaviour
{
    public Vector3 targetPosition = new Vector3(0, 50, 0);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void resetCameraPos()
    {
        // Immediately move the camera rig to the target position
        transform.position = targetPosition;
    }
}
