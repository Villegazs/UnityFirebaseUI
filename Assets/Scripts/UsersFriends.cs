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
    [SerializeField] private GameObject friendPrefabOnline;
    [SerializeField] private GameObject friendPrefabOffline;
    [SerializeField] private GameObject userOnlinePrefab;
    [SerializeField] private Transform friendsContainer;
    [SerializeField] private Transform usersOnlineContainer;
    private PersistentNotifier notification;
    private string currentUserId;
    private Dictionary<string, FriendDisplay> instantiatedFriends = new Dictionary<string, FriendDisplay>();
    private Dictionary<string, FriendDisplay> instantiatedUsersOnline = new Dictionary<string, FriendDisplay>();
    private Dictionary<string, bool> lastKnownOnlineStatus = new Dictionary<string, bool>();

    private DatabaseReference friendsReference;
    private DatabaseReference onlineUsersReference;

    public void OnEnable()
    {
        notification = FindObjectOfType<PersistentNotifier>();
        ClearFriends();
        ClearOnlineUsers();
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        // Referencias a la base de datos
        friendsReference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(currentUserId).Child("friends");
        onlineUsersReference = FirebaseDatabase.DefaultInstance.GetReference("users-online");

        // Suscripción a eventos de amigos
        friendsReference.ChildAdded += HandleFriendAdded;
        friendsReference.ChildRemoved += HandleFriendRemoved;

        // Suscripción a eventos de usuarios online
        onlineUsersReference.ChildAdded += HandleOnlineUserAdded;
        onlineUsersReference.ChildRemoved += HandleOnlineUserRemoved;

        // Cargar datos iniciales
        //LoadInitialFriends();
        //LoadInitialOnlineUsers();
    }

    private async void LoadInitialFriends()
    {
        DataSnapshot snapshot = await friendsReference.GetValueAsync();
        if (snapshot.Exists)
        {
            foreach (DataSnapshot child in snapshot.Children)
            {
                await ProcessFriendAdded(child.Key, child.Value.ToString());
            }
        }
    }

    private async void LoadInitialOnlineUsers()
    {
        DataSnapshot snapshot = await onlineUsersReference.GetValueAsync();
        if (snapshot.Exists)
        {
            foreach (DataSnapshot child in snapshot.Children)
            {
                await ProcessOnlineUserAdded(child.Key, child.Value.ToString());
            }
        }
    }

    private async void HandleFriendAdded(object sender, ChildChangedEventArgs args)
    {
        await ProcessFriendAdded(args.Snapshot.Key, args.Snapshot.Value.ToString());
    }

    private async Task ProcessFriendAdded(string friendId, string friendUsername)
    {
        try
        {
            // Verificar si el usuario ya está online
            bool isOnline = await IsUserOnline(friendId);
            int characterIndex = await GetFriendCharacterIndex(friendId);

            if (instantiatedFriends.ContainsKey(friendId))
            {
                // Actualizar amigo existente
                instantiatedFriends[friendId].Initialize(friendId, friendUsername, characterIndex, isOnline);
                UpdateFriendPrefab(friendId, isOnline);
            }
            else
            {
                // Crear nuevo amigo
                GameObject friendObj = Instantiate(
                    isOnline ? friendPrefabOnline : friendPrefabOffline,
                    friendsContainer);

                FriendDisplay display = friendObj.GetComponent<FriendDisplay>();
                display.Initialize(friendId, friendUsername, characterIndex, isOnline);
                instantiatedFriends.Add(friendId, display);
            }

            // Actualizar estado de conexión
            lastKnownOnlineStatus[friendId] = isOnline;

            // Notificar si acaba de conectarse
            if (isOnline && lastKnownOnlineStatus.TryGetValue(friendId, out bool wasOnline) && !wasOnline)
            {
                notification.ShowPersistentMessage($"Tu amigo {friendUsername} se ha conectado");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al procesar amigo añadido: {e.Message}");
        }
    }

    private void HandleFriendRemoved(object sender, ChildChangedEventArgs args)
    {
        string friendId = args.Snapshot.Key;
        if (instantiatedFriends.ContainsKey(friendId))
        {
            Destroy(instantiatedFriends[friendId].gameObject);
            instantiatedFriends.Remove(friendId);
            lastKnownOnlineStatus.Remove(friendId);
        }
    }

    private async void HandleOnlineUserAdded(object sender, ChildChangedEventArgs args)
    {
        await ProcessOnlineUserAdded(args.Snapshot.Key, args.Snapshot.Value.ToString());
    }

    private async Task ProcessOnlineUserAdded(string userId, string username)
    {
        try
        {
            // Saltar si es el usuario actual
            if (userId == currentUserId) return;

            // Verificar si ya es amigo
            bool isFriend = await IsUserFriend(userId);
            if (isFriend)
            {
                // Actualizar estado de amigo existente
                if (instantiatedFriends.ContainsKey(userId))
                {
                    instantiatedFriends[userId].Initialize(userId, username,
                        await GetFriendCharacterIndex(userId), true);
                    UpdateFriendPrefab(userId, true);
                }
                return;
            }

            // Procesar usuario online no amigo
            if (!instantiatedUsersOnline.ContainsKey(userId))
            {
                int characterIndex = await GetUserCharacterIndex(userId);
                GameObject userObj = Instantiate(userOnlinePrefab, usersOnlineContainer);
                FriendDisplay display = userObj.GetComponent<FriendDisplay>();
                display.Initialize(userId, username, characterIndex, true);
                instantiatedUsersOnline.Add(userId, display);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al procesar usuario online añadido: {e.Message}");
        }
    }

    private void HandleOnlineUserRemoved(object sender, ChildChangedEventArgs args)
    {
        string userId = args.Snapshot.Key;

        // Si es amigo, actualizar su estado a offline
        if (instantiatedFriends.ContainsKey(userId))
        {
            instantiatedFriends[userId].Initialize(userId,
                instantiatedFriends[userId].FriendName,
                instantiatedFriends[userId].CharacterIndex,
                false);
            UpdateFriendPrefab(userId, false);

            // Notificar desconexión
            notification.ShowPersistentMessage($"Tu amigo {instantiatedFriends[userId].FriendName} se ha desconectado");
        }

        // Si es usuario online no amigo, eliminarlo
        if (instantiatedUsersOnline.ContainsKey(userId))
        {
            Destroy(instantiatedUsersOnline[userId].gameObject);
            instantiatedUsersOnline.Remove(userId);
        }

        // Actualizar último estado conocido
        if (lastKnownOnlineStatus.ContainsKey(userId))
        {
            lastKnownOnlineStatus[userId] = false;
        }
    }

    private void UpdateFriendPrefab(string friendId, bool isOnline)
    {
        var currentDisplay = instantiatedFriends[friendId];
        bool needsUpdate = (isOnline && currentDisplay.gameObject != friendPrefabOnline) ||
                         (!isOnline && currentDisplay.gameObject != friendPrefabOffline);

        if (needsUpdate)
        {
            // Guardar datos
            string username = currentDisplay.FriendName;
            int characterIndex = currentDisplay.CharacterIndex;

            // Destruir prefab antiguo
            Destroy(currentDisplay.gameObject);

            // Crear nuevo prefab
            GameObject newFriendObj = Instantiate(
                isOnline ? friendPrefabOnline : friendPrefabOffline,
                friendsContainer);

            FriendDisplay display = newFriendObj.GetComponent<FriendDisplay>();
            display.Initialize(friendId, username, characterIndex, isOnline);
            instantiatedFriends[friendId] = display;
        }
    }

    private async Task<bool> IsUserOnline(string userId)
    {
        DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference("users-online")
            .Child(userId)
            .GetValueAsync();
        return snapshot.Exists;
    }

    private async Task<bool> IsUserFriend(string userId)
    {
        DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/friends")
            .Child(userId)
            .GetValueAsync();
        return snapshot.Exists;
    }

    private async Task<int> GetFriendCharacterIndex(string friendId)
    {
        DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{friendId}/character")
            .GetValueAsync();
        return snapshot.Exists ? Convert.ToInt32(snapshot.Value) : 0;
    }

    private async Task<int> GetUserCharacterIndex(string userId)
    {
        DataSnapshot snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/character")
            .GetValueAsync();
        return snapshot.Exists ? Convert.ToInt32(snapshot.Value) : 0;
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
        lastKnownOnlineStatus.Clear();
    }

    private void OnDisable()
    {

        // Limpiar todos los listeners
        if (friendsReference != null)
        {
            friendsReference.ChildAdded -= HandleFriendAdded;
            friendsReference.ChildRemoved -= HandleFriendRemoved;
        }

        if (onlineUsersReference != null)
        {
            onlineUsersReference.ChildAdded -= HandleOnlineUserAdded;
            onlineUsersReference.ChildRemoved -= HandleOnlineUserRemoved;
        }


    }
}