using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.AI.Navigation;

// Generates a dungeon by splitting rooms (BSP), building a graph, and spawning floor/wall prefabs.
public class DungeonGenerator : MonoBehaviour
{
    private const int MinRoomSizeToSplit = 4;
    private const int MinChildRoomSize = 2;
    private const int MinDoorOverlap = 2;
    private const float BspStepDelay = 1f;

    private enum SplitAxis
    {
        Vertical,
        Horizontal
    }

    public int mainRoomSize = 30;
    public float splitDeviation = 0.2f;
    public int doorWidth = 2;
    public float graphGenerationDelay = 0.5f;
    public float floorTileSize = 1f;

    public GraphVisualizer graphVisualizer;

    [SerializeField] private int splitsNumber = 4;
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;

    private List<RectInt> rooms = new List<RectInt>();
    private List<DoorInfo> doorInfos = new List<DoorInfo>();
    private Graph dungeonGraph;

    // Stores door position, orientation, and which two rooms it connects.
    private class DoorInfo
    {
        public Vector2Int position;
        public bool isVertical;
        public int room1Index;
        public int room2Index;
    }

    // Entry point: start the full generation pipeline.
    void Start()
    {
        StartCoroutine(GenerateDungeon());
    }

    // Runs room splitting, then graph building.
    private IEnumerator GenerateDungeon()
    {
        yield return GenerateRooms();
    }

    // Phase 1: BSP room splitting. Starts with one room and splits it repeatedly.
    public IEnumerator GenerateRooms()
    {
        rooms.Clear();
        doorInfos.Clear();

        RectInt initialRoom = new RectInt(0, 0, mainRoomSize, mainRoomSize);
        rooms.Add(initialRoom);

        for (int i = 0; i < splitsNumber; i++)
        {
            List<RectInt> newRooms = new List<RectInt>();

            // Split every room in the current list into two smaller rooms.
            foreach (RectInt room in rooms)
            {
                SplitAxis axis = ChooseSplitAxis(room);
                SplitRoom(room, axis, newRooms);
            }

            rooms = newRooms;
            yield return new WaitForSeconds(BspStepDelay);
        }

        yield return BuildGraph();
    }

    // Picks vertical or horizontal split. Falls back if the room is too small for that direction.
    private SplitAxis ChooseSplitAxis(RectInt room)
    {
        bool splitVertically = Random.value > 0.5f;

        if (splitVertically && room.width < MinRoomSizeToSplit)
        {
            splitVertically = false;
        }
        else if (!splitVertically && room.height < MinRoomSizeToSplit)
        {
            splitVertically = true;
        }

        if (splitVertically)
        {
            return SplitAxis.Vertical;
        }

        return SplitAxis.Horizontal;
    }

    // Returns a random split position near the middle of the given width or height.
    private int CalculateSplitPoint(int size)
    {
        int middle = size / 2;
        int deviation = Mathf.RoundToInt(size * splitDeviation);
        return Random.Range(middle - deviation, middle + deviation);
    }

    // Cuts one room into two rectangles and adds them to the output list.
    private void SplitRoom(RectInt room, SplitAxis axis, List<RectInt> output)
    {
        if (axis == SplitAxis.Vertical)
        {
            int splitPoint = CalculateSplitPoint(room.width);

            // Ensure neither child room is thinner than MinChildRoomSize.
            if (splitPoint < MinChildRoomSize)
            {
                splitPoint = MinChildRoomSize;
            }
            else if (room.width - splitPoint < MinChildRoomSize)
            {
                splitPoint = room.width - MinChildRoomSize;
            }

            // Left child and right child.
            output.Add(new RectInt(room.x, room.y, splitPoint, room.height));
            output.Add(new RectInt(room.x + splitPoint, room.y, room.width - splitPoint, room.height));
        }
        else
        {
            int splitPoint = CalculateSplitPoint(room.height);

            if (splitPoint < MinChildRoomSize)
            {
                splitPoint = MinChildRoomSize;
            }
            else if (room.height - splitPoint < MinChildRoomSize)
            {
                splitPoint = room.height - MinChildRoomSize;
            }

            // Bottom child and top child.
            output.Add(new RectInt(room.x, room.y, room.width, splitPoint));
            output.Add(new RectInt(room.x, room.y + splitPoint, room.width, room.height - splitPoint));
        }
    }

    // Phase 2: Build graph nodes for rooms and doors, then spawn geometry.
    private IEnumerator BuildGraph()
    {
        yield return new WaitForSeconds(0.5f);

        doorInfos.Clear();
        dungeonGraph = new Graph();

        // Add one graph node per room at its center.
        for (int i = 0; i < rooms.Count; i++)
        {
            Vector2 roomCenter = new Vector2(
                rooms[i].x + rooms[i].width / 2f,
                rooms[i].y + rooms[i].height / 2f
            );
            dungeonGraph.AddNode(i, roomCenter);
            yield return ShowGraphStep();
        }

        // Check every room pair for a shared wall and place doors where possible.
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                yield return TryPlaceDoorBetweenRooms(i, j, rooms[i], rooms[j]);
            }
        }

        SpawnDungeonAssets();
    }

    // Detects if two rooms share a wall and places a door on the overlap if it is large enough.
    private IEnumerator TryPlaceDoorBetweenRooms(int roomIndex1, int roomIndex2, RectInt room1, RectInt room2)
    {
        // Rooms side by side: share a vertical wall and overlap on the Y axis.
        bool shareVerticalWall = (room1.xMax == room2.xMin || room1.xMin == room2.xMax) &&
            !(room1.yMax <= room2.yMin || room1.yMin >= room2.yMax);

        if (shareVerticalWall)
        {
            int yStart = Mathf.Max(room1.yMin, room2.yMin);
            int yEnd = Mathf.Min(room1.yMax, room2.yMax);

            if (yEnd - yStart > MinDoorOverlap)
            {
                int doorY = Random.Range(yStart + 1, yEnd - 1);

                int doorX;
                if (room1.xMax == room2.xMin)
                {
                    doorX = room1.xMax;
                }
                else
                {
                    doorX = room1.xMin;
                }

                yield return AddDoorToGraph(roomIndex1, roomIndex2, doorX, doorY, true);
            }

            yield break;
        }

        // Rooms stacked: share a horizontal wall and overlap on the X axis.
        bool shareHorizontalWall = (room1.yMax == room2.yMin || room1.yMin == room2.yMax) &&
            !(room1.xMax <= room2.xMin || room1.xMin >= room2.xMax);

        if (shareHorizontalWall)
        {
            int xStart = Mathf.Max(room1.xMin, room2.xMin);
            int xEnd = Mathf.Min(room1.xMax, room2.xMax);

            if (xEnd - xStart > MinDoorOverlap)
            {
                int doorX = Random.Range(xStart + 1, xEnd - 1);

                int doorY;
                if (room1.yMax == room2.yMin)
                {
                    doorY = room1.yMax;
                }
                else
                {
                    doorY = room1.yMin;
                }

                yield return AddDoorToGraph(roomIndex1, roomIndex2, doorX, doorY, false);
            }
        }
    }

    // Adds a door node to the graph and connects it to both rooms.
    private IEnumerator AddDoorToGraph(int roomIndex1, int roomIndex2, int doorX, int doorY, bool isVertical)
    {
        int doorNodeId = rooms.Count + doorInfos.Count;
        Vector2 doorPosition = new Vector2(doorX, doorY);

        dungeonGraph.AddNode(doorNodeId, doorPosition);
        yield return ShowGraphStep();

        dungeonGraph.AddEdge(roomIndex1, doorNodeId);
        dungeonGraph.AddEdge(roomIndex2, doorNodeId);
        yield return ShowGraphStep();

        doorInfos.Add(new DoorInfo
        {
            position = new Vector2Int(doorX, doorY),
            isVertical = isVertical,
            room1Index = roomIndex1,
            room2Index = roomIndex2
        });
    }

    // Updates the graph visualizer and pauses so each step can be seen.
    private IEnumerator ShowGraphStep()
    {
        if (graphVisualizer != null)
        {
            graphVisualizer.SetGraph(dungeonGraph);
        }

        yield return new WaitForSeconds(graphGenerationDelay);
    }

    // Debug drawing: red room outlines and blue door lines every frame.
    void Update()
    {
        foreach (RectInt room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room, new Color(255, 0, 0));
        }

        foreach (DoorInfo doorInfo in doorInfos)
        {
            Vector2Int door = doorInfo.position;

            if (doorInfo.isVertical)
            {
                Debug.DrawLine(
                    new Vector3(door.x, 0, door.y - doorWidth / 2),
                    new Vector3(door.x, 0, door.y + doorWidth / 2),
                    Color.blue,
                    0.1f
                );
            }
            else
            {
                Debug.DrawLine(
                    new Vector3(door.x - doorWidth / 2, 0, door.y),
                    new Vector3(door.x + doorWidth / 2, 0, door.y),
                    Color.blue,
                    0.1f
                );
            }
        }
    }

    // Rebuilds the NavMesh so the player can walk on spawned floors.
    private void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
    }

    // Phase 3: Spawn floor and wall prefabs for every room, then bake NavMesh.
    private void SpawnDungeonAssets()
    {
        GameObject wallsParent = new GameObject("Walls");
        GameObject floorsParent = new GameObject("Floors");
        wallsParent.transform.SetParent(null);
        floorsParent.transform.SetParent(null);

        float wallWidth = 0.5f;
        float wallYOffset = 0.5f;

        foreach (RectInt room in rooms)
        {
            SpawnRoomFloors(room, floorsParent.transform);
            SpawnRoomWalls(room, wallsParent.transform, wallWidth, wallYOffset);
        }

        BakeNavMesh();
    }

    // Fills a room with floor tiles on a grid.
    private void SpawnRoomFloors(RectInt room, Transform parent)
    {
        float left = room.x;
        float right = room.x + room.width;
        float bottom = room.y;
        float top = room.y + room.height;
        float offset = floorTileSize / 2f;

        for (float x = left; x < right; x += floorTileSize)
        {
            for (float z = bottom; z < top; z += floorTileSize)
            {
                SpawnFloor(new Vector3(x + offset, 0, z + offset), parent);
            }
        }
    }

    // Places wall segments along all four edges of a room.
    private void SpawnRoomWalls(RectInt room, Transform parent, float wallWidth, float wallYOffset)
    {
        float left = room.x;
        float right = room.x + room.width;
        float bottom = room.y;
        float top = room.y + room.height;

        for (float x = left; x < right; x += wallWidth)
        {
            SpawnWall(new Vector3(x, wallYOffset, top), parent);
        }

        for (float x = left; x < right; x += wallWidth)
        {
            SpawnWall(new Vector3(x, wallYOffset, bottom), parent);
        }

        for (float z = bottom; z < top; z += wallWidth)
        {
            SpawnWall(new Vector3(left, wallYOffset, z), parent);
        }

        for (float z = bottom; z < top; z += wallWidth)
        {
            SpawnWall(new Vector3(right, wallYOffset, z), parent);
        }
    }

    // Spawns a wall unless this position overlaps a door opening.
    private void SpawnWall(Vector3 position, Transform parent)
    {
        bool isDoorPosition = false;

        foreach (DoorInfo doorInfo in doorInfos)
        {
            if (doorInfo.isVertical)
            {
                if (Mathf.Abs(position.x - doorInfo.position.x) < 0.1f &&
                    Mathf.Abs(position.z - doorInfo.position.y) < doorWidth / 2f)
                {
                    isDoorPosition = true;
                    break;
                }
            }
            else
            {
                if (Mathf.Abs(position.z - doorInfo.position.y) < 0.1f &&
                    Mathf.Abs(position.x - doorInfo.position.x) < doorWidth / 2f)
                {
                    isDoorPosition = true;
                    break;
                }
            }
        }

        if (!isDoorPosition)
        {
            GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, parent);
            wall.name = "Wall";
        }
    }

    // Spawns a single floor tile, rotated flat on the ground.
    private void SpawnFloor(Vector3 position, Transform parent)
    {
        GameObject floor = Instantiate(floorPrefab, position, Quaternion.Euler(90, 0, 0), parent);
        floor.name = "Floor";
    }
}
