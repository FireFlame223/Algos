using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public bool splitHorizontally = false;

    void Update()
    {
        RectInt room1, room2;
        if (splitHorizontally)
        {
            room1 = new RectInt(0, 0, 100, 25);
            room2 = new RectInt(0, 24, 100, 26);
        }
        else
        {
            room1 = new RectInt(0, 0, 50, 50);
            room2 = new RectInt(49, 0, 51, 50);
        }
        AlgorithmsUtils.DebugRectInt(room1, Color.green);
        AlgorithmsUtils.DebugRectInt(room2, Color.blue);
    }
}