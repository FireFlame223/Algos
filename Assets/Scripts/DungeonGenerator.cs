using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public bool splitHorizontally = false;

    void Update()
    {
        RectInt room1, room2, room3, room4;
        if (splitHorizontally)
        {
            room1 = new RectInt(0, 0, 100, 25);
            room2 = new RectInt(0, 24, 100, 26);
            room3 = new RectInt(0, 0, 50, 25);
            room4 = new RectInt(49, 0, 51, 25);
        }
        else
        {
            room1 = new RectInt(0, 0, 50, 50);
            room2 = new RectInt(49, 0, 51, 50);
            room3 = new RectInt(0, 0, 50, 25);
            room4 = new RectInt(0, 24, 50, 26);
        }
        AlgorithmsUtils.DebugRectInt(room1, Color.green);
        AlgorithmsUtils.DebugRectInt(room2, Color.blue);
        AlgorithmsUtils.DebugRectInt(room3, Color.yellow);
        AlgorithmsUtils.DebugRectInt(room4, Color.red);
    }
}