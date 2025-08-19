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

    private void Start()
    {
        Cursor.visible = false;

        if (mainCamera == null) mainCamera = Camera.main;
        originalCamPos = mainCamera.transform.localPosition;
        StartCoroutine(ShakeWithLetterDrop());
        mainUIHold.SetActive(false);
    }

    private IEnumerator ShakeWithLetterDrop()
    {
        List<Coroutine> moveCoroutines = new List<Coroutine>();

        for (int i = 0; i < letters.Length; i++)
        {
            if (letters[i] != null)
            {
                Coroutine c = StartCoroutine(MoveLetterZ(letters[i].transform, i * staggerDelay));
                moveCoroutines.Add(c);
            }
        }

        // Wait until the first letter lands
        yield return new WaitForSeconds(moveDuration);

        isShaking = true;
        StartCoroutine(ShakeCamera());

        // Wait until the last letter lands
        yield return new WaitForSeconds((letters.Length - 1) * staggerDelay + extraShakeTime);

        isShaking = false;
        mainCamera.transform.localPosition = originalCamPos;


        // turn on rest of the ui
        mainUIHold.SetActive(true);
        Cursor.visible = true;
    }

    private IEnumerator MoveLetterZ(Transform letterTransform, float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector3 startPos = letterTransform.position;
        Vector3 endPos = startPos + Vector3.forward * moveDistance;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            letterTransform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        letterTransform.position = endPos;
    }

    private IEnumerator ShakeCamera()
    {
        while (isShaking)
        {
            Vector3 randomOffset = Random.insideUnitSphere * shakeMagnitude;
            mainCamera.transform.localPosition = originalCamPos + randomOffset;
            yield return null;
        }
    }
}

