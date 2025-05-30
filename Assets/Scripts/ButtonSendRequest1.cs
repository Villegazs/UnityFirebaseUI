using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSendRequest : MonoBehaviour
{
    [SerializeField] private Button _addFriendButton;
    [SerializeField] private string friendUsername;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TMP_InputField addFriendInputField;

    private string currentUserId;
    private string currentUsername;

    void Start()
    {
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        // Obtener el username actual al iniciar
        GetCurrentUsername();
        _addFriendButton.onClick.AddListener(HandleAddFriendButtonClicked);
    }

    private async void GetCurrentUsername()
    {
        var snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/username")
            .GetValueAsync();

        if (snapshot.Exists)
        {
            currentUsername = snapshot.Value.ToString();
        }
    }

    private async void HandleAddFriendButtonClicked()
    {


        friendUsername = addFriendInputField.text.Trim();

        // 1. Verificar autoenvío (ahora por username)
        if (friendUsername == currentUsername)
        {
            UpdateStatus("No puedes añadirte a ti mismo");
            return;
        }

        // 2. Verificar username vacío
        if (string.IsNullOrEmpty(friendUsername))
        {
            UpdateStatus("Nombre de usuario no válido");
            return;
        }

        try
        {
            UpdateStatus("Verificando...");

            // 3. Verificar si el usuario existe y obtener su ID
            string friendUserId = await GetUserIdByUsername(friendUsername);
            if (string.IsNullOrEmpty(friendUserId))
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
            UpdateStatus("¡Solicitud enviada!");
            await SendFriendRequest(friendUserId);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error: {ex.Message}");
            UpdateStatus("Error al procesar");
        }
    }

    private async Task<string> GetUserIdByUsername(string username)
    {
        // Buscar el user ID correspondiente al username
        var snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference("usernames")
            .OrderByValue()
            .EqualTo(username)
            .GetValueAsync();

        if (snapshot.Exists && snapshot.ChildrenCount > 0)
        {
            foreach (DataSnapshot child in snapshot.Children)
            {
                return child.Key; // Devuelve el user ID
            }
        }
        return null;
    }

    private async Task<bool> CheckIfFriends(string userId1, string userId2)
    {
        Debug.Log("Check If friend exists");
        var initialSnapshot = await FirebaseDatabase.DefaultInstance
                               .GetReference($"users/{userId2}/friends")
                               .GetValueAsync();

        if (!initialSnapshot.Exists)
            return false;

        // Verificar ambas direcciones para asegurar una relación bidireccional
        var snapshot1 = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId1}/friends/{userId2}")
            .GetValueAsync();

        var snapshot2 = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId2}/friends/{userId1}")
            .GetValueAsync();

        return snapshot1.Exists && snapshot2.Exists;
    }

    private async Task<bool> CheckIfRequestExists(string targetUserId, string senderUserId)
    {
        Debug.Log("Check If Request exists");

        var snapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{targetUserId}/friendRequests/{senderUserId}")
            .GetValueAsync();
        return snapshot.Exists;
    }

    private async Task SendFriendRequest(string targetUserId)
    {
        // Enviar solicitud al destinatario con el username del remitente
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{targetUserId}/friendRequests/{currentUserId}")
            .SetValueAsync(currentUsername);

        // Registrar en el remitente
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/SendRequests/{targetUserId}")
            .SetValueAsync(0);
    }

    private void UpdateStatus(string message)
    {
        Debug.Log(message);
        if (statusText != null) statusText.text = message;
    }

    // Método público para asignar el friendUsername dinámicamente
    public void SetFriendUsername(string username)
    {
        friendUsername = username;
    }
}