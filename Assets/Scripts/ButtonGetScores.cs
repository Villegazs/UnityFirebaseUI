using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonGetScores : MonoBehaviour
{
    [SerializeField]
    private Button _buttonGetLeaderboard;

    void Reset()
    {
        _buttonGetLeaderboard = GetComponent<Button>();
    }

    void Start()
    {
        _buttonGetLeaderboard.onClick.AddListener(GetUsersHighestScores);

        FirebaseDatabase.DefaultInstance.GetReference("users")
          .OrderByChild("score").LimitToLast(3)
        .ValueChanged += HandleValueChanged;
    }

    public void GetUsersHighestScores()
    {
        FirebaseDatabase.DefaultInstance.GetReference("users")
        .OrderByChild("score").LimitToLast(3)
        .ValueChanged += HandleValueChanged;
    }
    private void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {

        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;


        foreach (var userDoc in (Dictionary<string, object>)snapshot.Value)
        {
            var userObject = (Dictionary<string, object>)userDoc.Value;
            Debug.Log(userDoc.Key);

            Debug.Log(userObject["username"] + " | " + userObject["score"]);

        }

    }
}