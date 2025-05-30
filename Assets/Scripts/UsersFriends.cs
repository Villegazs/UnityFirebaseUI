using UnityEngine;
using System.Collections.Generic;
using Firebase.Database;
using System;
using TMPro;
using Firebase.Auth;
using Firebase.Extensions;
using System.Threading.Tasks;
using System.Linq;

public class UsersFriends : MonoBehaviour
{
    [SerializeField] private GameObject friendPrefabOnline; // Prefab para amigos online
    [SerializeField] private GameObject friendPrefabOffline; // Prefab para amigos offline
    [SerializeField] private GameObject userOnlinePrefab; // Prefab para usuarios online que no son amigos
    [SerializeField] private Transform friendsContainer;
    [SerializeField] private Transform usersOnlineContainer; // Contenedor para usuarios online no amigos
    private PersistentNotifier notification;
    private string currentUserId;
    private Dictionary<string, FriendDisplay> instantiatedFriends = new Dictionary<string, FriendDisplay>();
    private Dictionary<string, FriendDisplay> instantiatedUsersOnline = new Dictionary<string, FriendDisplay>();

    private Dictionary<string, bool> lastKnownOnlineStatus = new Dictionary<string, bool>();

    public void OnEnable()
    {
        // Limpiar contenedores al activarse
        notification = FindObjectOfType<PersistentNotifier>();
        ClearFriends();
        ClearOnlineUsers();
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        var friendsReference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(currentUserId).Child("friends");
        friendsReference.ValueChanged += HandleFriendsChanged;

        // Escuchar cambios en usuarios online
        var onlineUsersReference = FirebaseDatabase.DefaultInstance.GetReference("users-online");
        onlineUsersReference.ValueChanged += HandleOnlineUsersChanged;



    }
    private void ClearOnlineUsers()
    {
        foreach (var user in instantiatedUsersOnline.Values)
        {
            if (user != null && user.gameObject != null)
            {
                Destroy(user.gameObject);
            }
        }
        instantiatedUsersOnline.Clear();
    }

    private void ClearFriends()
    {
        foreach (var friend in instantiatedFriends.Values)
        {
            if (friend != null && friend.gameObject != null)
            {
                Destroy(friend.gameObject);
            }
        }
        instantiatedFriends.Clear();
    }

    private async void HandleFriendsChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;

        if (!snapshot.Exists)
        {
            Debug.Log("No tienes amigos :(");
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

                // Obtenemos la lista de usuarios online
                DataSnapshot onlineUsersSnapshot = await FirebaseDatabase.DefaultInstance
                    .GetReference("users-online")
                    .GetValueAsync();

                Dictionary<string, object> onlineUsers = onlineUsersSnapshot.Exists ?
                    onlineUsersSnapshot.Value as Dictionary<string, object> :
                    new Dictionary<string, object>();
                // Primero verificamos si algún amigo se desconectó
                foreach (var friend in instantiatedFriends)
                {
                    bool wasOnline = lastKnownOnlineStatus.ContainsKey(friend.Key) && lastKnownOnlineStatus[friend.Key];
                    bool isNowOnline = onlineUsers.ContainsKey(friend.Key);

                    if (wasOnline && !isNowOnline)
                    {
                        notification.ShowPersistentMessage($"Tu amigo {instantiatedFriends[friend.Key]}");
                    }

                    // Actualizamos el último estado conocido
                    lastKnownOnlineStatus[friend.Key] = isNowOnline;
                }

                // Separamos amigos online y offline
                var onlineFriends = new Dictionary<string, object>();
                var offlineFriends = new Dictionary<string, object>();

                foreach (var friendEntry in friends)
                {
                    if (onlineUsers.ContainsKey(friendEntry.Key))
                    {
                        onlineFriends.Add(friendEntry.Key, friendEntry.Value);
                    }
                    else
                    {
                        offlineFriends.Add(friendEntry.Key, friendEntry.Value);
                    }
                }

                // Instanciamos primero amigos online
                await InstantiateFriends(onlineFriends, true);

                // Luego amigos offline
                await InstantiateFriends(offlineFriends, false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al procesar amigos: {e.Message}");
        }
    }

    private async Task InstantiateFriends(Dictionary<string, object> friends, bool isOnline)
    {
        foreach (var friendEntry in friends)
        {
            string friendId = friendEntry.Key;
            string friendUsername = friendEntry.Value.ToString();

            int characterIndex = await GetFriendCharacterIndex(friendId);

            if (instantiatedFriends.ContainsKey(friendId))
            {
                // Actualizar amigo existente
                instantiatedFriends[friendId].Initialize(friendId, friendUsername, characterIndex, isOnline);

                // Si el estado cambió, necesitamos reemplazar el prefab
                if ((isOnline && instantiatedFriends[friendId].gameObject != friendPrefabOnline) ||
                    (!isOnline && instantiatedFriends[friendId].gameObject != friendPrefabOffline))
                {
                    // Destruir el prefab antiguo
                    Destroy(instantiatedFriends[friendId].gameObject);

                    // Instanciar el nuevo prefab correcto
                    GameObject newFriendObj = Instantiate(
                        isOnline ? friendPrefabOnline : friendPrefabOffline,
                        friendsContainer);

                    FriendDisplay display = newFriendObj.GetComponent<FriendDisplay>();
                    display.Initialize(friendId, friendUsername, characterIndex, isOnline);
                    instantiatedFriends[friendId] = display;
                }
            }
            else
            {
                // Instanciar nuevo amigo con el prefab correcto
                GameObject friendObj = Instantiate(
                    isOnline ? friendPrefabOnline : friendPrefabOffline,
                    friendsContainer);

                FriendDisplay display = friendObj.GetComponent<FriendDisplay>();
                display.Initialize(friendId, friendUsername, characterIndex, isOnline);
                instantiatedFriends.Add(friendId, display);
            }
        }
    }

    private async void HandleOnlineUsersChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;

        try
        {
            // Obtenemos la lista de amigos actual
            DataSnapshot friendsSnapshot = await FirebaseDatabase.DefaultInstance
                .GetReference($"users/{currentUserId}/friends")
                .GetValueAsync();

            Dictionary<string, object> friends = friendsSnapshot.Exists ?
                friendsSnapshot.Value as Dictionary<string, object> :
                new Dictionary<string, object>();

            // Procesamos usuarios online que no son amigos
            if (snapshot.Exists)
            {
                Dictionary<string, object> onlineUsers = snapshot.Value as Dictionary<string, object>;

                // Eliminamos usuarios online que ya no están
                List<string> toRemove = new List<string>();
                foreach (var user in instantiatedUsersOnline)
                {
                    if (!onlineUsers.ContainsKey(user.Key) || friends.ContainsKey(user.Key))
                    {
                        toRemove.Add(user.Key);
                    }
                }

                foreach (string key in toRemove)
                {
                    Destroy(instantiatedUsersOnline[key].gameObject);
                    instantiatedUsersOnline.Remove(key);
                }

                // Añadimos nuevos usuarios online que no son amigos
                foreach (var userEntry in onlineUsers)
                {
                    string userId = userEntry.Key;
                    string username = userEntry.Value.ToString();

                    // Saltar si es el usuario actual o ya es amigo
                    if (userId == currentUserId || friends.ContainsKey(userId))
                        continue;

                    if (!instantiatedUsersOnline.ContainsKey(userId))
                    {
                        // Obtenemos información adicional del usuario
                        DataSnapshot userSnapshot = await FirebaseDatabase.DefaultInstance
                            .GetReference($"users/{userId}")
                            .GetValueAsync();

                        if (userSnapshot.Exists)
                        {
                            int characterIndex = 0;
                            if (userSnapshot.HasChild("character"))
                            {
                                characterIndex = Convert.ToInt32(userSnapshot.Child("character").Value);
                            }

                            // Instanciar usuario online no amigo
                            GameObject userObj = Instantiate(userOnlinePrefab, usersOnlineContainer);
                            FriendDisplay display = userObj.GetComponent<FriendDisplay>();
                            display.Initialize(userId, username, characterIndex, true);
                            instantiatedUsersOnline.Add(userId, display);
                        }
                    }
                }
            }
            else
            {
                // No hay usuarios online, limpiamos
                foreach (var user in instantiatedUsersOnline.Values)
                {
                    Destroy(user.gameObject);
                }
                instantiatedUsersOnline.Clear();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al procesar usuarios online: {e.Message}");
        }
    }


    private async Task<int> GetFriendCharacterIndex(string friendId)
    {
        try
        {
            DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
                .GetReference($"users/{friendId}/character")
                .GetValueAsync();

            if (snapshot.Exists)
            {
                return Convert.ToInt32(snapshot.Value);
            }
            return 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener personaje del amigo: {e.Message}");
            return 0;
        }
    }

    private void OnDisable()
    {
        var friendsReference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(currentUserId).Child("friends");
        friendsReference.ValueChanged -= HandleFriendsChanged;

        var onlineUsersReference = FirebaseDatabase.DefaultInstance.GetReference("users-online");
        onlineUsersReference.ValueChanged -= HandleOnlineUsersChanged;
    }
}