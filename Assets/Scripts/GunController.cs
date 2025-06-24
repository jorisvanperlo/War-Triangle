using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    public GameObject bulletPrefab;           // Bullet prefab
    public float shootForce;                  // Force applied to bullets
    public float rpm;                         // Rounds per minute (controls fire rate)
    public float spread;                      // Spread angle in degrees
    public float reloadTime;                  // Time to reload the weapon
    public int magazineSize;                  // Ammo per magazine
    public Transform[] bulletSpawnPoints;     // All gun barrels
    public int poolSize = 50;                 // Size of bullet object pool

    private bool canShoot = true;
    private bool isReloading = false;
    private float nextFireTime = 0f;
    private int currentAmmo;
    private Queue<GameObject> bulletPool = new();

    public float bulletLifeTime;

    private Rigidbody rb;

    //MuzzleFlash
    public GameObject[] muzzleFlashes;
    public float flashDuration = 0.2f;

    void Start()
    {
        currentAmmo = magazineSize;
        rb = GetComponent<Rigidbody>();
        // Initialize bullet pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject b = Instantiate(bulletPrefab);
            b.SetActive(false);
            bulletPool.Enqueue(b);
        }
    }

    void FixedUpdate()
    {
        if (!isReloading)
        {
            FireInput();

            // Manual reload
            if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineSize)
            {
                StartCoroutine(Reload());
            }
        }
    }

    private void FireInput()
    {
        if (Input.GetKey(KeyCode.Mouse0) && canShoot && Time.time >= nextFireTime && currentAmmo > 0)
        {
            Shoot();
            currentAmmo--;
            nextFireTime = Time.time + 60f / rpm;

            // Auto reload if out of ammo
            if (currentAmmo <= 0)
            {
                StartCoroutine(Reload());
            }
        }
    }

    private void Shoot()
    {
        // Not enough bullets in pool for a full volley
        if (bulletPool.Count < bulletSpawnPoints.Length) return;

        foreach (Transform spawnPoint in bulletSpawnPoints)
        {
            GameObject bullet = bulletPool.Dequeue();
            bullet.transform.position = spawnPoint.position;


            // Apply spread
            Quaternion spreadRotation = Quaternion.Euler(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                0f
            );

            Vector3 shotDirection = spreadRotation * spawnPoint.forward;
            bullet.transform.rotation = Quaternion.LookRotation(shotDirection);
            bullet.SetActive(true);


            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                Rigidbody shooterRb = GetComponent<Rigidbody>(); // Assuming the shooter has a Rigidbody
                Vector3 inheritedVelocity = shooterRb != null ? shooterRb.linearVelocity : Vector3.zero;
                rb.linearVelocity = inheritedVelocity + shotDirection * shootForce;
            }

            // Disable and return to pool after reloadTime
            StartCoroutine(DisableAfterTime(bullet, bulletLifeTime));
        }
        StartCoroutine(FlashMuzzle());
    }

    private IEnumerator DisableAfterTime(GameObject bullet, float time)
    {
        yield return new WaitForSeconds(time);
        bullet.SetActive(false);
        TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear(); // Reset the trail before showing the bullet again
        }
        bulletPool.Enqueue(bullet);
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = magazineSize;
        isReloading = false;
    }
    private IEnumerator FlashMuzzle()
    {
        foreach (GameObject flash in muzzleFlashes)
        {
            if (flash != null)
                flash.SetActive(true);
        }

        yield return new WaitForSeconds(flashDuration);

        foreach (GameObject flash in muzzleFlashes)
        {
            if (flash != null)
                flash.SetActive(false);
        }
    }
}
