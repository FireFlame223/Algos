using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DungeonGenerator : MonoBehaviour
{
    public int mainRoomSize = 30; // Size of the initial large room
    public float splitDeviation = 0.2f; // How much the split point can deviate from the middle (0.5 = 50% deviation)

    private List<RectInt> rooms = new List<RectInt>(); // List to store all generated rooms
    private int maxSplits = 4; // Maximum number of times we'll split rooms

    void Start()
    {
        // Start the room generation process
        StartCoroutine(GenerateRoomsCoroutine());
    }

    IEnumerator GenerateRoomsCoroutine()
    {
        // To ensure safity, clear any existing rooms and create the initial large room
        rooms.Clear();
        RectInt initialRoom = new RectInt(0, 0, mainRoomSize, mainRoomSize);
        rooms.Add(initialRoom);

        // Perform the specified number of splits
        for (int i = 0; i < maxSplits; i++)
        {
            // Create a new list to store rooms after current split
            List<RectInt> newRooms = new List<RectInt>();
            
            // Process each existing room
            foreach (var room in rooms)
            {
                // Randomly decide whether to split vertically or horizontally
                bool splitVertically = Random.value > 0.5f;
                
                // Check if room is too small for the chosen split direction
                // If width < 4, switch to horizontal split
                if (splitVertically && room.width < 4)
                {
                    splitVertically = false;
                }
                // If height < 4, switch to vertical split
                else if (!splitVertically && room.height < 4)
                {
                    splitVertically = true;
                }

                if (splitVertically)
                {
                    // Calculate split point for vertical split
                    int middle = room.width / 2;
                    int deviation = Mathf.RoundToInt(room.width * splitDeviation);
                    int splitPoint = Random.Range(middle - deviation, middle + deviation);
                    int minSplit = middle - deviation;
                    int maxSplit = middle + deviation;
                    splitPoint = Mathf.Clamp(splitPoint, minSplit, maxSplit);

                    // Ensure both resulting rooms are at least 2 units wide
                    if (splitPoint < 2)
                    {
                        splitPoint = 2;
                    }
                    else if (room.width - splitPoint < 2)
                    {
                        splitPoint = room.width - 2;
                    }

                    // Create two new rooms from the vertical split
                    newRooms.Add(new RectInt(room.x, room.y, splitPoint, room.height));
                    newRooms.Add(new RectInt(room.x + splitPoint, room.y, room.width - splitPoint, room.height));
                }
                else
                {
                    // Calculate split point for horizontal split
                    int middle = room.height / 2;
                    int deviation = Mathf.RoundToInt(room.height * splitDeviation);
                    int splitPoint = Random.Range(middle - deviation, middle + deviation);
                    int minSplit = middle - deviation;
                    int maxSplit = middle + deviation;
                    splitPoint = Mathf.Clamp(splitPoint, minSplit, maxSplit);

                    // Ensure both resulting rooms are at least 2 units tall
                    if (splitPoint < 2)
                    {
                        splitPoint = 2;
                    }
                    else if (room.height - splitPoint < 2)
                    {
                        splitPoint = room.height - 2;
                    }

                    // Create two new rooms from the horizontal split
                    newRooms.Add(new RectInt(room.x, room.y, room.width, splitPoint));
                    newRooms.Add(new RectInt(room.x, room.y + splitPoint, room.width, room.height - splitPoint));
                }
            }
            // Update the rooms list with the new split rooms
            rooms = newRooms;
            // Wait before next split
            yield return new WaitForSeconds(1f);
        }

        // Log the total number of rooms generated
        Debug.Log($"Rooms generated: {rooms.Count}");
    }

    void Update()
    {
        // Draw each room in red
        foreach (var room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room, new Color(255, 0, 0));
        }
    }
}