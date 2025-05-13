using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int minRoomSize = 25;
    public int maxRooms = 4;

    private RectInt room1, room2, room3, room4;

    void Start()
    {
        GenerateRooms();
    }

    void GenerateRooms()
    {
        int numberOfRooms = Random.Range(1, maxRooms + 1);
        RectInt[] rooms = new RectInt[numberOfRooms];

        for (int i = 0; i < numberOfRooms; i++)
        {
            int width = Random.Range(minRoomSize, 100);
            int height = Random.Range(minRoomSize, 50);
            rooms[i] = new RectInt(0, 0, width, height);
        }

        if (numberOfRooms > 0)
        {
            room1 = rooms[0];
        }
        else
        {
            room1 = new RectInt(0, 0, 0, 0);
        }

        if (numberOfRooms > 1)
        {
            room2 = rooms[1];
        }
        else
        {
            room2 = new RectInt(0, 0, 0, 0);
        }

        if (numberOfRooms > 2)
        {
            room3 = rooms[2];
        }
        else
        {
            room3 = new RectInt(0, 0, 0, 0);
        }

        if (numberOfRooms > 3)
        {
            room4 = rooms[3];
        }
        else
        {
            room4 = new RectInt(0, 0, 0, 0);
        }
    }

    void Update()
    {
        AlgorithmsUtils.DebugRectInt(room1, Color.green);
        AlgorithmsUtils.DebugRectInt(room2, Color.blue);
        AlgorithmsUtils.DebugRectInt(room3, Color.yellow);
        AlgorithmsUtils.DebugRectInt(room4, Color.red);
    }
}