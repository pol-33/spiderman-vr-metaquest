using UnityEngine;
using UnityEngine.Events;

public class FireworkToggle : MonoBehaviour
{
    public GameObject fireworkObject;

    void Start()
    {
        fireworkObject.SetActive(false);
    }
    
    public void ToggleFirework()
    {
        fireworkObject.SetActive(!fireworkObject.activeSelf);
    }
    
    public void ShowFirework()
    {
        fireworkObject.SetActive(true);
    }
    
    public void HideFirework()
    {
        fireworkObject.SetActive(false);
    }
}