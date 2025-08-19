using UnityEngine;

public class HitableObjects : MonoBehaviour
{
    public Material hitMaterial;
    private bool hasDropped = false;
    private void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !hasDropped)
        {
            hasDropped = true;

            if (GetComponent<Renderer>() != null && hitMaterial != null)
            {
                GetComponent<Renderer>().material = hitMaterial;
            }
        }
    }
}
