using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class FirebaseResponseFetcher : MonoBehaviour
{
    public static FirebaseResponseFetcher Instance;
    private DatabaseReference dbRef;
    void Start()
    {
        string dbUrl = "https://aria-admin-1091a-default-rtdb.asia-southeast1.firebasedatabase.app/";
        dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, dbUrl).RootReference;
    }

    public void GetResponse(string tag)
    {
        if (tag == "unknown")
        {
            Debug.Log("Sorry I do not understand.");
        }
        dbRef.Child("intents").Child(tag).Child("responses").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                List<string> responses = new();

                foreach (var child in task.Result.Children)
                {
                    responses.Add(child.Value.ToString());
                }

                if (responses.Count > 0)
                {
                    string reply = responses[Random.Range(0, responses.Count)];
                    Debug.Log("Response: " + reply);
                }
                else
                {
                    Debug.Log("No responses found for tag: " + tag);
                }
            }
            else
            {
                Debug.LogError("Failed to load responses: " + task.Exception);
            }
        });
    }
}