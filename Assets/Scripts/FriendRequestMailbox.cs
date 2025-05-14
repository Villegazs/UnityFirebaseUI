using Firebase.Auth;
using Firebase.Database;
using System;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

public class FriendRequestMailbox : MonoBehaviour
{

    private void OnEnable()
    {
        Debug.Log("OnEnable");
        if (FirebaseAuth.DefaultInstance.CurrentUser!= null)
        {
            var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
            var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            var reference = mDatabaseRef.Child("users").Child(userId).Child("friendRequests");
            reference.ChildAdded += HandleChildAdded;
        }
    }

    private void OnDisable()
    {
        Debug.Log("OnDisable");
        if(FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
            var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            var reference = mDatabaseRef.Child("users").Child(userId).Child("friendRequests");
            reference.ChildAdded -= HandleChildAdded;
        }
    }
    private void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;

        string friendId = snapshot.Key;
        string friendUsername =snapshot.Value.ToString();

        Debug.Log("Tienes una solicitud de amista de  " + friendUsername);
    }


}
