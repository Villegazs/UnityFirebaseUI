
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameCharacter;
    [SerializeField] private TMP_Text _selectionText;
    [SerializeField] private Image characterDisplay;
    [SerializeField] private Image characterStatsDisplay;
    [SerializeField] private Image characterBGDisplay;
    private int _currentIndex;

    private int selectedIndex = 0;
    CharacterManager characterManager;



    private void OnEnable()
    {
        characterManager = CharacterManager.Instance;

        selectedIndex = characterManager.characters[selectedIndex].indexCharacter;
    }


    public void NextAvatar()
    {
        _currentIndex = (_currentIndex + 1) % characterManager.characters.Count;
        selectedIndex = _currentIndex;
        Debug.Log(_currentIndex);
        SelectedClass(_currentIndex);


    }
    public void PreviousAvatar()
    {
        if (_currentIndex <= 0)
        {
            _currentIndex = characterManager.characters.Count - 1;
        }
        else
        {
            _currentIndex = (_currentIndex - 1) % characterManager.characters.Count;
        }
        SelectedClass(_currentIndex);

        Debug.Log(_currentIndex);
    }
    public void SelectedClass(int currentIndex)
    {
        selectedIndex = currentIndex;
        characterDisplay.sprite = characterManager.characters[selectedIndex].ClassImage;
        characterStatsDisplay.sprite = characterManager.characters[selectedIndex].ClassStatsImage;
        characterBGDisplay.sprite = characterManager.characters[selectedIndex].ClassBG;
        
        characterManager.selectedIndexCharacter = selectedIndex;
    }
}