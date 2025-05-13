using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    RectInt room1 = new RectInt(0, 0, 50, 50);
    RectInt room2 = new RectInt(49, 0, 51, 50);

    void Update()
    {
        AlgorithmsUtils.DebugRectInt(room1, Color.green);
        AlgorithmsUtils.DebugRectInt(room2, Color.blue);
    }
}