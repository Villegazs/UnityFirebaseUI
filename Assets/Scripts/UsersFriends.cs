using UnityEngine;
using System.Collections.Generic;
using Firebase.Database;
using System;
using TMPro;
using Firebase.Auth;

public class UsersFriends : MonoBehaviour
{
    [SerializeField] private GameObject friendPrefab; // Asigna el prefab en el Inspector
    [SerializeField] private Transform friendsContainer; // Padre donde instanciar los amigos

    private string currentUserId;
    private Dictionary<string, FriendDisplay> instantiatedFriends = new Dictionary<string, FriendDisplay>();

    public void OnEnable()
    {
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        var friendsReference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(currentUserId).Child("friends");
        friendsReference.ValueChanged += HandleFriendsChanged;
    }

    private void HandleFriendsChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;

        if (!snapshot.Exists)
        {
            Debug.Log("El usuario no tiene amigos registrados");
            ClearFriends();
            return;
        }

        try
        {
            Dictionary<string, object> friends = snapshot.Value as Dictionary<string, object>;

            if (friends != null)
            {
                // Primero limpiamos amigos que ya no están
                List<string> toRemove = new List<string>();
                foreach (var friend in instantiatedFriends)
                {
                    if (!friends.ContainsKey(friend.Key))
                    {
                        toRemove.Add(friend.Key);
                    }
                }

                foreach (string key in toRemove)
                {
                    Destroy(instantiatedFriends[key].gameObject);
                    instantiatedFriends.Remove(key);
                }

                // Luego añadimos/actualizamos los amigos
                foreach (var friendEntry in friends)
                {
                    string friendId = friendEntry.Key;
                    string friendUsername = friendEntry.Value.ToString();

                    if (instantiatedFriends.ContainsKey(friendId))
                    {
                        // Actualizar amigo existente
                        instantiatedFriends[friendId].Initialize(friendId, friendUsername);
                    }
                    else
                    {
                        // Instanciar nuevo amigo
                        GameObject friendObj = Instantiate(friendPrefab, friendsContainer);
                        FriendDisplay display = friendObj.GetComponent<FriendDisplay>();
                        display.Initialize(friendId, friendUsername);
                        instantiatedFriends.Add(friendId, display);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al procesar amigos: {e.Message}");
        }
    }

    private void ClearFriends()
    {
        foreach (var friend in instantiatedFriends.Values)
        {
            Destroy(friend.gameObject);
        }
        instantiatedFriends.Clear();
    }

    private void OnDisable()
    {
        var friendsReference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(currentUserId).Child("friends");
        friendsReference.ValueChanged -= HandleFriendsChanged;
    }
}