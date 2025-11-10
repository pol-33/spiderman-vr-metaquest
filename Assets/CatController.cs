using UnityEngine;

public class CatController : MonoBehaviour
{
    public PetAreaController area;
    private Rigidbody rb;
    public float moveSpeed = 0.5f;
    private Vector3 moveDir = Vector3.zero;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetInsideArea(PetAreaController petArea)
    {
        area = petArea;
        Vector3 randomTarget = area.GetRandomPointInside();
        moveDir = (randomTarget - transform.position).normalized;
        rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
    }

    private void Update()
    {
        if (area != null && !isGrabbed)
        {
            StayInsideArea();
        }
    }

    bool isGrabbed = false;

    public void OnSelectEnter()
    {
        isGrabbed = true;
    }

    public void OnSelectExit()
    {
        isGrabbed = false;
    }

    void StayInsideArea()
    {
        var bounds = area.GetComponent<Collider>().bounds;
        Vector3 pos = transform.position;

        // If outside, gently move it back
        if (!bounds.Contains(pos))
        {
            Vector3 center = bounds.center;
            Vector3 dir = (center - pos).normalized;
            rb.MovePosition(pos + dir * moveSpeed * Time.deltaTime);
        }
        else
        {
            // Random wandering inside
            if (Random.value < 0.01f)
            {
                Vector3 randomTarget = area.GetRandomPointInside();
                moveDir = (randomTarget - transform.position).normalized;
                
            }
            rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
        }
    }
}
