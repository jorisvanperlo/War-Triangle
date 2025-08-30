using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogoAnimation : MonoBehaviour
{
    [Header("References")]
    public GameObject[] letters;
    public Camera mainCamera;
    public GameObject mainUIHold;

    [Header("Letter Settings")]
    public float moveDistance = 5f;
    public float moveDuration = 1f;
    public float staggerDelay = 0.2f;

    [Header("Shake Settings")]
    public float shakeMagnitude = 0.1f;
    public float extraShakeTime = 0.3f;

    private Vector3 originalCamPos;
    private bool isShaking = false;
    private bool skipped = false;

    private Vector3[] startPositions;
    private Vector3[] endPositions;

    private List<Coroutine> runningCoroutines = new List<Coroutine>();

    private void Start()
    {
        Cursor.visible = false;

        if (mainCamera == null) mainCamera = Camera.main;
        originalCamPos = mainCamera.transform.localPosition;

        // Cache positions
        startPositions = new Vector3[letters.Length];
        endPositions = new Vector3[letters.Length];
        for (int i = 0; i < letters.Length; i++)
        {
            if (letters[i] == null) continue;
            startPositions[i] = letters[i].transform.position;
            endPositions[i] = startPositions[i] + Vector3.forward * moveDistance;
        }

        runningCoroutines.Add(StartCoroutine(ShakeWithLetterDrop()));
        mainUIHold.SetActive(false);
    }

    private void Update()
    {
        if (!skipped && Input.GetMouseButtonDown(0))
        {
            SkipAnimation();
        }
    }

    private IEnumerator ShakeWithLetterDrop()
    {
        for (int i = 0; i < letters.Length; i++)
        {
            if (letters[i] != null)
            {
                runningCoroutines.Add(StartCoroutine(MoveLetterZ(i, i * staggerDelay)));
            }
        }

        yield return new WaitForSeconds(moveDuration);
        if (skipped) yield break;

        isShaking = true;
        runningCoroutines.Add(StartCoroutine(ShakeCamera()));

        yield return new WaitForSeconds((letters.Length - 1) * staggerDelay + extraShakeTime);
        if (skipped) yield break;

        isShaking = false;
        mainCamera.transform.localPosition = originalCamPos;

        mainUIHold.SetActive(true);
        Cursor.visible = true;
    }

    private IEnumerator MoveLetterZ(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (skipped) yield break;

        Transform t = letters[index].transform;
        Vector3 start = startPositions[index];
        Vector3 end = endPositions[index];

        float elapsed = 0f;
        while (elapsed < moveDuration && !skipped)
        {
            float tNorm = elapsed / moveDuration;
            t.position = Vector3.Lerp(start, end, tNorm);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!skipped)
            t.position = end;
    }

    private IEnumerator ShakeCamera()
    {
        while (isShaking && !skipped)
        {
            Vector3 randomOffset = Random.insideUnitSphere * shakeMagnitude;
            mainCamera.transform.localPosition = originalCamPos + randomOffset;
            yield return null;
        }
    }

    private void SkipAnimation()
    {
        if (skipped) return;
        skipped = true;

        // Stop everything that was moving
        foreach (var c in runningCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        runningCoroutines.Clear();

        // Reset camera
        isShaking = false;
        mainCamera.transform.localPosition = originalCamPos;

        // Snap all letters to their final positions (no +=)
        for (int i = 0; i < letters.Length; i++)
        {
            if (letters[i] != null)
                letters[i].transform.position = endPositions[i];
        }

        // Show UI
        mainUIHold.SetActive(true);
        Cursor.visible = true;
    }
}