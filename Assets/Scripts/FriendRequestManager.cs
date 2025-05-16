using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using UnityEngine;

public class FriendRequestManager : MonoBehaviour
{
    private string currentUserId;

    void OnEnable()
    {
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        SetupRequestListeners();
    }

    private void SetupRequestListeners()
    {
        var dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // Escuchar nuevas respuestas a solicitudes enviadas
        var reference = dbRef.Child("users")
             .Child(currentUserId)
             .Child("SendRequests");
        
        reference.ChildAdded += HandleRequestResponseAdded;
    }

    private async void HandleRequestResponseAdded(object sender, ChildChangedEventArgs args)
    {
        Debug.Log("HandleRequest");
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        string friendId = snapshot.Key;
        int responseStatus = int.Parse(snapshot.Value.ToString());

        // Verificar si la respuesta aún es válida
        var requestCheck = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/SendRequests/{friendId}")
            .GetValueAsync();

        if (requestCheck.Value == null)
        {
            Debug.Log($"Respuesta obsoleta de {friendId}");
            await RemoveRequest(friendId, "SendRequests");
            return;
        }

        // Obtener nombre del amigo
        string friendUsername = await GetUserUsername(friendId);

        switch (responseStatus)
        {
            case 1: // Solicitud aceptada
                await HandleAcceptedRequest(friendId, friendUsername);
                break;

            case 2: // Solicitud rechazada
                Debug.Log($"{friendUsername} ha rechazado tu solicitud");
                break;

            default:
                Debug.Log($"Estado de respuesta no reconocido: {responseStatus}");
                break;
        }

        // Limpiar solicitudes de ambos lados
        //await CleanupRequests(friendId);
    }

    private async Task HandleAcceptedRequest(string friendId, string friendUsername)
    {
        var dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        Debug.Log($"{friendUsername} ha aceptado tu solicitud");

        // Añadir a la lista de amigos del usuario actual
        await dbRef.Child("users")
                  .Child(currentUserId)
                  .Child("friends")
                  .Child(friendId)
                  .SetValueAsync(friendUsername);

        await CleanupRequests(friendId);
    }

    private async Task CleanupRequests(string friendId)
    {
        await RemoveRequest(currentUserId, "friendRequests", friendId);
        // Eliminar de SendRequests del usuario actual
        await RemoveRequest(friendId, "SendRequests");

        // Eliminar de friendResponse del amigo (si existe)
       
    }

    private async Task<string> GetUserUsername(string userId)
    {
        var usernameSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/username")
            .GetValueAsync();

        return usernameSnapshot.Value?.ToString() ?? "Usuario desconocido";
    }

    private async Task RemoveRequest(string targetUserId, string requestType, string userId = null)
    {
        userId = userId ?? currentUserId;

        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/{requestType}/{targetUserId}")
            .SetValueAsync(null);
    }

    // Método para rechazar una solicitud (como en tu botón original)
    public async void RejectFriendRequest(string friendId)
    {
        // Actualizar el estado a 2 (rechazado) en el nodo friendResponse del amigo
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{friendId}/SendRequests/{currentUserId}")
            .SetValueAsync(2);

        Debug.Log("Solicitud rechazada");
    }

    private void OnDisable()
    {
        var dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // Escuchar nuevas respuestas a solicitudes enviadas
        var reference = dbRef.Child("users")
             .Child(currentUserId)
             .Child("SendRequests");

        reference.ChildAdded -= HandleRequestResponseAdded;
    }
}