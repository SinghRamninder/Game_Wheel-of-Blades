using UnityEngine;

public class WordGenerator : MonoBehaviour
{
    [SerializeField] private GameObject wheel;
    [SerializeField] private GameObject lastPlane;
    [SerializeField] private GameObject plane;
    [SerializeField] private float planeSpawnDistance = 18f;

    private void Update()
    {
        Renderer lastPlaneRenderer = lastPlane.GetComponent<Renderer>();

        float lastPlaneLength = lastPlaneRenderer.bounds.size.x;

        float lastPlaneEnd = lastPlane.transform.position.x + lastPlaneLength / 2f;

        if (wheel.transform.position.x + planeSpawnDistance > lastPlaneEnd)
        {
            CreatePlatformChunk();
        }
    }

    private void CreatePlatformChunk()
    {
        Renderer renderer = lastPlane.GetComponent<Renderer>();

        float planeLength = renderer.bounds.size.x;

        Vector3 spawnPosition = lastPlane.transform.position + lastPlane.transform.right * planeLength;

        GameObject spawnPlane = Instantiate(plane, spawnPosition, lastPlane.transform.rotation);

        lastPlane = spawnPlane;
    }
}
