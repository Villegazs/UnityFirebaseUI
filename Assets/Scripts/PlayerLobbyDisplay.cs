using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image characterStats;

    private string playerId;

    public void Initialize(string id, string username, int characterIndex)
    {
        playerId = id;
        usernameText.text = username;
        SetCharacter(characterIndex);
    }

    public void SetCharacter(int characterIndex)
    {
        if (CharacterManager.Instance != null &&
            characterIndex >= 0 &&
            characterIndex < CharacterManager.Instance.characters.Count)
        {
            characterImage.sprite = CharacterManager.Instance.characters[characterIndex].ClassImage;
            characterStats.sprite = CharacterManager.Instance.characters[characterIndex].ClassStatsImageLobby;

        }
    }
}