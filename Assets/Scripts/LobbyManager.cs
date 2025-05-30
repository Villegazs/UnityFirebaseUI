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

        // Check if user is authenticated
        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.LogError("No authenticated user!");
            return;
        }

        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        currentLobbyId = currentUserId; // Use user ID as lobby ID
        lobbyPlayersRef = FirebaseDatabase.DefaultInstance
            .GetReference($"lobbies/{currentUserId}/players");
        lobbyPlayersRef.ValueChanged += HandleLobbyPlayersChanged;
        try
        {
            // Get character index
            int characterIndex = await GetCharacterIndex(currentUserId);

            // Create the lobby
            await CreateLobby(currentUserId, characterIndex);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in OnEnable: {ex.Message}");
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

            string lobbyId = friendId;
            currentLobbyId = lobbyId;
            Debug.Log($"Attempting to join lobby: {currentLobbyId}");

            // Get character index with proper error handling
            int characterIndex;
            try
            {
                characterIndex = await GetCharacterIndex(currentUserId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get character index: {ex.Message}");
                return false;
            }

            var lobbyRef = FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{friendId}");

            DataSnapshot snapshot;
            try
            {
                snapshot = await lobbyRef.GetValueAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to access lobby: {ex.Message}");
                return false;
            }

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

        if (snapshot.Exists)
        {
            return int.Parse(snapshot.Value.ToString());
        }
        return 0; // Valor por defecto
    }

    private async Task<bool> CreateLobby(string friendId, int characterIndex)
    {
        try
        {
            currentLobbyId = friendId; // The lobby ID is just the user's ID

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
            if (string.IsNullOrEmpty(friendId))
            {
                Debug.LogError("Friend ID is null or empty");
                return false;
            }
            currentLobbyId = friendId; // The lobby ID is just the friend's user ID

            var lobbyRef = FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{currentLobbyId}/players");
            SetupLobbyPlayersListener();


            var playerData = new Dictionary<string, object>
        {
            {"userId", currentUserId},
            {"characterIndex", characterIndex},
        };

            await lobbyRef.Child(currentUserId).SetValueAsync(playerData);

            

            lobbyStatusText.text = "Te has unido al lobby!";


            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error joining existing lobby: {ex.Message}");
            return false;
        }
    }

    private void SetupLobbyPlayersListener()
    {
        // Remove previous listener
        if (lobbyPlayersRef != null)
        {
            lobbyPlayersRef.ValueChanged -= HandleLobbyPlayersChanged;
        }

        if (string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogError("Tried to setup listener without currentLobbyId");
            return;
        }

        // Setup new listener with correct path
        lobbyPlayersRef = FirebaseDatabase.DefaultInstance
            .GetReference($"lobbies/{currentLobbyId}/players");

        lobbyPlayersRef.ValueChanged += HandleLobbyPlayersChanged;
    }
    private void HandleLobbyPlayersChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Check if we're still in a lobby
        if (string.IsNullOrEmpty(currentLobbyId))
        {
            return;
        }

        DataSnapshot snapshot = args.Snapshot;

        // Verify the snapshot is for our current lobby
        if (!snapshot.Reference.Equals($"lobbies/{currentLobbyId}/players"))
        {
            Debug.Log("Lobby Incorrecto");
            return;
        }

        if (!snapshot.Exists)
        {
            LeaveLobby();
            return;
        }

        // Update player list
        Dictionary<string, GameObject> currentPlayers = new Dictionary<string, GameObject>();

        foreach (DataSnapshot playerSnapshot in snapshot.Children)
        {
            string playerId = playerSnapshot.Key;

            if (!playerSnapshot.HasChild("characterIndex") || playerSnapshot.Child("characterIndex").Value == null)
            {
                Debug.LogWarning($"Player {playerId} is missing characterIndex");
                continue;
            }

            if (!int.TryParse(playerSnapshot.Child("characterIndex").Value.ToString(), out int characterIndex))
            {
                Debug.LogWarning($"Invalid characterIndex for player {playerId}");
                continue;
            }

            if (!lobbyPlayers.ContainsKey(playerId))
            {
                AddPlayerToLobby(playerId, characterIndex);
            }
            else
            {
                UpdatePlayerStatus(playerId, characterIndex);
            }

            if (lobbyPlayers.ContainsKey(playerId)) // Double-check it was added
            {
                currentPlayers[playerId] = lobbyPlayers[playerId];
            }
        }

        RemoveDisconnectedPlayers(currentPlayers);
    }

    private void AddPlayerToLobby(string playerId, int characterIndex)
    {
        // Obtener nombre del jugador
        FirebaseDatabase.DefaultInstance.GetReference($"users/{playerId}/username")
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.Exists)
                {
                    string username = task.Result.Value.ToString();

                    GameObject playerEntry = Instantiate(playerEntryPrefab, playersContainer);
                    var display = playerEntry.GetComponent<PlayerLobbyDisplay>();
                    display.Initialize(playerId, username, characterIndex);

                    lobbyPlayers[playerId] = playerEntry;
                }
            });
    }

    private void UpdatePlayerStatus(string playerId, int characterIndex)
    {
        if (lobbyPlayers.TryGetValue(playerId, out GameObject playerEntry))
        {
            var display = playerEntry.GetComponent<PlayerLobbyDisplay>();
            display.SetCharacter(characterIndex);
        }
    }

    private void RemoveDisconnectedPlayers(Dictionary<string, GameObject> currentPlayers)
    {
        List<string> toRemove = new List<string>();

        foreach (var player in lobbyPlayers)
        {
            if (!currentPlayers.ContainsKey(player.Key))
            {
                toRemove.Add(player.Key);
            }
        }

        foreach (string playerId in toRemove)
        {
            Destroy(lobbyPlayers[playerId]);
            lobbyPlayers.Remove(playerId);
        }
    }


    public void LeaveLobby()
    {
        if (!string.IsNullOrEmpty(currentLobbyId))
        {
            // Remover listener
            if (lobbyPlayersRef != null)
            {
                lobbyPlayersRef.ValueChanged -= HandleLobbyPlayersChanged;
                lobbyPlayersRef = null;
            }

            // Remover nuestro jugador del lobby
            FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{currentLobbyId}/players/{currentUserId}")
                .RemoveValueAsync();

            // Verificar si el lobby quedó vacío
            FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{currentLobbyId}/players")
                .GetValueAsync().ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully && !task.Result.Exists)
                    {
                        // Eliminar el lobby si está vacío
                        FirebaseDatabase.DefaultInstance.GetReference($"lobbies/{currentLobbyId}")
                            .RemoveValueAsync();
                    }
                });
        }

        // Limpiar UI
        ClearLobbyPlayers();
        currentLobbyId = null;
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
        // Actualizar localmente
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/character")
            .SetValueAsync(newCharacterIndex);

        // Actualizar en el lobby
        if (!string.IsNullOrEmpty(currentLobbyId))
        {
            await FirebaseDatabase.DefaultInstance
                .GetReference($"lobbies/{currentLobbyId}/players/{currentUserId}/characterIndex")
                .SetValueAsync(newCharacterIndex);
        }
    }

}