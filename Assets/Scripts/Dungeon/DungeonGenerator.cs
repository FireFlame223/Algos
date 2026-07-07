using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.AI.Navigation;

/// <summary>
/// Procedurally generates a dungeon in three phases:
/// 1. BSP - split one big rectangle into smaller rooms until they cannot split anymore.
/// 2. Graph - rooms and doors become nodes; connections become edges. BFS/DFS verify connectivity.
/// 3. Spawn - one floor for the whole area, walls around each room (no duplicates), then bake NavMesh.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    // --- Constants ---

    // Shared wall must be at least this long before we allow a door.
    private const int MinSharedWallLength = 2;

    // Door opening is 2 grid units wide; door.position is the center.
    private const int DoorHalfWidth = 1;

    private enum SplitDirection
    {
        Vertical,   // Cut left / right
        Horizontal  // Cut bottom / top
    }

    // --- Inspector settings ---

    [Header("Dungeon size")]
    [Tooltip("Width and height of the starting rectangle before any splits.")]
    public int mainRoomSize = 30;

    [Tooltip("No room will end up narrower or shorter than this. Rooms do not have to be square.")]
    public int minRoomSize = 8;

    [Header("Debug & visuals")]
    [Tooltip("Pause between BSP split rounds so you can watch rooms appear.")]
    public bool animateRoomSplitting = true;

    [Tooltip("Pause after each room or door node is added to the graph (yellow spheres / cyan edges).")]
    public bool animateNodeCreation = true;

    [Tooltip("Pause between BSP split rounds. Not affected by Graph Generation Delay.")]
    public float splitAnimationDelay = 1f;

    [Tooltip("Pause after each graph node step.")]
    public float graphGenerationDelay = 0.5f;

    [Tooltip("Draws nodes and edges while the graph is built.")]
    public GraphVisualizer graphVisualizer;

    [Header("Prefabs & NavMesh")]
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;

    private float floorTileSize = 1f;

    // --- Runtime data ---

    private List<RectInt> rooms = new List<RectInt>();
    private List<DoorInfo> doors = new List<DoorInfo>();

    // Fast lookup: "do rooms A and B already have a door?" Stored as (smallerId, largerId).
    private HashSet<(int, int)> doorPairs = new HashSet<(int, int)>();

    private Graph dungeonGraph;

    // Tracks every wall position we have used or reserved (shared walls + door gaps).
    // Prevents spawning two wall prefabs on the same spot.
    private HashSet<Vector3Int> usedWallSlots = new HashSet<Vector3Int>();

    /// <summary>Everything we need to know about one door after it is placed.</summary>
    private class DoorInfo
    {
        public Vector2Int position;
        public bool isOnVerticalWall; // true = wall runs north-south, door opens east-west
        public int roomA;
        public int roomB;
    }

    // --- Unity lifecycle ---

    void Start()
    {
        StartCoroutine(GenerateDungeon());
    }

    /// <summary>Runs the full pipeline: split rooms, build graph, spawn 3D assets.</summary>
    private IEnumerator GenerateDungeon()
    {
        yield return SplitRoomsWithBSP();
    }

    // =========================================================================================
    // PHASE 1 - Binary Space Partitioning (BSP)
    // Keep cutting rooms in half until every room is too small to cut again.
    // =========================================================================================

    /// <summary>
    /// Starts with one big room and repeatedly splits splittable rooms.
    /// Time complexity: O(n) where n = number of final rooms (one split pass per room created).
    /// </summary>
    public IEnumerator SplitRoomsWithBSP()
    {
        rooms.Clear();
        doors.Clear();
        doorPairs.Clear();

        RectInt wholeDungeon = new RectInt(0, 0, mainRoomSize, mainRoomSize);
        List<RectInt> currentRooms = new List<RectInt> { wholeDungeon };

        // Keep splitting until one full pass finds no splittable rooms.
        bool anyRoomWasSplit = true;

        while (anyRoomWasSplit)
        {
            anyRoomWasSplit = false;
            List<RectInt> roomsAfterThisRound = new List<RectInt>();

            // One round: try to split every current room. Unsplittable rooms pass through unchanged.
            foreach (RectInt room in currentRooms)
            {
                if (CanStillSplit(room))
                {
                    SplitDirection direction = PickSplitDirection(room);
                    CutRoomInHalf(room, direction, roomsAfterThisRound);
                    anyRoomWasSplit = true;
                }
                else
                {
                    roomsAfterThisRound.Add(room);
                }
            }

            currentRooms = roomsAfterThisRound;
            rooms = new List<RectInt>(currentRooms);

            if (animateRoomSplitting)
            {
                yield return new WaitForSeconds(splitAnimationDelay);
            }
        }

        yield return BuildGraphAndSpawn();
    }

    /// <summary>True if the room is big enough to split on at least one axis.</summary>
    private bool CanStillSplit(RectInt room)
    {
        return CanSplitVertically(room) || CanSplitHorizontally(room);
    }

    /// <summary>Need room.width >= 2 * minRoomSize so both left and right children stay big enough.</summary>
    private bool CanSplitVertically(RectInt room)
    {
        return room.width >= minRoomSize * 2;
    }

    /// <summary>Need room.height >= 2 * minRoomSize so both bottom and top children stay big enough.</summary>
    private bool CanSplitHorizontally(RectInt room)
    {
        return room.height >= minRoomSize * 2;
    }

    /// <summary>Picks a random split direction, or the only valid one if the room is very thin.</summary>
    private SplitDirection PickSplitDirection(RectInt room)
    {
        bool canGoVertical = CanSplitVertically(room);
        bool canGoHorizontal = CanSplitHorizontally(room);

        if (canGoVertical && canGoHorizontal)
        {
            return Random.value > 0.5f ? SplitDirection.Vertical : SplitDirection.Horizontal;
        }

        return canGoVertical ? SplitDirection.Vertical : SplitDirection.Horizontal;
    }

    /// <summary>
    /// Picks a random split line anywhere along the room axis, as long as both sides stay >= minRoomSize.
    /// Only the edges (too-small children) are excluded - not a narrow band around the center.
    /// </summary>
    private int PickRandomSplitLine(int size)
    {
        int minSplit = minRoomSize;
        int maxSplit = size - minRoomSize;
        return Random.Range(minSplit, maxSplit + 1);
    }

    /// <summary>Cut one room into two smaller rectangles and add them to the output list.</summary>
    private void CutRoomInHalf(RectInt room, SplitDirection direction, List<RectInt> output)
    {
        if (direction == SplitDirection.Vertical)
        {
            int splitX = PickRandomSplitLine(room.width);
            splitX = ClampSplitLine(splitX, room.width);

            RectInt leftRoom = new RectInt(room.x, room.y, splitX, room.height);
            RectInt rightRoom = new RectInt(room.x + splitX, room.y, room.width - splitX, room.height);
            output.Add(leftRoom);
            output.Add(rightRoom);
        }
        else
        {
            int splitY = PickRandomSplitLine(room.height);
            splitY = ClampSplitLine(splitY, room.height);

            RectInt bottomRoom = new RectInt(room.x, room.y, room.width, splitY);
            RectInt topRoom = new RectInt(room.x, room.y + splitY, room.width, room.height - splitY);
            output.Add(bottomRoom);
            output.Add(topRoom);
        }
    }

    /// <summary>Makes sure both children after a split are at least minRoomSize wide/tall.</summary>
    private int ClampSplitLine(int splitLine, int totalSize)
    {
        if (splitLine < minRoomSize)
        {
            return minRoomSize;
        }

        if (totalSize - splitLine < minRoomSize)
        {
            return totalSize - minRoomSize;
        }

        return splitLine;
    }

    // =========================================================================================
    // PHASE 2 - Graph (rooms + doors as nodes, connections as edges)
    // =========================================================================================

    /// <summary>
    /// Builds the graph, places doors, guarantees connectivity, then spawns 3D geometry.
    /// Door detection is O(n²) where n = number of rooms (every room pair is checked).
    /// </summary>
    private IEnumerator BuildGraphAndSpawn()
    {
        doors.Clear();
        doorPairs.Clear();
        dungeonGraph = new Graph();

        // Step 1: one node per room, placed at the room center.
        for (int i = 0; i < rooms.Count; i++)
        {
            Vector2 center = GetRoomCenter(rooms[i]);
            dungeonGraph.AddNode(i, center);

            if (animateNodeCreation)
            {
                yield return PauseForNodeVisualization();
            }
        }

        // Step 2: place a door on every shared wall (BSP neighbors always share an edge).
        // That keeps the dungeon connected by design - no extra fix-up step needed.
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                if (AlreadyHasDoor(i, j))
                {
                    continue;
                }

                if (TryFindDoorSpot(rooms[i], rooms[j], out int doorX, out int doorY, out bool isOnVerticalWall))
                {
                    RegisterDoor(i, j, doorX, doorY, isOnVerticalWall);

                    if (animateNodeCreation)
                    {
                        yield return PauseForNodeVisualization();
                    }
                }
            }
        }

        VerifyAllRoomsReachable();

        RefreshGraphVisualizer();

        SpawnDungeonAssets();
    }

    private static Vector2 GetRoomCenter(RectInt room)
    {
        return new Vector2(room.x + room.width / 2f, room.y + room.height / 2f);
    }

    /// <summary>
    /// Uses BFS and DFS from room 0 to confirm every room was visited.
    /// Doors on shared walls are meant to keep the dungeon connected; this proves it.
    /// </summary>
    private void VerifyAllRoomsReachable()
    {
        HashSet<int> bfsVisited = dungeonGraph.BFS(0);
        HashSet<int> dfsVisited = dungeonGraph.DFS(0);

        for (int i = 0; i < rooms.Count; i++)
        {
            if (!bfsVisited.Contains(i))
            {
                Debug.LogError($"Connectivity check failed: BFS from room 0 did not reach room {i}.");
                return;
            }

            if (!dfsVisited.Contains(i))
            {
                Debug.LogError($"Connectivity check failed: DFS from room 0 did not reach room {i}.");
                return;
            }
        }

        Debug.Log($"Connectivity verified: all {rooms.Count} rooms are reachable from room 0 (BFS and DFS).");
    }

    /// <summary>Normalizes (roomA, roomB) so the smaller index is first - same pair either way.</summary>
    private static (int, int) DoorPairKey(int roomA, int roomB)
    {
        return roomA < roomB ? (roomA, roomB) : (roomB, roomA);
    }

    private bool AlreadyHasDoor(int roomA, int roomB)
    {
        return doorPairs.Contains(DoorPairKey(roomA, roomB));
    }

    /// <summary>
    /// Checks whether two rooms share a wall. If yes, picks a random door position anywhere along the shared section (not stuck in a corner).
    /// Returns true/false; door position is returned through out parameters (doorX, doorY, isOnVerticalWall).
    /// </summary>
    private bool TryFindDoorSpot(RectInt roomA, RectInt roomB,
        out int doorX, out int doorY, out bool isOnVerticalWall)
    {
        doorX = 0;
        doorY = 0;
        isOnVerticalWall = false;

        // CASE 1: Side-by-side rooms (vertical shared wall - same X edge, overlapping Y range).
        bool shareVerticalWall =
            (roomA.xMax == roomB.xMin || roomA.xMin == roomB.xMax) &&
            !(roomA.yMax <= roomB.yMin || roomA.yMin >= roomB.yMax);

        if (shareVerticalWall)
        {
            // Only the overlapping Y section counts - rooms may not align perfectly top/bottom.
            int overlapStart = Mathf.Max(roomA.yMin, roomB.yMin);
            int overlapEnd = Mathf.Min(roomA.yMax, roomB.yMax);

            if (overlapEnd - overlapStart > MinSharedWallLength &&
                PickRandomSpotOnWall(overlapStart, overlapEnd, out doorY))
            {
                doorX = roomA.xMax == roomB.xMin ? roomA.xMax : roomA.xMin;
                isOnVerticalWall = true;
                return true;
            }

            return false;
        }

        // CASE 2: Stacked rooms (horizontal shared wall - same Y edge, overlapping X range).
        bool shareHorizontalWall =
            (roomA.yMax == roomB.yMin || roomA.yMin == roomB.yMax) &&
            !(roomA.xMax <= roomB.xMin || roomA.xMin >= roomB.xMax);

        if (shareHorizontalWall)
        {
            // Only the overlapping X section counts - rooms may not align perfectly left/right.
            int overlapStart = Mathf.Max(roomA.xMin, roomB.xMin);
            int overlapEnd = Mathf.Min(roomA.xMax, roomB.xMax);

            if (overlapEnd - overlapStart > MinSharedWallLength &&
                PickRandomSpotOnWall(overlapStart, overlapEnd, out doorX))
            {
                doorY = roomA.yMax == roomB.yMin ? roomA.yMax : roomA.yMin;
                isOnVerticalWall = false;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Picks a random grid position along the shared wall section.
    /// Leaves DoorHalfWidth padding on each end so the door is not stuck in a corner.
    /// </summary>
    private bool PickRandomSpotOnWall(int wallStart, int wallEnd, out int position)
    {
        int minPos = wallStart + DoorHalfWidth;
        int maxPos = wallEnd - DoorHalfWidth;

        if (minPos > maxPos)
        {
            position = (wallStart + wallEnd) / 2;
            return wallEnd - wallStart > MinSharedWallLength;
        }

        position = Random.Range(minPos, maxPos + 1);
        return true;
    }

    /// <summary>
    /// Adds a door node to the graph and links it to both rooms: roomA - door - roomB.
    /// Room IDs are 0..rooms.Count-1. Door IDs start at rooms.Count so they never clash.
    /// </summary>
      private void RegisterDoor(int roomA, int roomB, int doorX, int doorY, bool isOnVerticalWall)
    {
        int doorNodeId = rooms.Count + doors.Count;
        Vector2 doorPosition = new Vector2(doorX, doorY);

        dungeonGraph.AddNode(doorNodeId, doorPosition);
        dungeonGraph.AddEdge(roomA, doorNodeId);
        dungeonGraph.AddEdge(roomB, doorNodeId);

        doors.Add(new DoorInfo
        {
            position = new Vector2Int(doorX, doorY),
            isOnVerticalWall = isOnVerticalWall,
            roomA = roomA,
            roomB = roomB
        });
        doorPairs.Add(DoorPairKey(roomA, roomB));
    }

    private void RefreshGraphVisualizer()
    {
        if (graphVisualizer != null)
        {
            graphVisualizer.SetGraph(dungeonGraph);
        }
    }

    private IEnumerator PauseForNodeVisualization()
    {
        RefreshGraphVisualizer();
        yield return new WaitForSeconds(graphGenerationDelay);
    }

    // =========================================================================================
    // Debug drawing (Game view / Scene view lines while playing)
    // =========================================================================================

    void Update()
    {
        // Red outlines = room boundaries.
        foreach (RectInt room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room, new Color(255, 0, 0));
        }

        // Blue lines = door openings.
        foreach (DoorInfo door in doors)
        {
            DrawDoorDebugLine(door);
        }
    }

    private void DrawDoorDebugLine(DoorInfo door)
    {
        Vector2Int pos = door.position;

        if (door.isOnVerticalWall)
        {
            Debug.DrawLine(
                new Vector3(pos.x, 0, pos.y - DoorHalfWidth),
                new Vector3(pos.x, 0, pos.y + DoorHalfWidth),
                Color.blue,
                0.1f
            );
        }
        else
        {
            Debug.DrawLine(
                new Vector3(pos.x - DoorHalfWidth, 0, pos.y),
                new Vector3(pos.x + DoorHalfWidth, 0, pos.y),
                Color.blue,
                0.1f
            );
        }
    }

    // =========================================================================================
    // PHASE 3 - Spawn 3D floor and walls, then bake NavMesh for the hero
    // =========================================================================================

    /// <summary>
    /// Spawns floor and walls, then bakes NavMesh.
    /// ReserveDoorSlots runs first so door gaps are never filled with walls.
    /// Time complexity: O(n) where n = number of rooms (each room has bounded edge length).
    /// </summary>
    private void SpawnDungeonAssets()
    {
        usedWallSlots.Clear();
        ReserveDoorSlots();

        GameObject wallsParent = new GameObject("Walls");
        GameObject floorsParent = new GameObject("Floors");

        SpawnEntireFloor(floorsParent.transform);

        const float wallSegmentSize = 0.5f;
        const float wallHeightOffset = 0.5f;

        foreach (RectInt room in rooms)
        {
            SpawnWallsAroundRoom(room, wallsParent.transform, wallSegmentSize, wallHeightOffset);
        }

        BakeNavMesh();
    }

    /// <summary>One continuous floor grid covering the original dungeon area (not per-room).</summary>
    private void SpawnEntireFloor(Transform parent)
    {
        float tileCenterOffset = floorTileSize / 2f;

        for (float x = 0; x < mainRoomSize; x += floorTileSize)
        {
            for (float z = 0; z < mainRoomSize; z += floorTileSize)
            {
                Vector3 tilePosition = new Vector3(x + tileCenterOffset, 0, z + tileCenterOffset);
                GameObject floor = Instantiate(floorPrefab, tilePosition, Quaternion.Euler(90, 0, 0), parent);
                floor.name = "Floor";
            }
        }
    }

    /// <summary>
    /// Pre-marks every grid slot inside a door opening in usedWallSlots.
    /// Later, TrySpawnWall sees those slots as taken and skips them
    /// Time complexity: O(n) where n = number of doors (fixed slots per door).
    /// </summary>
    private void ReserveDoorSlots()
    {
        const float wallHeightOffset = 0.5f;
        const float wallSegmentSize = 0.5f;

        foreach (DoorInfo door in doors)
        {
            if (door.isOnVerticalWall)
            {
                // Walk along the door opening on the Z axis
                for (float z = door.position.y - DoorHalfWidth; z <= door.position.y + DoorHalfWidth; z += wallSegmentSize)
                {
                    Vector3 position = new Vector3(door.position.x, wallHeightOffset, z);
                    // Strictly inside the opening (not on the exact edge)
                    if (Mathf.Abs(position.z - door.position.y) < DoorHalfWidth)
                    {
                        usedWallSlots.Add(ToWallSlot(position));
                    }
                }
            }
            else
            {
                // Walk along the door opening on the X axis.
                for (float x = door.position.x - DoorHalfWidth; x <= door.position.x + DoorHalfWidth; x += wallSegmentSize)
                {
                    Vector3 position = new Vector3(x, wallHeightOffset, door.position.y);
                    if (Mathf.Abs(position.x - door.position.x) < DoorHalfWidth)
                    {
                        usedWallSlots.Add(ToWallSlot(position));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Places wall segments every segmentSize units along all four edges of one room.
    /// Adjacent rooms share edges - usedWallSlots prevents duplicate walls on shared borders.
    /// </summary>
    private void SpawnWallsAroundRoom(RectInt room, Transform parent, float segmentSize, float yOffset)
    {
        float left = room.x;
        float right = room.x + room.width;
        float bottom = room.y;
        float top = room.y + room.height;

        for (float x = left; x < right; x += segmentSize)
        {
            TrySpawnWall(new Vector3(x, yOffset, top), parent);
            TrySpawnWall(new Vector3(x, yOffset, bottom), parent);
        }

        for (float z = bottom; z < top; z += segmentSize)
        {
            TrySpawnWall(new Vector3(left, yOffset, z), parent);
            TrySpawnWall(new Vector3(right, yOffset, z), parent);
        }
    }

    /// <summary>
    /// Converts a world position to an integer grid slot (×2 because wall segments are 0.5 units).
    /// Two positions that land on the same slot are treated as the same wall location.
    /// </summary>
    private static Vector3Int ToWallSlot(Vector3 position)
    {
        return new Vector3Int(
            Mathf.RoundToInt(position.x * 2f),
            Mathf.RoundToInt(position.y * 2f),
            Mathf.RoundToInt(position.z * 2f)
        );
    }

    /// <summary>
    /// Spawns one wall prefab at this position unless the slot is already taken.
    /// Slot may be taken by a previous room's shared wall or by ReserveDoorSlots (door gap).
    /// </summary>
    private void TrySpawnWall(Vector3 position, Transform parent)
    {
        Vector3Int slot = ToWallSlot(position);

        if (usedWallSlots.Contains(slot))
        {
            return; // shared wall or door opening - do not spawn
        }

        usedWallSlots.Add(slot);
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, parent);
        wall.name = "Wall";
    }

    /// <summary>Tells Unity's NavMesh system to recalculate walkable paths on the new floor.</summary>
    private void BakeNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
    }
}


