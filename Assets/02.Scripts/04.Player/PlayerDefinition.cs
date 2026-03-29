using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player Definition")]
public class PlayerDefinition : ScriptableObject
{
    public string playerId;
    public GameObject playerPrefab;
}