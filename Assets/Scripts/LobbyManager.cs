using Firebase.Database;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Threading.Tasks;
using Firebase.Auth;
using System;
using Unity.VisualScripting;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("UI References")]
    [SerializeField] private Transform playersContainer;
    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private TMP_Text lobbyStatusText;

    private string currentUserId;
    private string currentLobbyId;
    private Dictionary<string, GameObject> lobbyPlayers = new Dictionary<string, GameObject>();
    private DatabaseReference lobbyPlayersRef;

    private async void OnEnable()
    {
        Instance = this;

        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.LogError("No authenticated user!");
            return;
        }

        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        currentLobbyId = currentUserId;
        SetupLobbyPlayersListener();

        try
        {
            int characterIndex = await GetCharacterIndex(currentUserId);
            await CreateLobby(currentUserId, characterIndex);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in OnEnable: {ex.Message}");
        }
    }

    private void SetupLobbyPlayersListener()
    {
        // Limpiar listeners anteriores
        if (lobbyPlayersRef != null)
        {
            lobbyPlayersRef.ChildAdded -= HandlePlayerAdded;
            lobbyPlayersRef.ChildRemoved -= HandlePlayerRemoved;
            lobbyPlayersRef.ChildChanged -= HandlePlayerChanged;
        }

        if (string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogError("Tried to setup listener without currentLobbyId");
            return;
        }

        // Configurar nuevos listeners
        lobbyPlayersRef = FirebaseDatabase.DefaultInstance
            .GetReference($"lobbies/{currentLobbyId}/players");

        lobbyPlayersRef.ChildAdded += HandlePlayerAdded;
        lobbyPlayersRef.ChildRemoved += HandlePlayerRemoved;
        lobbyPlayersRef.ChildChanged += HandlePlayerChanged;

        // Cargar jugadores existentes
        LoadExistingPlayers();
    }

    private async void LoadExistingPlayers()
    {
        try
        {
            DataSnapshot snapshot = await lobbyPlayersRef.GetValueAsync();
            if (snapshot.Exists)
            {
                foreach (DataSnapshot playerSnapshot in snapshot.Children)
                {
                    await ProcessPlayerAdded(playerSnapshot);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading existing players: {ex.Message}");
        }
    }

    private async void HandlePlayerAdded(object sender, ChildChangedEventArgs args)
    {
        await ProcessPlayerAdded(args.Snapshot);
    }

    private async Task ProcessPlayerAdded(DataSnapshot playerSnapshot)
    {
        string playerId = playerSnapshot.Key;

        if (!playerSnapshot.HasChild("characterIndex") || playerSnapshot.Child("characterIndex").Value == null)
        {
            Debug.LogWarning($"Player {playerId} is missing characterIndex");
            return;
        }

        if (!int.TryParse(playerSnapshot.Child("characterIndex").Value.ToString(), out int characterIndex))
        {
            Debug.LogWarning($"Invalid characterIndex for player {playerId}");
            return;
        }

        // Obtener nombre del jugador
        DataSnapshot usernameSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{playerId}/username")
            .GetValueAsync();

        if (!usernameSnapshot.Exists)
        {
            Debug.LogWarning($"No username found for player {playerId}");
            return;
        }

        string username = usernameSnapshot.Value.ToString();

        // Crear entrada en el lobby
        if (!lobbyPlayers.ContainsKey(playerId))
        {
            GameObject playerEntry = Instantiate(playerEntryPrefab, playersContainer);
            var display = playerEntry.GetComponent<PlayerLobbyDisplay>();
            display.Initialize(playerId, username, characterIndex);
            lobbyPlayers[playerId] = playerEntry;
        }
    }

    private void HandlePlayerRemoved(object sender, ChildChangedEventArgs args)
    {
        string playerId = args.Snapshot.Key;
        RemovePlayerFromLobby(playerId);
    }

    private void HandlePlayerChanged(object sender, ChildChangedEventArgs args)
    {
        string playerId = args.Snapshot.Key;

        if (!args.Snapshot.HasChild("characterIndex") || args.Snapshot.Child("characterIndex").Value == null)
        {
            Debug.LogWarning($"Player {playerId} changed but missing characterIndex");
            return;
        }

        if (!int.TryParse(args.Snapshot.Child("characterIndex").Value.ToString(), out int characterIndex))
        {
            Debug.LogWarning($"Invalid characterIndex update for player {playerId}");
            return;
        }

        if (lobbyPlayers.TryGetValue(playerId, out GameObject playerEntry))
        {
            var display = playerEntry.GetComponent<PlayerLobbyDisplay>();
            display.SetCharacter(characterIndex);
        }
    }

    private void RemovePlayerFromLobby(string playerId)
    {
        if (lobbyPlayers.TryGetValue(playerId, out GameObject playerEntry))
        {
            Destroy(playerEntry);
            lobbyPlayers.Remove(playerId);
        }
    }

    public async Task<bool> JoinFriendLobby(string friendId)
    {
        try
        {
            if (string.IsNullOrEmpty(friendId))
            {
                Debug.LogError("Friend ID is null or empty");
                return false;
            }

            if (currentUserId == friendId)
            {
                Debug.LogWarning("Cannot join your own lobby as friend");
                return false;
            }

            currentLobbyId = friendId;
            Debug.Log($"Attempting to join lobby: {currentLobbyId}");

            int characterIndex = await GetCharacterIndex(currentUserId);
            var lobbyRef = FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{friendId}");

            DataSnapshot snapshot = await lobbyRef.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.Log("Lobby doesn't exist, creating new one");
                return await CreateLobby(friendId, characterIndex);
            }
            else
            {
                Debug.Log("Joining existing lobby");
                return await JoinExistingLobby(characterIndex, friendId);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error joining friend lobby: {ex}");
            return false;
        }
    }

    private async Task<int> GetCharacterIndex(string userId)
    {
        var snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/character")
            .GetValueAsync();

        return snapshot.Exists ? int.Parse(snapshot.Value.ToString()) : 0;
    }

    private async Task<bool> CreateLobby(string friendId, int characterIndex)
    {
        try
        {
            currentLobbyId = friendId;

            var lobbyRef = FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{currentLobbyId}");

            var lobbyData = new Dictionary<string, object>
            {
                {"hostId", friendId},
            };

            var playerData = new Dictionary<string, object>
            {
                {"userId", currentUserId},
                {"characterIndex", characterIndex},
                {"isReady", false}
            };

            await lobbyRef.UpdateChildrenAsync(lobbyData);
            await lobbyRef.Child("players").Child(currentUserId).SetValueAsync(playerData);

            SetupLobbyPlayersListener();
            lobbyStatusText.text = "Lobby creado! Esperando al amigo...";

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating lobby: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> JoinExistingLobby(int characterIndex, string friendId)
    {
        try
        {
            currentLobbyId = friendId;

            var playerData = new Dictionary<string, object>
            {
                {"userId", currentUserId},
                {"characterIndex", characterIndex},
            };

            await FirebaseDatabase.DefaultInstance
                .GetReference($"lobbies/{currentLobbyId}/players/{currentUserId}")
                .SetValueAsync(playerData);

            SetupLobbyPlayersListener();
            lobbyStatusText.text = "Te has unido al lobby!";

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error joining existing lobby: {ex.Message}");
            return false;
        }
    }

    public void LeaveLobby()
    {
        if (!string.IsNullOrEmpty(currentLobbyId))
        {
            // Remover jugador del lobby
            FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{currentLobbyId}/players/{currentUserId}")
                .RemoveValueAsync();
            // Remover listeners
            if (lobbyPlayersRef != null)
            {
                lobbyPlayersRef.ChildAdded -= HandlePlayerAdded;
                lobbyPlayersRef.ChildRemoved -= HandlePlayerRemoved;
                lobbyPlayersRef.ChildChanged -= HandlePlayerChanged;
                lobbyPlayersRef = null;
            }


            // Verificar si el lobby quedó vacío
            CheckAndCleanEmptyLobby();
        }

        ClearLobbyPlayers();
        currentLobbyId = null;
    }

    private void CheckAndCleanEmptyLobby()
    {
        FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{currentLobbyId}/players")
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && !task.Result.Exists)
                {
                    FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{currentLobbyId}")
                        .RemoveValueAsync();
                }
            });
    }

    private void ClearLobbyPlayers()
    {
        foreach (var player in lobbyPlayers.Values)
        {
            Destroy(player);
        }
        lobbyPlayers.Clear();
    }

    public async void ChangeCharacter(int newCharacterIndex)
    {
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/character")
            .SetValueAsync(newCharacterIndex);

        if (!string.IsNullOrEmpty(currentLobbyId))
        {
            await FirebaseDatabase.DefaultInstance
                .GetReference($"lobbies/{currentLobbyId}/players/{currentUserId}/characterIndex")
                .SetValueAsync(newCharacterIndex);
        }
    }

    private void OnDisable()
    {
        if (lobbyPlayersRef != null)
        {
            lobbyPlayersRef.ChildAdded -= HandlePlayerAdded;
            lobbyPlayersRef.ChildRemoved -= HandlePlayerRemoved;
            lobbyPlayersRef.ChildChanged -= HandlePlayerChanged;
        }

    }
}