using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DungeonGenerator : MonoBehaviour
{
    public int minRoomSize = 10;
    public int maxRoomSize = 30;
    public int maxRooms = 4;
    public int minRooms = 1;

    private List<RectInt> rooms = new List<RectInt>();

    void Start()
    {
        StartCoroutine(GenerateRoomsCoroutine());
    }

    IEnumerator GenerateRoomsCoroutine()
    {
        int numberOfRooms = Random.Range(minRooms, maxRooms + 1);
        rooms.Clear();

        for (int i = 0; i < numberOfRooms; i++)
        {
            int width = Random.Range(minRoomSize, maxRoomSize);
            int height = Random.Range(minRoomSize, maxRoomSize);
            rooms.Add(new RectInt(0, 0, width, height));
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log($"Rooms generated: {rooms.Count}");
    }

    void Update()
    {
        foreach (var room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room, new Color(255, 0, 0));
        }
    }
}