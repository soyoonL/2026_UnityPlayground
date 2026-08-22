using UnityEngine;

[System.Serializable]
public struct EvolutionData
{
    public string stageName;
    public Sprite characterSprite;
    public int damage;
    public int requiredPoint;
}

[CreateAssetMenu(fileName = "NewCharacter",menuName = "ScriptableObjects/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("캐릭터 정보")]
    public string characterName; // 캐릭터 이름
    public Sprite icon;
    public int price;
    public bool isUnlocked;

    [Header("진화 데이터")]
    public EvolutionData[] evolutionStages;
}
