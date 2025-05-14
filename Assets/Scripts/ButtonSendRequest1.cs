using Firebase.Auth;
using Firebase.Database;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSendRequest : MonoBehaviour
{

    [SerializeField]
    private Button _sendRequestButton;
    [SerializeField]
    private TMP_InputField _friendUserIdInputField;

    private void Reset()

    {
        _sendRequestButton = GetComponent<Button>();
        _friendUserIdInputField = GameObject.Find("InputFieldFriendUserId").GetComponent<TMP_InputField>();

    }
    void Start()
    {
        _sendRequestButton.onClick.AddListener(HandleSendRequestButtonClicked);
    }

    private void HandleSendRequestButtonClicked()
    {
        string friendUserId = _friendUserIdInputField.text;

        var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        var username = PlayerPrefs.GetString("username");



        mDatabaseRef.Child("users")
                    .Child(friendUserId)
                    .Child("friendRequests")
                    .Child(userId)
                    .SetValueAsync(username).ContinueWith(t =>
                    {
                        //Manejar el Error
                        mDatabaseRef.Child("users")
                           .Child(userId)
                           .Child("SendRequests")
                           .Child(friendUserId)
                           .SetValueAsync(0);
                        //Establece estado 0 para solicitud pendiente
                    });
        
    }
}
