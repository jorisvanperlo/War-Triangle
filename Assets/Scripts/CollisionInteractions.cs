using UnityEngine;

public class CollisionInteractions : MonoBehaviour
{

    public GameObject deathExplosion;

    private Material planeMat;
    private Rigidbody rb;

    public float kinEnergy;
    private bool dead;
    void Start()
    {
        deathExplosion = GameObject.Find("DeathExplosion");
        deathExplosion.SetActive(false);
        planeMat = GetComponent<Renderer>().material;
        rb = GetComponent<Rigidbody>();
    }

    public void OnCollisionEnter(Collision col)
    {
        kinEnergy = 0.5f * rb.mass * rb.linearVelocity.sqrMagnitude;
        if (kinEnergy > 5000000f && !dead || col.gameObject.tag == ("Ocean") && !dead)
        {
            planeMat.color = Color.grey;

            deathExplosion.SetActive(true);
            dead = true;

            GetComponent<Flightcontroller>().enabled = false;

            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.0f;
        }
    }

    public void Update()
    {
      if (dead)
        {
            deathExplosion.transform.position = transform.position;
        }

    }
}
