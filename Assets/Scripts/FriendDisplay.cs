using TMPro;
using UnityEngine;

public class FriendDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;

    public string FriendId { get; private set; }
    public string FriendName { get; private set; }

    public void Initialize(string friendId, string friendName)
    {
        FriendId = friendId;
        FriendName = friendName;
        nameText.text = friendName;
    }
}