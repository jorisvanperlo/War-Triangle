using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CloudPlacerEditor : EditorWindow
{
    public List<GameObject> cloudPrefabs = new List<GameObject>();
    public int count = 1000;
    public Vector3 areaSize = new Vector3(500, 100, 500);
    public Vector3 areaCenter = Vector3.zero;
    public float minScale = 0.5f;
    public float maxScale = 2.0f;

    [MenuItem("Tools/Cloud Placer")]
    public static void ShowWindow()
    {
        GetWindow<CloudPlacerEditor>("Cloud Placer");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Cloud Prefabs", EditorStyles.boldLabel);

        int newSize = Mathf.Max(0, EditorGUILayout.IntField("Number of Prefabs", cloudPrefabs.Count));
        while (newSize > cloudPrefabs.Count)
            cloudPrefabs.Add(null);
        while (newSize < cloudPrefabs.Count)
            cloudPrefabs.RemoveAt(cloudPrefabs.Count - 1);

        for (int i = 0; i < cloudPrefabs.Count; i++)
        {
            cloudPrefabs[i] = (GameObject)EditorGUILayout.ObjectField($"Prefab {i + 1}", cloudPrefabs[i], typeof(GameObject), false);
        }

        EditorGUILayout.Space();

        count = EditorGUILayout.IntField("Cloud Count", count);
        areaCenter = EditorGUILayout.Vector3Field("Area Center", areaCenter);
        areaSize = EditorGUILayout.Vector3Field("Area Size", areaSize);
        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);

        if (GUILayout.Button("Place Clouds"))
        {
            PlaceClouds();
        }
    }

    void PlaceClouds()
    {
        if (cloudPrefabs.Count == 0 || cloudPrefabs.Exists(p => p == null) == false)
        {
            GameObject cloudParent = new GameObject("Clouds");

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Count)];

                Vector3 randomPos = areaCenter + new Vector3(
                    Random.Range(-areaSize.x / 2, areaSize.x / 2),
                    Random.Range(-areaSize.y / 2, areaSize.y / 2),
                    Random.Range(-areaSize.z / 2, areaSize.z / 2)
                );

                GameObject cloud = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                cloud.transform.position = randomPos;

                float randomScale = Random.Range(minScale, maxScale);
                cloud.transform.localScale = Vector3.one * randomScale;
                cloud.transform.parent = cloudParent.transform;
                cloud.isStatic = true;
            }

            Debug.Log($"{count} clouds placed.");
        }
        else
        {
            Debug.LogWarning("Please assign all prefab slots.");
        }
    }
}