using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.IO;

public class IntentResponseHandler : MonoBehaviour
{
    public DatabaseReference dbRef;
    private Dictionary<string, List<string>> intentCache = new Dictionary<string, List<string>>();
    private string cacheFilePath;
    public string responseNLP;

    public static IntentResponseHandler instance;

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
    private void Update()
    {
        LoadIntentCacheFromLocal();
    }

    private bool IsLocationIntent(string tag)
    {
        return tag.StartsWith("location_"); // all your location-related intents use this prefix
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

            SaveIntentCacheToLocal();
            Debug.Log("Preloaded and cached all intents.");
        });
    }

    public void GetResponseByIntent(string intentTag)
    {
        List<string> responses = intentCache[intentTag];
        string selected = responses[Random.Range(0, responses.Count)];
        if (intentCache.ContainsKey(intentTag))
        {
            
            if (IsLocationIntent(intentTag))
            {
                responseNLP = selected;
                Invoke("SuggestNavigationModule", TTS.instance.getClipLength()+0.5f);
            }
            Debug.Log($"Intent '{intentTag}' (cached) response: {selected}");
            TTS.instance.play(selected);
           
        }
        else
        {
            Debug.LogWarning($"Intent '{intentTag}' not found in cache.");
        }
    }

    private void SaveIntentCacheToLocal()
    {
        string json = JsonUtility.ToJson(new SerializableIntentCache(intentCache));
        File.WriteAllText(cacheFilePath, json);
    }

    private void LoadIntentCacheFromLocal()
    {
        if (File.Exists(cacheFilePath))
        {
            string json = File.ReadAllText(cacheFilePath);
            SerializableIntentCache loaded = JsonUtility.FromJson<SerializableIntentCache>(json);
            intentCache = loaded.ToDictionary();
        }
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
    private void SuggestNavigationModule()
    {
        string suggestion = "Would you like me to open the navigation module to guide you there?";
        TTS.instance.play(suggestion);

        // Optionally, show a UI prompt or auto-launch navigation
        // UIManager.Instance.ShowNavigationPrompt(); // if you have a UI for this
    }


    [System.Serializable]
    private class SerializableIntent
    {
        public string tag;
        public List<string> responses;
    }
}
