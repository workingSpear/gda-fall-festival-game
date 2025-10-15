using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2 position;

    void Start()
    {
        position = new Vector2(transform.localPosition.x, transform.localPosition.y);
    }
}
