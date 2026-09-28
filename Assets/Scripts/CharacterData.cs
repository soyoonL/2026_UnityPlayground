using UnityEngine;

[System.Serializable]
public struct EvolutionData
{
    public string stageName;
    public Sprite characterSprite;
    public AnimationClip idleClip;   // 이 캐릭터/진화단계의 Idle 클립
    public AnimationClip cheerClip;  // 이 캐릭터/진화단계의 Cheer 클립
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
    public Projectile projectilePrefab; // 캐릭터 전용 발사체 프리팹

    [Header("실시간 변경 데이터")]
    public bool isUnlocked;
    public int currentStage = 0;

    [Header("진화 데이터")]
    public EvolutionData[] evolutionStages;

    [Header("오디오 설정")]
    public SFXType shootSfxType; // 캐릭터 고유 발사 음원 타입 선택

    public void ResetData()
    {
        isUnlocked = defaultIsUnlocked;
        currentStage = 0;
    }
}
