using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PendingFriendResponse : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        var reference = mDatabaseRef.Child("users").Child(userId).Child("friendResponse");
        reference.ChildAdded += HandleChildAdded;
        //reference.ChildRemoved += HandleChildRemoved;
    }

    private async void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }      

        DataSnapshot snapshot = args.Snapshot;

        string friendId = snapshot.Key;
        Debug.Log("Respuest de "+friendId + " estado:" +snapshot.Value);
        int estado = int.Parse(snapshot.Value.ToString());
        var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        string friendUsermane = (await FirebaseDatabase.DefaultInstance
                                        .GetReference("users/" + friendId + "/username")
                                        .GetValueAsync()).Value?.ToString();


        var checkRequestId =await (FirebaseDatabase.DefaultInstance
                                     .GetReference("users/" + userId + "/SendRequests/" + friendId).GetValueAsync());
                                     
        //Validamos si la respuesta es una solitud pendinete
        if ((checkRequestId.Value == null))
        {
            Debug.Log("Se descarta respuesta de solicitud de amistad con id " + friendId);
            eliminarSolicitud(friendId, "friendResponse");
            return;
        }
        //Estado 1 para solicitud aceptada
        if (estado == 1)
        {

            Debug.Log(friendUsermane + " ha aceptado tu solicitud");
            mDatabaseRef.Child("users").Child(userId).Child("friends").Child(friendId).SetValueAsync(friendUsermane);
        }
        //Estado 2 para solicitud rechazada
        if (estado == 2)
        {
            Debug.Log(friendUsermane + " ha rechazado tu solicitud");
        }
        eliminarSolicitud(friendId, "SendRequests");
        eliminarSolicitud(friendId, "friendResponse");

    }

    private void eliminarSolicitud(string requestUserId,string requestMailbox)
    {
        var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        mDatabaseRef.Child("users")
                           .Child(userId)
                           .Child(requestMailbox)
                           .Child(requestUserId)
                           .SetValueAsync(null);
    }
}
