using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int minRoomSize = 25;

    private RectInt room1, room2, room3, room4;

    void Start()
    {
        GenerateRooms();
    }

    void GenerateRooms()
    {
        int width1 = Random.Range(minRoomSize, 100);
        int width2 = Random.Range(minRoomSize, 100);
        int height1 = Random.Range(minRoomSize, 50);
        int height2 = Random.Range(minRoomSize, 50);
        room1 = new RectInt(0, 0, width1, height1);
        room2 = new RectInt(0, 24, width2, height2);
        room3 = new RectInt(0, 0, width1, height1);
        room4 = new RectInt(49, 0, width2, height2);
    }

    void Update()
    {
        AlgorithmsUtils.DebugRectInt(room1, Color.green);
        AlgorithmsUtils.DebugRectInt(room2, Color.blue);
        AlgorithmsUtils.DebugRectInt(room3, Color.yellow);
        AlgorithmsUtils.DebugRectInt(room4, Color.red);
    }
}