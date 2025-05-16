using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSendRequest : MonoBehaviour
{
    [SerializeField] private Button _addFriendButton;
    [SerializeField] private string friendUserId;
    [SerializeField] private TextMeshProUGUI statusText;

    private string currentUserId;

    void Start()
    {
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        _addFriendButton.onClick.AddListener(HandleAddFriendButtonClicked);
    }

    private async void HandleAddFriendButtonClicked()
    {
        // 1. Verificar autoenvío
        if (friendUserId == currentUserId)
        {
            UpdateStatus("No puedes añadirte a ti mismo");
            return;
        }

        // 2. Verificar ID vacío
        if (string.IsNullOrEmpty(friendUserId))
        {
            UpdateStatus("ID de amigo no válido");
            return;
        }

        try
        {
            UpdateStatus("Verificando...");

            // 3. Verificar si el usuario existe
            bool userExists = await CheckIfUserExists(friendUserId);
            if (!userExists)
            {
                UpdateStatus("Usuario no encontrado");
                return;
            }

            // 4. Verificar si ya son amigos
            bool alreadyFriends = await CheckIfFriends(currentUserId, friendUserId);
            if (alreadyFriends)
            {
                UpdateStatus("Ya son amigos");
                return;
            }

            // 5. Verificar solicitud existente
            bool requestExists = await CheckIfRequestExists(friendUserId, currentUserId);
            if (requestExists)
            {
                UpdateStatus("Solicitud ya enviada");
                return;
            }

            // Si pasa todas las validaciones, enviar solicitud
            await SendFriendRequest(friendUserId);
            UpdateStatus("¡Solicitud enviada!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error: {ex.Message}");
            UpdateStatus("Error al procesar");
        }
    }

    private async Task<bool> CheckIfUserExists(string userId)
    {
        var snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}")
            .GetValueAsync();
        return snapshot.Exists;
    }

    private async Task<bool> CheckIfFriends(string userId1, string userId2)
    {
        var snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId1}/friends/{userId2}")
            .GetValueAsync();
        return snapshot.Exists;
    }

    private async Task<bool> CheckIfRequestExists(string targetUserId, string senderUserId)
    {
        var snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{targetUserId}/friendRequests/{senderUserId}")
            .GetValueAsync();
        return snapshot.Exists;
    }

    private async Task SendFriendRequest(string targetUserId)
    {
        // Obtener nombre de usuario para mostrar en la solicitud
        var usernameSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/username")
            .GetValueAsync();
        string username = usernameSnapshot.Value?.ToString() ?? "Usuario";

        // Enviar solicitud al destinatario
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{targetUserId}/friendRequests/{currentUserId}")
            .SetValueAsync(username);

        // Registrar en el remitente
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/SendRequests/{targetUserId}")
            .SetValueAsync(1);
    }

    private void UpdateStatus(string message)
    {
        Debug.Log(message);
        if (statusText != null) statusText.text = message;
    }

    // Método público para asignar el friendUserId dinámicamente
    public void SetFriendId(string id)
    {
        friendUserId = id;
    }
}
