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
    public bool defaultIsUnlocked;

    [Header("실시간 변경 데이터")]
    public bool isUnlocked;
    public int currentStage = 0;

    [Header("진화 데이터")]
    public EvolutionData[] evolutionStages;

    public void ResetData()
    {
        isUnlocked = defaultIsUnlocked;
        currentStage = 0;
    }
}
