using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    public ParticleSystem[] FireEmitor;
    private Rigidbody targetRigidbody;
    public TrailRenderer[] wingTrail;

    public float timeMultiplier = 0.1f;
    public float speedMultiplier = 1f;

    void Start()
    {
        if (targetRigidbody == null)
            targetRigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float rbSpeed = targetRigidbody.linearVelocity.magnitude;

        float trailTime = Mathf.Clamp(rbSpeed * timeMultiplier, 0, 0.3f);

        foreach (TrailRenderer trail in wingTrail)
        {
            if (trail != null)
                trail.time = trailTime;
        }
    }
}
