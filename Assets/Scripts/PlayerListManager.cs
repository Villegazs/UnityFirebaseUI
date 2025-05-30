using Firebase.Database;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using Firebase.Auth;

public class PlayerListManager : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject friendOnlinePrefab;
    [SerializeField] private GameObject friendOfflinePrefab;
    [SerializeField] private GameObject playerOnlinePrefab;

    private string currentUserId;
    private Dictionary<string, FriendData> friendsData = new Dictionary<string, FriendData>();
    private Dictionary<string, PlayerData> onlinePlayers = new Dictionary<string, PlayerData>();

    private class FriendData
    {
        public string username;
        public bool isOnline;
        public GameObject displayObject;
    }

    private class PlayerData
    {
        public string username;
        public GameObject displayObject;
    }

    void Start()
    {
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        LoadFriends();
        SetupOnlinePlayersListener();
    }

    private async void LoadFriends()
    {
        DataSnapshot friendsSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/friends")
            .GetValueAsync();

        if (friendsSnapshot.Exists)
        {
            foreach (DataSnapshot friend in friendsSnapshot.Children)
            {
                string friendId = friend.Key;
                string friendName = friend.Value.ToString();

                friendsData[friendId] = new FriendData
                {
                    username = friendName,
                    isOnline = false,
                    displayObject = null
                };

                // Verificar estado online
                CheckFriendOnlineStatus(friendId);
            }
        }
    }

    private void SetupOnlinePlayersListener()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("users-online")
            .ValueChanged += HandleOnlinePlayersChanged;
    }

    private void HandleOnlinePlayersChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        onlinePlayers.Clear();

        if (args.Snapshot.Exists)
        {
            foreach (DataSnapshot player in args.Snapshot.Children)
            {
                string playerId = player.Key;
                if (playerId == currentUserId) continue;

                // Solo añadir si no es amigo (los amigos ya se manejan aparte)
                if (!friendsData.ContainsKey(playerId))
                {
                    onlinePlayers[playerId] = new PlayerData
                    {
                        username = "Cargando...",
                        displayObject = null
                    };

                    LoadPlayerUsername(playerId);
                }
            }
        }

        UpdateAllDisplays();
    }

    private async void CheckFriendOnlineStatus(string friendId)
    {
        DataSnapshot onlineStatus = await FirebaseDatabase.DefaultInstance
            .GetReference($"users-online/{friendId}")
            .GetValueAsync();

        bool isOnline = onlineStatus.Exists && onlineStatus.Value != null;

        if (friendsData.TryGetValue(friendId, out FriendData friend))
        {
            friend.isOnline = isOnline;
            UpdateFriendDisplay(friendId);
        }
    }


    private void UpdateFriendDisplay(string friendId)
    {
       
    }
    private async void LoadPlayerUsername(string playerId)
    {
        DataSnapshot usernameSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{playerId}/username")
            .GetValueAsync();

        if (usernameSnapshot.Exists && onlinePlayers.TryGetValue(playerId, out PlayerData player))
        {
            player.username = usernameSnapshot.Value.ToString();
            //UpdatePlayerDisplay(playerId);
        }
    }

    private void UpdateAllDisplays()
    {
        // Limpiar todos los objetos existentes
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 1. Mostrar amigos online
        foreach (var friend in friendsData
            .Where(f => f.Value.isOnline)
            .OrderBy(f => f.Value.username))
        {
            CreateFriendDisplay(friend.Key, friend.Value, true);
        }

        // 2. Mostrar amigos offline
        foreach (var friend in friendsData
            .Where(f => !f.Value.isOnline)
            .OrderBy(f => f.Value.username))
        {
            CreateFriendDisplay(friend.Key, friend.Value, false);
        }

        // 3. Mostrar otros jugadores online
        foreach (var player in onlinePlayers
            .OrderBy(p => p.Value.username))
        {
            CreatePlayerDisplay(player.Key, player.Value);
        }
    }

    private void CreateFriendDisplay(string friendId, FriendData friend, bool isOnline)
    {
        if (friend.displayObject != null)
        {
            Destroy(friend.displayObject);
        }

        GameObject prefab = isOnline ? friendOnlinePrefab : friendOfflinePrefab;
        friend.displayObject = Instantiate(prefab, contentParent);

        FriendDisplay display = friend.displayObject.GetComponent<FriendDisplay>();
        //display.Initialize(friendId, friend.username);
    }

    private void CreatePlayerDisplay(string playerId, PlayerData player)
    {
        if (player.displayObject != null)
        {
            Destroy(player.displayObject);
        }

        player.displayObject = Instantiate(playerOnlinePrefab, contentParent);

        PlayerDisplay display = player.displayObject.GetComponent<PlayerDisplay>();
        display.Initialize(playerId, player.username);
    }

    private void OnDestroy()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("users-online")
            .ValueChanged -= HandleOnlinePlayersChanged;
    }
}