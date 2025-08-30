using UnityEngine;

public class CollisionInteractions : MonoBehaviour
{
    public GameObject deathExplosion;

    public Rigidbody rb;
    public float kinEnergy;
    private bool dead;

    void Start()
    {
        deathExplosion = GameObject.Find("DeathExplosion");
        deathExplosion.SetActive(false);

        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision col)
    {
        // kinetic energy calculation
        kinEnergy = 0.5f * rb.mass * rb.linearVelocity.sqrMagnitude;

        // did *my own* BoxCollider make contact?
        bool myBoxColliderHit = false;
        foreach (ContactPoint contact in col.contacts)
        {
            if (contact.thisCollider is BoxCollider)
            {
                myBoxColliderHit = true;
                break;
            }
        }

        // condition check
        if ((!dead && kinEnergy > 5000000f)
            || (!dead && col.gameObject.CompareTag("Ocean"))
            || (!dead && myBoxColliderHit))
        {
            // set all child materials to grey
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.material.color = Color.grey;
            }

            foreach (var prop in GetComponent<Flightcontroller>().fakeProplers)
            {
                prop.SetActive(false);
            }

            deathExplosion.SetActive(true);
            dead = true;

            GetComponent<Flightcontroller>().enabled = false;

            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.0f;
            rb.mass *= 3f;
        }
    }

    private void Update()
    {
        if (dead)
        {
            deathExplosion.transform.position = transform.position;
        }
    }
}