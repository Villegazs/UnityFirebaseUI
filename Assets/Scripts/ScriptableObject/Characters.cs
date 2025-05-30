using UnityEngine;

[CreateAssetMenu(fileName = "Characters", menuName = "Scriptable Objects/Characters")]
public class Characters : ScriptableObject
{
    public int indexCharacter;

    public string ClassName;
    public Sprite ClassImage;
    public Sprite ClassStatsImageLobby;
    public Sprite ClassStatsImage;
    public Sprite ClassBG;
}
