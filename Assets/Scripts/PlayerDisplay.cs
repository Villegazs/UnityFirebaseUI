using TMPro;
using UnityEngine;

public class PlayerDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameText;

    public void Initialize(string playerId, string username)
    {
        usernameText.text = username;
    }
}