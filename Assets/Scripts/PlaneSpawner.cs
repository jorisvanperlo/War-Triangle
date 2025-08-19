using UnityEngine;

public class PlaneSpawner : MonoBehaviour
{
    public GameObject[] Planes;
    void Start()
    {
        Instantiate(Planes[ButtonInteractions.chosenPlane - 1], new Vector3(0,75,0), Quaternion.identity);
    }
}
