using Firebase.Auth;
using Firebase.Database;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FriendRequestManager : MonoBehaviour
{
    private string currentUserId;
    [SerializeField] private GameObject friendRequestPrefab;
    [SerializeField] private Transform requestsContainer;

    private Dictionary<string, FriendRequestDisplay> instantiatedFriendsRequest = new Dictionary<string, FriendRequestDisplay>();

    void OnEnable()
    {
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        SetupRequestListeners();
    }

    private void SetupRequestListeners()
    {
        var dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // Escuchar respuestas a solicitudes enviadas (SendRequests)
            var sentRequestsRef = dbRef.Child("users")
             .Child(currentUserId)
             .Child("SendRequests");
        sentRequestsRef.ChildAdded += HandleRequestResponseAdded;

        // Escuchar nuevas solicitudes recibidas (friendRequests)
        var receivedRequestsRef = dbRef.Child("users")
             .Child(currentUserId)
             .Child("friendRequests");
        receivedRequestsRef.ChildAdded += HandleNewRequestAdded;
        receivedRequestsRef.ChildRemoved += HandleRequestRemoved;
    }
    private async void HandleNewRequestAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        string senderId = snapshot.Key;
        string senderData = snapshot.Value.ToString();

        // Parsear datos del solicitante (formato: "Nombre|score")
        string[] senderParts = senderData.Split('|');
        string senderName = senderParts[0];

        if (!instantiatedFriendsRequest.ContainsKey(senderId))
        {
            GameObject requestObj = Instantiate(friendRequestPrefab, requestsContainer);
            FriendRequestDisplay display = requestObj.GetComponent<FriendRequestDisplay>();
            display.Initialize(senderId, senderName, HandleRequestDecision);
            instantiatedFriendsRequest.Add(senderId, display);
        }
    }

    private void HandleRequestRemoved(object sender, ChildChangedEventArgs args)
    {
        string senderId = args.Snapshot.Key;
        if (instantiatedFriendsRequest.TryGetValue(senderId, out var display))
        {
            Destroy(display.gameObject);
            instantiatedFriendsRequest.Remove(senderId);
        }
    }
    private async void HandleRequestDecision(string senderId, int decision)
    {
        // decision: 1 = Aceptar, 2 = Rechazar
        if (decision == 1)
        {
            // Añadir como amigos en ambos usuarios
            string senderName = await GetUserUsername(senderId);
            await AddFriend(currentUserId, senderId, senderName);
            //await AddFriend(senderId, currentUserId, FirebaseAuth.DefaultInstance.CurrentUser.DisplayName);
        }

        // Eliminar la solicitud
        await RemoveRequest(senderId, "friendRequests");

        // Notificar al remitente de la decisión
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{senderId}/SendRequests/{currentUserId}")
            .SetValueAsync(decision);
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

        string friendUsername = await GetUserUsername(friendId);

        switch (responseStatus)
        {
            case 1: // Solicitud aceptada
                await HandleAcceptedRequest(friendId, friendUsername);
                break;

            case 2: // Solicitud rechazada
                await HandleDeclineRequest(friendId, friendUsername);
                break;

            default:
                Debug.Log($"Estado de respuesta no reconocido: {responseStatus}");
                break;
        }

        //await RemoveRequest(friendId, "SendRequests");
    }

    private async Task HandleAcceptedRequest(string friendId, string friendUsername)
    {
        Debug.Log($"{friendUsername} ha aceptado tu solicitud");

        // Añadir a la lista de amigos del usuario actual
        await AddFriend(currentUserId, friendId, friendUsername);
        await RemoveRequest(friendId, "SendRequests");
    }
    private async Task HandleDeclineRequest(string friendId, string friendUsername)
    {
        Debug.Log($"{friendUsername} ha rechazado tu solicitud");
        await RemoveRequest(friendId, "SendRequests");
    }

    private async Task AddFriend(string userId, string friendId, string friendName)
    {
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/friends/{friendId}")
            .SetValueAsync(friendName);
    }

    private async Task<string> GetUserUsername(string userId)
    {
        var usernameSnapshot = await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{userId}/username")
            .GetValueAsync();

        return usernameSnapshot.Value?.ToString() ?? "Usuario desconocido";
    }

    private async Task RemoveRequest(string targetUserId, string requestType)
    {
        await FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/{requestType}/{targetUserId}")
            .SetValueAsync(null);
    }

    private void OnDisable()
    {
        var dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        var sentRequestsRef = dbRef.Child("users")
             .Child(currentUserId)
             .Child("SendRequests");
        sentRequestsRef.ChildAdded -= HandleRequestResponseAdded;

        var receivedRequestsRef = dbRef.Child("users")
             .Child(currentUserId)
             .Child("friendRequests");
       receivedRequestsRef.ChildAdded -= HandleNewRequestAdded;
       receivedRequestsRef.ChildRemoved -= HandleRequestRemoved;
    }
}