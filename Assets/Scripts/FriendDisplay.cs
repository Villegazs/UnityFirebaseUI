using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FriendDisplay : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] Image characterImage;
    [SerializeField] private GameObject contextMenuPrefab;
    public string FriendId { get; private set; }
    public string FriendName { get; private set; }
    public int CharacterIndex { get; private set; }
    // Agrega estas propiedades públicas

    public void Initialize(string friendId, string friendName, int characterIndex, bool isOnline)
    {
        FriendId = friendId;
        FriendName = friendName;
        nameText.text = friendName;
        characterImage.sprite = CharacterManager.Instance.characters[characterIndex].ClassImage;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ShowContextMenu();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Aquí puedes agregar la lógica para el click izquierdo
            OnLeftClick();
        }
    }

    private void ShowContextMenu()
    {
        // Destruir cualquier menú previo
        foreach (var menu in FindObjectsOfType<FriendContextMenu>())
        {
            Destroy(menu.gameObject);
        }

        // Crear nuevo menú
        if (contextMenuPrefab == null) return;

        GameObject menuObj = Instantiate(contextMenuPrefab, this.GetComponentInParent<ScrollRect>().transform);
        menuObj.GetComponent<FriendContextMenu>().Initialize(FriendId, transform);

        // Posicionar el menú cerca del cursor

    }

    private void OnLeftClick()
    {
        // Implementa aquí lo que debe suceder con un click izquierdo
        Debug.Log($"Click izquierdo en {FriendName} (ID: {FriendId})");

        // Por ejemplo, podrías:
        // 1. Abrir un chat con este amigo
        // 2. Seleccionar el amigo
        // 3. Cualquier otra acción que necesites
    }

    private void OnApplicationQuit()
    {
        Destroy(gameObject);
    }
}