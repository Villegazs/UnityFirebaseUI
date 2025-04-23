using Firebase.Auth;
using Firebase.Database;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSetScore : MonoBehaviour
{

    [SerializeField]
    private Button _setScoreButton;
    [SerializeField]
    private TMP_InputField _scoreInputField;



    private void Reset()

    {
        _setScoreButton = GetComponent<Button>();
        _scoreInputField = GameObject.Find("InputFieldScore").GetComponent<TMP_InputField>();

    }

    void Start()
    {
        _setScoreButton.onClick.AddListener(HandleSetScoreBittonClicked);
    }

    private void HandleSetScoreBittonClicked()
    {
        int score = int.Parse(_scoreInputField.text);

        var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        mDatabaseRef.Child("users").Child(userId).Child("score").SetValueAsync(score);
    }

}
