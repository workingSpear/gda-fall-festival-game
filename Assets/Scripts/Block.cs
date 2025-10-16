using UnityEngine;

public class Block : MonoBehaviour
{
    public int size; //1 block, 2 block, 3 blocks, maximum of 4 blocks
    public string blockName, blockDescription;
    public Sprite blockSprite;
    public GameObject blockPrefab;

    public SpriteRenderer tint;
    public Color originalColor;
}
