using Firebase.Auth;
using Firebase.Database;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAcceptFriend : MonoBehaviour
{
    [SerializeField]
    private Button _addFriendButton;
    [SerializeField]
    private string friendUserId;



    private void Reset()

    {
        _addFriendButton = GetComponent<Button>();

    }

    void Start()
    {
        _addFriendButton.onClick.AddListener(HandleAddFriendButtonClicked);
    }

    private async void HandleAddFriendButtonClicked()
    {
        Debug.Log("Anadir Amigo");
        var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;


        Debug.Log(friendUserId);
        Debug.Log(userId);


        await mDatabaseRef.Child("users")
                  .Child(friendUserId)
                  .Child("SendRequests")
                  .Child(userId)
                  .SetValueAsync(1);

        var usernameFriend =  await GetUserUsername(friendUserId);

        await mDatabaseRef.Child("users")
          .Child(userId)
          .Child("friends")
          .Child(friendUserId)
          .SetValueAsync(usernameFriend);
        //mDatabaseRef.Child("users").Child(userId).Child("friends").SetValueAsync(friendUsername);
    }

    private async Task<string> GetUserUsername(string userId)
    {
        var usernameSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/username")
            .GetValueAsync();

        return usernameSnapshot.Value?.ToString() ?? "Usuario desconocido";
    }
}
