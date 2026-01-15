using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ImageTrackingHandler : MonoBehaviour
{
    public ARTrackedImageManager trackedImageManager;
    public GameObject prefabToSpawn;

    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            SpawnPrefab(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdatePrefab(trackedImage);
        }
    }

    void SpawnPrefab(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (spawnedPrefabs.ContainsKey(imageName))
            return;

        // Create a container object
        GameObject container = new GameObject("ModelContainer_" + imageName);
        container.transform.SetParent(trackedImage.transform);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;

        // Instantiate the model as a child of the container
        GameObject model = Instantiate(prefabToSpawn, container.transform);
        model.transform.localPosition = Vector3.zero;

        //Apply your custom rotation here (example: flip upright)
        model.transform.localRotation = Quaternion.Euler(0f, 180f, 180f);

        model.transform.localScale = Vector3.one;

        // Store the container in the dictionary
        spawnedPrefabs[imageName] = container;
    }

    void UpdatePrefab(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        if (spawnedPrefabs.TryGetValue(imageName, out GameObject container))
        {

            container.transform.position = trackedImage.transform.position;
            container.transform.rotation = trackedImage.transform.rotation;

            // Activate or deactivate based on tracking state
            //container.SetActive(trackedImage.trackingState == TrackingState.Tracking);
        }
    }
}
