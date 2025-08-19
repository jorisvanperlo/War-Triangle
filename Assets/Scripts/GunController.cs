using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float shootForce;
    public float rpm;
    public float spread;
    public float reloadTime;
    public int magazineSize;
    public Transform[] bulletSpawnPoints;
    public int poolSize = 50;
    public float bulletLifeTime;

    private bool canShoot = true;
    private bool isReloading = false;
    private float nextFireTime = 0f;
    private int currentAmmo;
    private Queue<GameObject> bulletPool = new();
    private Rigidbody rb;

    public GameObject[] muzzleFlashes;
    public float flashDuration = 0.2f;

    void Start()
    {
        currentAmmo = magazineSize;
        rb = GetComponent<Rigidbody>();

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

            if (currentAmmo <= 0)
            {
                StartCoroutine(Reload());
            }
        }
    }

    private void Shoot()
    {
        if (bulletPool.Count < bulletSpawnPoints.Length) return;

        Vector3 inheritedVelocity = rb != null ? rb.linearVelocity : Vector3.zero;

        foreach (Transform spawnPoint in bulletSpawnPoints)
        {
            GameObject bullet = bulletPool.Dequeue();
            bullet.transform.position = spawnPoint.position;

            Quaternion spreadRotation = Quaternion.Euler(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                0f
            );

            Vector3 shotDirection = spreadRotation * spawnPoint.forward;
            bullet.transform.rotation = Quaternion.LookRotation(shotDirection);
            bullet.SetActive(true);

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = Vector3.zero;
                bulletRb.angularVelocity = Vector3.zero;

                Vector3 advanceOffset = inheritedVelocity * Time.fixedDeltaTime;
                bullet.transform.position += advanceOffset;

                bulletRb.linearVelocity = inheritedVelocity + shotDirection * shootForce;
            }

            TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = false;
                StartCoroutine(EnableTrailDelayed(trail, 0.001f));
            }

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
            trail.Clear();
            trail.emitting = false;
        }

        bulletPool.Enqueue(bullet);
    }

    private IEnumerator EnableTrailDelayed(TrailRenderer trail, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (trail != null && trail.gameObject.activeInHierarchy)
        {
            trail.emitting = true;
        }
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
