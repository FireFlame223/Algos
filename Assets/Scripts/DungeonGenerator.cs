using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    void Update()
    {
        RectInt room = new RectInt(0, 0, 100, 50);
        AlgorithmsUtils.DebugRectInt(room, Color.green);
    }
}
