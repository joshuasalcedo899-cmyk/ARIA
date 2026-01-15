using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.IO;

public class FirebaseHandler : MonoBehaviour
{
    public DatabaseReference dbRef;
    private Dictionary<string, List<string>> intentCache = new Dictionary<string, List<string>>();
    private string cacheFilePath;   

    public static FirebaseHandler instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        instance = this;


        cacheFilePath = Path.Combine(Application.persistentDataPath, "intentCache.json");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null || task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase init error: " + task.Exception);
                return;
            }

            string dbUrl = "https://aria-admin-1091a-default-rtdb.asia-southeast1.firebasedatabase.app/";
            dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, dbUrl).RootReference;


            PreloadAllIntents();

        });
    }



    private void PreloadAllIntents()
    {
        dbRef.Child("intents").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to preload intents: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            intentCache.Clear();

            foreach (var intent in snapshot.Children)
            {
                string intentTag = intent.Key;
                List<string> responses = new List<string>();

                foreach (var resp in intent.Child("responses").Children)
                {
                    responses.Add(resp.Value.ToString());
                }

                intentCache[intentTag] = responses;
            }
            Debug.Log("Preloaded and cached all intents.");
        });
    }

    

    

   

    [System.Serializable]
    private class SerializableIntentCache
    {
        public List<SerializableIntent> intents = new List<SerializableIntent>();

        public SerializableIntentCache(Dictionary<string, List<string>> dict)
        {
            foreach (var pair in dict)
            {
                intents.Add(new SerializableIntent { tag = pair.Key, responses = pair.Value });
            }
        }

        public Dictionary<string, List<string>> ToDictionary()
        {
            Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
            foreach (var item in intents)
            {
                dict[item.tag] = item.responses;
            }
            return dict;
        }
    }


    [System.Serializable]
    private class SerializableIntent
    {
        public string tag;
        public List<string> responses;
    }
}
