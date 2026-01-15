using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using System;

public class FirebaseAdminEditor : MonoBehaviour
{
    [Header("Intent Editor UI")]
    public TMP_Dropdown intentDropdown;
    public TMP_InputField responsesEdit;
    public TextMeshProUGUI responsesDisplay;
    public TextMeshProUGUI tagDisplay;
    public Button updateIntentButton;
    public GameObject intentTagsGroup;
    public GameObject ViewPanel;
    public GameObject EditPanel;

    [Header("Navigation Editor UI")]
    public TMP_Dropdown targetDropdown;
    public TMP_InputField targetNameEdit;
    public TextMeshProUGUI targetNodeDisplay; // 👈 Added for node info
    public Button updateTargetButton;
    public GameObject navigationTargetsGroup;

    private DatabaseReference dbRef;
    private List<string> intentTagList = new();
    private Dictionary<string, List<string>> intentsDict = new();

    private string ariaResponse;

    // 🔹 Navigation Data
    private List<string> navTargetKeys = new();
    private Dictionary<string, (string name, string node)> navDataDict = new();

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null || task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase init error: " + task.Exception);
                return;
            }

            string dbUrl = "https://aria-admin-1091a-default-rtdb.asia-southeast1.firebasedatabase.app/";
            dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, dbUrl).RootReference;

            LoadIntents();
            LoadNavigationTargets();
        });

        updateTargetButton.onClick.AddListener(ApplyTargetEdit);
    }

    void LoadIntents()
    {
        dbRef.Child("intents").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || task.IsFaulted)
            {
                Debug.LogError("Failed to load intents: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            intentTagList.Clear();
            intentsDict.Clear();

            foreach (var child in snapshot.Children)
            {
                string tag = child.Key;
                intentTagList.Add(tag);

                List<string> responses = new();
                foreach (var res in child.Child("responses").Children)
                    responses.Add(res.Value.ToString());

                intentsDict[tag] = responses;
            }

            intentDropdown.ClearOptions();
            intentDropdown.AddOptions(intentTagList);
            intentDropdown.onValueChanged.AddListener(index => ShowIntent(index));

            if (intentTagList.Count > 0)
                ShowIntent(0);
        });
    }

    void ShowIntent(int index)
    {
        string tag = intentTagList[index];
        tagDisplay.text = $"Tag: {tag}";
        ariaResponse = string.Join(";", intentsDict[tag]);
        responsesDisplay.text = ariaResponse;
    }

    public void EditIntent()
    {
        responsesEdit.text = ariaResponse;
        EditPanel.SetActive(true);
        ViewPanel.SetActive(false);
    }

    public void cancelEditting()
    {
        EditPanel.SetActive(false);
        ViewPanel.SetActive(true);
    }

    public void ApplyIntentEdit()
    {
        int index = intentDropdown.value;
        string tag = intentTagList[index];
        string[] updatedResponses = responsesEdit.text.Split(';');

        Dictionary<string, object> responseDict = new();
        for (int i = 0; i < updatedResponses.Length; i++)
            responseDict[i.ToString()] = updatedResponses[i].Trim();

        dbRef.Child("intents").Child(tag).Child("responses").SetValueAsync(responseDict).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                intentsDict[tag] = new List<string>(updatedResponses);
                ShowIntent(index);
                Debug.Log("Intent updated successfully.");
                PopupManager.instance.ShowMessage("Response updated succefully");
                EditPanel.SetActive(false);
                ViewPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("Failed to update intent: " + task.Exception);
            }
        });
    }

      void LoadNavigationTargets()
    {
        dbRef.Child("navigation_location").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || task.IsFaulted)
            {
                Debug.LogError("Failed to load targets: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            navTargetKeys.Clear();
            navDataDict.Clear();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            foreach (var child in snapshot.Children)
            {
                string key = child.Key;
                string name = child.Child("name").Value != null ? child.Child("name").Value.ToString() : "Unnamed";
                string node = child.Child("node").Value != null ? child.Child("node").Value.ToString() : "none";

                navTargetKeys.Add(key);
                navDataDict[key] = (name, node);

                string display = $"{key} - {name}";
                options.Add(new TMP_Dropdown.OptionData(display));
            }

            targetDropdown.ClearOptions();
            targetDropdown.AddOptions(options);

            targetDropdown.onValueChanged.RemoveAllListeners();
            targetDropdown.onValueChanged.AddListener(ShowTarget);

            if (navTargetKeys.Count > 0)
                ShowTarget(0);
        });
    }

    void ShowTarget(int index)
    {
        string key = navTargetKeys[index];
        var data = navDataDict[key];

        targetNameEdit.text = data.name;
        if (targetNodeDisplay != null)
            targetNodeDisplay.text = $"Node: {data.node}";

        navigationTargetsGroup.SetActive(true);
    }

    void ApplyTargetEdit()
    {
        int index = targetDropdown.value;
        string key = navTargetKeys[index];
        var existingData = navDataDict[key];

        string newName = targetNameEdit.text.Trim();
        string node = existingData.node; // keep same node (not editable)

        Dictionary<string, object> updateData = new()
        {
            { "name", newName },
            { "node", node }
        };

        dbRef.Child("navigation_location").Child(key).SetValueAsync(updateData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                navDataDict[key] = (newName, node);
                PopupManager.instance.ShowMessage("Navigation target updated successfully");
                Debug.Log($"✅ Navigation target '{key}' updated.");
            }
            else
            {
                Debug.LogError("Failed to update target: " + task.Exception);
            }
        });
    }
}
