using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using System.Threading.Tasks;
public class FriendManager : MonoBehaviour
{
    public static FriendManager Instance;

    private string currentUserId;
    public string CurrentUserId { get => currentUserId; }

    private void OnEnable()
    {
        currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        SetupResponseListener();

        Instance = this;
 
    }

    private void SetupResponseListener()
    {
        DatabaseReference responseRef = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/friendResponse");

        responseRef.ChildAdded += HandleFriendResponse;
    }

    private async void HandleFriendResponse(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        string friendId = snapshot.Key;
        int response = int.Parse(snapshot.Value.ToString());

        // Limpiar la respuesta inmediatamente
        await snapshot.Reference.RemoveValueAsync();

        if (response == 1) // Amigo aceptó tu solicitud
        {
            Debug.Log($"{friendId} aceptó tu solicitud de amistad");
            // Actualizar TU lista de amigos localmente
        }
        else if (response == 2) // Amigo te eliminó
        {

            RemoveFriend(friendId, "friends");
            Debug.Log($"{friendId} te ha eliminado de sus amigos");
            // Remover de TU lista de amigos localmente

        }
    }

    public void RemoveFriend(string targetUserId, string requestType)
    {
            FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/{requestType}/{targetUserId}")
            .SetValueAsync(null);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            DatabaseReference responseRef = FirebaseDatabase.DefaultInstance
                .GetReference($"users/{currentUserId}/friendResponse");
            responseRef.ChildAdded -= HandleFriendResponse;
        }
    }

    private void OnDisable()
    {
        DatabaseReference responseRef = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{currentUserId}/friendResponse");
        responseRef.ChildAdded -= HandleFriendResponse;
        Instance = null;
    }
}
