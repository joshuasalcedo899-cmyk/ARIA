using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FirebaseAdminUI : MonoBehaviour
{
    public TMP_Dropdown tagDropdown;
    public TMP_InputField responseInputField;
    public Button updateButton;

    private DatabaseReference dbRef;
    private Dictionary<string, List<string>> intentsDict = new();

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            string dbUrl = "https://aria-admin-1091a-default-rtdb.asia-southeast1.firebasedatabase.app/";
            dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, dbUrl).RootReference;
            LoadIntents();
        });

        tagDropdown.onValueChanged.AddListener(OnTagSelected);
        updateButton.onClick.AddListener(UpdateSelectedIntent);
    }

    void LoadIntents()
    {
        FirebaseDatabase.DefaultInstance.GetReference("intents")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled) return;

                DataSnapshot snapshot = task.Result;
                intentsDict.Clear();
                List<string> tags = new();

                foreach (var intent in snapshot.Children)
                {
                    string tag = intent.Key;
                    Debug.Log("Found tag: " + tag);  // ADD THIS
                    List<string> responses = new();
                    foreach (var res in intent.Child("responses").Children)
                    {
                        responses.Add(res.Value.ToString());
                    }
                    intentsDict[tag] = responses;
                    tags.Add(tag);
                }

                tagDropdown.ClearOptions();
                tagDropdown.AddOptions(tags);
                if (tags.Count > 0)
                {
                    tagDropdown.value = 0;
                    OnTagSelected(0);
                }
            });
    }

    void OnTagSelected(int index)
    {
        string selectedTag = tagDropdown.options[index].text;
        if (intentsDict.ContainsKey(selectedTag))
        {
            responseInputField.text = string.Join(";", intentsDict[selectedTag]);
        }
    }

    void UpdateSelectedIntent()
    {
        string selectedTag = tagDropdown.options[tagDropdown.value].text;
        string[] responseArray = responseInputField.text.Split(';');

        if (string.IsNullOrEmpty(selectedTag) || responseArray.Length == 0) return;

        Dictionary<string, object> responseDict = new();
        for (int i = 0; i < responseArray.Length; i++)
        {
            responseDict[i.ToString()] = responseArray[i].Trim();
        }

        dbRef.Child("intents").Child(selectedTag).Child("responses").SetValueAsync(responseDict).ContinueWithOnMainThread(t =>
        {
            if (t.IsCompleted)
            {
                intentsDict[selectedTag] = new List<string>(responseArray);
                Debug.Log("Intent updated successfully.");
            }
        });
    }
}
