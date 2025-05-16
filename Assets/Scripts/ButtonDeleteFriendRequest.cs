using UnityEngine;

public class ButtonDeleteFriendRequest : MonoBehaviour
{
    [SerializeField] private FriendRequestManager requestManager;
    [SerializeField] private string friendId; // Asignar esto cuando creas el bot�n

    public void OnRejectButtonClicked()
    {
        requestManager.RejectFriendRequest(friendId);
    }
}