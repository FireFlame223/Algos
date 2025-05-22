using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.AI.Navigation;

public class DungeonGenerator : MonoBehaviour
{
    public int mainRoomSize = 30; // Size of the initial large room
    public float splitDeviation = 0.2f; // How much the split point can deviate from the middle (0.5 = 50% deviation)
    public int doorWidth = 2; // Width of doors between rooms
    public float graphGenerationDelay = 0.5f; // Delay between graph generation steps for visualization

    public GraphVisualizer graphVisualizer; // Reference to the graph visualizer component

    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private GameObject wallPrefab; // Prefab for the wall segments
    [SerializeField] private GameObject floorPrefab; // Prefab for the floor segments
    private int splitsNumber = 4; // Maximum number of times we'll split rooms
    private List<RectInt> rooms = new List<RectInt>(); // Store generated rooms
    private List<Vector2Int> doors = new List<Vector2Int>(); // Store door positions
    private List<DoorInfo> doorInfos = new List<DoorInfo>(); // Store door orientation information
    private Graph dungeonGraph; // The graph representing the dungeon structure

    void Start()
    {
        dungeonGraph = new Graph();
        StartCoroutine(GenerateRooms());
        BakeNavMesh();
    }

    public IEnumerator GenerateRooms()
    {
        // To ensure safity, clear any existing rooms and doors and create the initial large room
        rooms.Clear();
        doors.Clear();
        RectInt initialRoom = new RectInt(0, 0, mainRoomSize, mainRoomSize);
        rooms.Add(initialRoom);

        // Perform the specified number of splits
        for (int i = 0; i < splitsNumber; i++)
        {
            // Create a new list to store rooms after current split
            List<RectInt> newRooms = new List<RectInt>();
            
            // Process each existing room
            foreach (var room in rooms)
            {
                // Randomly decide whether to split vertically or horizontally
                bool splitVertically = Random.value > 0.5f;
                
                // Check if room is too small for the chosen split direction.
                // If so, switch the split direction.
                if (splitVertically && room.width < 4)
                {
                    splitVertically = false;
                }
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
            yield return new WaitForSeconds(1f);
        }

        yield return StartCoroutine(PlaceDoors());
    }

    // Helper class to store door information
    private class DoorInfo
    {
        public Vector2Int position; // Position of the door
        public bool isVertical; // Whether the door is vertical
        // Indices of the rooms that the door connects
        public int room1Index;
        public int room2Index;
    }

    IEnumerator PlaceDoors()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Clear existing data structures
        doors.Clear();
        doorInfos.Clear();
        dungeonGraph = new Graph();
        
        //Create nodes for each room
        for (int i = 0; i < rooms.Count; i++)
        {
            Vector2 roomCenter = new Vector2(
                rooms[i].x + rooms[i].width / 2f,
                rooms[i].y + rooms[i].height / 2f
            );
            dungeonGraph.AddNode(i, roomCenter);
            
            // Update visualizer after each room node
            if (graphVisualizer != null)
            {
                graphVisualizer.SetGraph(dungeonGraph);
            }
            yield return new WaitForSeconds(graphGenerationDelay);
        }
        
        // Create doors and nodes for the dungeon and connect them to rooms and room noeds
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                RectInt room1 = rooms[i];
                RectInt room2 = rooms[j];

                // Check for vertical wall sharing
                if ((room1.xMax == room2.xMin || room1.xMin == room2.xMax) && 
                    !(room1.yMax <= room2.yMin || room1.yMin >= room2.yMax))
                {
                    // Find overlap area
                    int yStart = Mathf.Max(room1.yMin, room2.yMin);
                    int yEnd = Mathf.Min(room1.yMax, room2.yMax);
                    
                    // Make sure overlap is big enough for a door
                    if (yEnd - yStart > 2)
                    {
                        // Place door somewhere in the middle of the overlap, avoiding corners
                        int doorY = Random.Range(yStart + 1, yEnd - 1);
                        int doorX = room1.xMax == room2.xMin ? room1.xMax : room1.xMin;
                        
                        // Create door node
                        int doorNodeId = rooms.Count + doors.Count;
                        Vector2 doorPosition = new Vector2(doorX, doorY);
                        dungeonGraph.AddNode(doorNodeId, doorPosition);
                        
                        // Update visualizer after adding door node
                        if (graphVisualizer != null)
                        {
                            graphVisualizer.SetGraph(dungeonGraph);
                        }
                        yield return new WaitForSeconds(graphGenerationDelay);
                        
                        // Connect door to both rooms
                        dungeonGraph.AddEdge(i, doorNodeId);
                        dungeonGraph.AddEdge(j, doorNodeId);
                        
                        // Update visualizer after adding connections
                        if (graphVisualizer != null)
                        {
                            graphVisualizer.SetGraph(dungeonGraph);
                        }
                        yield return new WaitForSeconds(graphGenerationDelay);
                        
                        // Store door information
                        DoorInfo newDoor = new DoorInfo { 
                            position = new Vector2Int(doorX, doorY),
                            isVertical = true,
                            room1Index = i,
                            room2Index = j
                        };
                        
                        doors.Add(newDoor.position);
                        doorInfos.Add(newDoor);
                    }
                }
                // Check for horizontal wall sharing
                else if ((room1.yMax == room2.yMin || room1.yMin == room2.yMax) && 
                         !(room1.xMax <= room2.xMin || room1.xMin >= room2.xMax))
                {
                    // Find overlap area
                    int xStart = Mathf.Max(room1.xMin, room2.xMin);
                    int xEnd = Mathf.Min(room1.xMax, room2.xMax);
                    
                    // Make sure overlap is big enough for a door
                    if (xEnd - xStart > 2)
                    {
                        // Place door somewhere in the middle of the overlap, avoiding corners
                        int doorX = Random.Range(xStart + 1, xEnd - 1);
                        int doorY = room1.yMax == room2.yMin ? room1.yMax : room1.yMin;
                        
                        // Create door node
                        int doorNodeId = rooms.Count + doors.Count;
                        Vector2 doorPosition = new Vector2(doorX, doorY);
                        dungeonGraph.AddNode(doorNodeId, doorPosition);
                        
                        // Update visualizer after adding door node
                        if (graphVisualizer != null)
                        {
                            graphVisualizer.SetGraph(dungeonGraph);
                        }
                        yield return new WaitForSeconds(graphGenerationDelay);
                        
                        // Connect door to both rooms
                        dungeonGraph.AddEdge(i, doorNodeId);
                        dungeonGraph.AddEdge(j, doorNodeId);
                        
                        // Update visualizer after adding connections
                        if (graphVisualizer != null)
                        {
                            graphVisualizer.SetGraph(dungeonGraph);
                        }
                        yield return new WaitForSeconds(graphGenerationDelay);
                        
                        // Store door information
                        DoorInfo newDoor = new DoorInfo { 
                            position = new Vector2Int(doorX, doorY),
                            isVertical = false,
                            room1Index = i,
                            room2Index = j
                        };
                        
                        doors.Add(newDoor.position);
                        doorInfos.Add(newDoor);
                    }
                }
            }
        }

        // Spawn the physical dungeon assets after all rooms and doors are generated
        SpawnDungeonAssets();
    }
    
    void Update()
    {
        // Draw each room
        foreach (var room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room, new Color(255, 0, 0));
        }
        
        // Draw each door
        foreach (var door in doors)
        {
            // Get each door's data from the list
            DoorInfo doorInfo = null;
            for (int i = 0; i < doorInfos.Count; i++) 
            {
                if (doorInfos[i].position == door) 
                {
                    doorInfo = doorInfos[i];
                    break;
                }
            }
            
            // Draw vertical doors
            if (doorInfo != null && doorInfo.isVertical)
            {
                Debug.DrawLine(
                    new Vector3(door.x, 0, door.y - doorWidth/2),
                    new Vector3(door.x, 0, door.y + doorWidth/2),
                    Color.blue,
                    0.1f
                );
            }
            // Draw horizontal doors
            else
            {
                Debug.DrawLine(
                    new Vector3(door.x - doorWidth/2, 0, door.y),
                    new Vector3(door.x + doorWidth/2, 0, door.y),
                    Color.blue,
                    0.1f
                );
            }
        }
    }

    private void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
    }

    private void SpawnDungeonAssets()
    {
        // Create parent objects at scene root level
        GameObject wallsParent = new GameObject("Walls");
        GameObject floorsParent = new GameObject("Floors");
        wallsParent.transform.SetParent(null); // Set to null to make it a root object
        floorsParent.transform.SetParent(null);

        // Get wall prefab dimensions
        float wallWidth = 0.5f;  // Wall prefab is 0.5 units wide
        float wallYOffset = 0.5f; // Raise walls by 0.5 units

        // Spawn walls and floors for each room
        foreach (var room in rooms)
        {
            // Calculate room boundaries
            float left = room.x;
            float right = room.x + room.width;
            float bottom = room.y;
            float top = room.y + room.height;

            // Spawn floors for the room
            for (float x = left; x < right; x += 1f)
            {
                for (float z = bottom; z < top; z += 1f)
                {
                    SpawnFloor(new Vector3(x + 0.5f, 0, z + 0.5f), floorsParent.transform);
                }
            }

            // Spawn walls for each side of the room
            // Top wall
            for (float x = left; x < right; x += wallWidth)
            {
                SpawnWall(new Vector3(x, wallYOffset, top), wallsParent.transform);
            }

            // Bottom wall
            for (float x = left; x < right; x += wallWidth)
            {
                SpawnWall(new Vector3(x, wallYOffset, bottom), wallsParent.transform);
            }

            // Left wall
            for (float z = bottom; z < top; z += wallWidth)
            {
                SpawnWall(new Vector3(left, wallYOffset, z), wallsParent.transform);
            }

            // Right wall
            for (float z = bottom; z < top; z += wallWidth)
            {
                SpawnWall(new Vector3(right, wallYOffset, z), wallsParent.transform);
            }
        }

        // Rebuild the nav mesh after spawning all assets
        BakeNavMesh();
    }

    private void SpawnWall(Vector3 position, Transform parent)
    {
        // Check if this position overlaps with any door
        bool isDoorPosition = false;
        foreach (var doorInfo in doorInfos)
        {
            if (doorInfo.isVertical)
            {
                // For vertical doors, check if we're within the door width on the correct wall
                if (Mathf.Abs(position.x - doorInfo.position.x) < 0.1f &&
                    Mathf.Abs(position.z - doorInfo.position.y) < doorWidth / 2f)
                {
                    isDoorPosition = true;
                    break;
                }
            }
            else
            {
                // For horizontal doors, check if we're within the door width on the correct wall
                if (Mathf.Abs(position.z - doorInfo.position.y) < 0.1f &&
                    Mathf.Abs(position.x - doorInfo.position.x) < doorWidth / 2f)
                {
                    isDoorPosition = true;
                    break;
                }
            }
        }

        // Only spawn wall if this position isn't a door
        if (!isDoorPosition)
        {
            GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, parent);
            wall.name = "Wall";
        }
    }

    private void SpawnFloor(Vector3 position, Transform parent)
    {
        GameObject floor = Instantiate(floorPrefab, position, Quaternion.Euler(90, 0, 0), parent);
        floor.name = "Floor";
    }
}