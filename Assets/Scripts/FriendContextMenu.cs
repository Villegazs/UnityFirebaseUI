using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;

public class FriendContextMenu : MonoBehaviour
{
    [SerializeField] private Button removeFriendButton;
    [SerializeField] private Button viewProfileButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Vector3 offset = new Vector3(0, 100, 0);

    private string friendId;
    private Transform targetFriendContainer;
    public void Initialize(string friendId, Transform friendContainer)
    {
        Vector3 mousePos = Input.mousePosition;
        this.GetComponent<RectTransform>().position = offset + mousePos;
        this.friendId = friendId;
        this.targetFriendContainer = friendContainer;

        removeFriendButton.onClick.AddListener(OnRemoveFriend);
        joinLobbyButton.onClick.AddListener(OnJoinLobby);
    }
    private async void OnJoinLobby()
    {
       LobbyManager.Instance.LeaveLobby();

        await LobbyManager.Instance.JoinFriendLobby(friendId);
        Destroy(gameObject);
    }

    private void OnRemoveFriend()
    {
        SendDeleteFriendMessage(friendId);
        FriendManager.Instance.RemoveFriend(friendId, "friends");
        Destroy(gameObject);
    }

    private void SendDeleteFriendMessage(string friendId)
    {
        // Verificar si la respuesta aún es válida
        var deleteFriendRequest = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{friendId}/friendResponse/{FriendManager.Instance.CurrentUserId}");
        deleteFriendRequest.SetValueAsync(2);
    }

    private void Update()
    {
        // Cerrar menú si se hace click fuera
        if (Input.GetMouseButtonDown(0) && !RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(), Input.mousePosition))
        {
            Destroy(gameObject);
        }
    }
}