using System.Collections.Generic;
using UnityEngine;

public class WordGenerator : MonoBehaviour
{
    [SerializeField] private GameObject wheel;
    [SerializeField] private GameObject lastStartPlane;
    [SerializeField] private GameObject lastEndPlane;
    [SerializeField] private GameObject plane;
    [SerializeField] private PlatformPool platformPlane;
    [SerializeField] private float planeSpawnDistance = 18f;
    [SerializeField] private float planeDestroyDistance = 20f;

    private List<GameObject> activePlanes = new List<GameObject>();

    private void Start()
    {
        if (!activePlanes.Contains(lastStartPlane))
            activePlanes.Add(lastStartPlane);

        if (!activePlanes.Contains(lastEndPlane))
            activePlanes.Add(lastEndPlane);
    }

    private void Update()
    {
        Renderer lastEndPlaneRenderer = lastEndPlane.GetComponent<Renderer>();
        Renderer lastStartPlaneRenderer = lastStartPlane.GetComponent<Renderer>();

        float lastPlaneEnd = lastEndPlaneRenderer.bounds.max.x;
        float lastPlaneStart = lastStartPlaneRenderer.bounds.min.x;

        if (wheel.transform.position.x + planeSpawnDistance > lastPlaneEnd)
        {
            CreatePlatformChunk(true);
        }
        else if (wheel.transform.position.x - planeSpawnDistance < lastPlaneStart)
        {
            CreatePlatformChunk(false);
        }

        RemovePlatformChunk();
    }

    private void CreatePlatformChunk(bool isEnd)
    {
        Renderer renderer = lastEndPlane.GetComponent<Renderer>();

        float planeLength = renderer.bounds.size.x;

        if (isEnd)
        {
            Vector3 spawnPosition = lastEndPlane.transform.position + lastEndPlane.transform.right * planeLength;
            GameObject spawnPlane = platformPlane.GetPlatform();
            spawnPlane.transform.position = spawnPosition;
            activePlanes.Add(spawnPlane);

            lastEndPlane = spawnPlane;
        }
        else
        {
            Vector3 spawnPosition = lastStartPlane.transform.position - lastStartPlane.transform.right * planeLength;
            GameObject spawnPlane = platformPlane.GetPlatform();
            spawnPlane.transform.position = spawnPosition;
            activePlanes.Add(spawnPlane);

            lastStartPlane = spawnPlane;
        }
    }

    private void RemovePlatformChunk()
    {
        List<GameObject> PlanesToRemove = new List<GameObject>();

        foreach (GameObject currentPlane in activePlanes)
        {
            Renderer renderer = currentPlane.GetComponent<Renderer>();

            float planeStart = renderer.bounds.min.x;
            float planeEnd = renderer.bounds.max.x;

            if (planeEnd < wheel.transform.position.x - planeDestroyDistance || planeStart > wheel.transform.position.x + planeDestroyDistance)
            {
                PlanesToRemove.Add(currentPlane);
            }
        }

        foreach (GameObject currentPlane in PlanesToRemove)
        {
            activePlanes.Remove(currentPlane);
            platformPlane.ReturnPlatform(currentPlane);
        }

        if (activePlanes.Count > 0)
        {
            lastStartPlane = activePlanes[0];
            lastEndPlane = activePlanes[0];

            foreach (GameObject currentPlane in activePlanes)
            {
                if (currentPlane.transform.position.x < lastStartPlane.transform.position.x)
                {
                    lastStartPlane = currentPlane;
                }

                if (currentPlane.transform.position.x > lastEndPlane.transform.position.x)
                {
                    lastEndPlane = currentPlane;
                }
            }
        }
    }
}
