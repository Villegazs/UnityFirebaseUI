using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance;
    public int selectedIndexCharacter = 0;
    public List<Characters> characters;


    private void Awake()
    {
        if (CharacterManager.Instance == null)
        {
            CharacterManager.Instance = this;
        }

    }
}