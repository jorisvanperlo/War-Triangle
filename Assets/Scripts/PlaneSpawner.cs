using UnityEngine;

public class PlaneSpawner : MonoBehaviour
{
    public GameObject[] planes;
    public Vector3[] planeSpawn;
    private Vector3 curPlaneSpawn;

    public Vector3[] planeRot;
    private Vector3 curPlaneRot;

    public int defaultPlane;
    void Start()
    {
        

        int index = ButtonInteractions.chosenPlane - 1;
        if (index < 0) { index = defaultPlane; }

        curPlaneSpawn = planeSpawn[index];
        curPlaneRot = planeRot[index];

        Instantiate(
            planes[index],
            curPlaneSpawn,
            Quaternion.Euler(curPlaneRot)
        );
    }
}
