using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public class Cell
    {
        public bool visited = false;
        // 0 = Up, 1 = Down, 2 = Right, 3 = Left
        public bool[] status = new bool[4];
    }

    [System.Serializable]
    public class Rule
    {
        public GameObject room;
        public Vector2Int minPosition;
        public Vector2Int maxPosition;
        public bool obligatory;

        public int ProbabilityOfSpawning(int x, int y)
        {
            if (x >= minPosition.x && x <= maxPosition.x && y >= minPosition.y && y <= maxPosition.y)
                return obligatory ? 2 : 1;
            return 0;
        }
    }

    public Vector2Int size;
    public int startPos = 0;
    public Rule[] rooms;
    public Vector2 offset;

    // Player spawn
    public Transform player;
    public float playerSpawnHeight = 0.1f;

    public LayerMask groundMask;

    // Extra connections
    [Range(0f, 1f)]
    public float extraDoorChance = 0.25f;  // try 0.1–0.2 if you want fewer loops

    // ---------- Boxes ----------
    [Header("Box Spawning")]
    public GameObject[] boxPrefabs;                 // assign crate/box prefabs here
    [Range(0f, 1f)] public float boxSpawnChance = 0.15f; // chance per visited room
    public Vector2Int boxesPerRoomRange = new Vector2Int(1, 2);
    public bool skipStartRoomForBoxes = true;
    public float spawnMargin = 1.0f;                // keep away from walls/doorways (meters)
    public LayerMask boxGroundMask;                 // set to your Ground layer

    // Internals
    private List<Cell> board;
    private Transform startRoomInstance;
    private readonly List<Transform> allRoomInstances = new List<Transform>();

    // NEW: keep a reference to each spawned RoomBehaviour (index = i + j * size.x)
    private RoomBehaviour[] roomRefs;

    void Start()
    {
        MazeGenerator();
    }

    void GenerateDungeon()
    {
        startRoomInstance = null;
        allRoomInstances.Clear();

        // NEW: allocate room reference array
        roomRefs = new RoomBehaviour[size.x * size.y];

        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                int idx = i + j * size.x;
                Cell currentCell = board[idx];
                if (!currentCell.visited) continue;

                int randomRoom = -1;
                List<int> availableRooms = new List<int>();

                for (int k = 0; k < rooms.Length; k++)
                {
                    int p = rooms[k].ProbabilityOfSpawning(i, j);
                    if (p == 2) { randomRoom = k; break; }
                    else if (p == 1) availableRooms.Add(k);
                }

                if (randomRoom == -1)
                    randomRoom = (availableRooms.Count > 0)
                        ? availableRooms[Random.Range(0, availableRooms.Count)]
                        : 0;

                var roomGO = Instantiate(
                    rooms[randomRoom].room,
                    new Vector3(i * offset.x, 0f, -j * offset.y), // -j on Z as before
                    Quaternion.identity,
                    transform
                );

                var roomBehaviour = roomGO.GetComponent<RoomBehaviour>();
                roomBehaviour.UpdateRoom(currentCell.status);
                roomGO.name += $" {i}-{j}";

                allRoomInstances.Add(roomGO.transform);
                if (idx == startPos) startRoomInstance = roomGO.transform;

                // NEW: remember this room for deduplication
                roomRefs[idx] = roomBehaviour;
            }
        }

        // Spawn player at the actual start room center (or SpawnPoint), then raycast to floor
        if (player != null && startRoomInstance != null)
        {
            Vector3 spawnPos;

            var marker = startRoomInstance.Find("SpawnPoint");
            if (marker != null)
            {
                spawnPos = marker.position;
            }
            else if (TryGetWorldBounds(startRoomInstance, out Bounds b))
            {
                spawnPos = b.center;
            }
            else
            {
                int sx = startPos % size.x;
                int sy = startPos / size.x;
                spawnPos = new Vector3(
                    sx * offset.x + offset.x * 0.5f,
                    2f,
                    -sy * offset.y - offset.y * 0.5f
                );
            }

            Vector3 rayStart = spawnPos + Vector3.up * 1f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 3f, groundMask, QueryTriggerInteraction.Ignore))
            {
                spawnPos = hit.point;
            }
            else
            {
                spawnPos.y = 0f;
            }

            var cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            player.position = spawnPos + Vector3.up * 0.02f;
            if (cc) cc.enabled = true;
        }

        // NEW: remove overlapping door leaves (keep one per shared doorway)
        DeduplicateSharedDoors();

        // ---------- spawn boxes after rooms & player ----------
        SpawnBoxesInRooms();
    }

    void MazeGenerator()
    {
        board = new List<Cell>();
        for (int i = 0; i < size.x; i++)
            for (int j = 0; j < size.y; j++)
                board.Add(new Cell());

        int currentCell = startPos;
        Stack<int> path = new Stack<int>();
        int k = 0;

        while (k < 1000)
        {
            k++;
            board[currentCell].visited = true;
            if (currentCell == board.Count - 1) break;

            List<int> neighbors = CheckNeighbors(currentCell);
            if (neighbors.Count == 0)
            {
                if (path.Count == 0) break;
                currentCell = path.Pop();
            }
            else
            {
                path.Push(currentCell);
                int newCell = neighbors[Random.Range(0, neighbors.Count)];

                if (newCell > currentCell)
                {
                    // down or right
                    if (newCell - 1 == currentCell)
                    {
                        board[currentCell].status[2] = true; // right
                        currentCell = newCell;
                        board[currentCell].status[3] = true; // left
                    }
                    else
                    {
                        board[currentCell].status[1] = true; // down
                        currentCell = newCell;
                        board[currentCell].status[0] = true; // up
                    }
                }
                else
                {
                    // up or left
                    if (newCell + 1 == currentCell)
                    {
                        board[currentCell].status[3] = true; // left
                        currentCell = newCell;
                        board[currentCell].status[2] = true; // right
                    }
                    else
                    {
                        board[currentCell].status[0] = true; // up
                        currentCell = newCell;
                        board[currentCell].status[1] = true; // down
                    }
                }
            }
        }

        // Add extra connections between already-visited neighbors (randomized)
        AddExtraConnections();

        GenerateDungeon();
    }

    List<int> CheckNeighbors(int cell)
    {
        List<int> neighbors = new List<int>();

        if (cell - size.x >= 0 && !board[(cell - size.x)].visited) neighbors.Add((cell - size.x));        // up
        if (cell + size.x < board.Count && !board[(cell + size.x)].visited) neighbors.Add((cell + size.x)); // down
        if ((cell + 1) % size.x != 0 && !board[(cell + 1)].visited) neighbors.Add((cell + 1));             // right
        if (cell % size.x != 0 && !board[(cell - 1)].visited) neighbors.Add((cell - 1));                   // left

        return neighbors;
    }

    // Open a bidirectional connection between two adjacent cells
    void ConnectCells(int aIdx, int bIdx)
    {
        if (aIdx < 0 || aIdx >= board.Count || bIdx < 0 || bIdx >= board.Count) return;

        int ax = aIdx % size.x;
        int ay = aIdx / size.x;
        int bx = bIdx % size.x;
        int by = bIdx / size.x;

        if (bx == ax && by == ay - 1) { board[aIdx].status[0] = true; board[bIdx].status[1] = true; } // a up
        else if (bx == ax && by == ay + 1) { board[aIdx].status[1] = true; board[bIdx].status[0] = true; } // a down
        else if (bx == ax + 1 && by == ay) { board[aIdx].status[2] = true; board[bIdx].status[3] = true; } // a right
        else if (bx == ax - 1 && by == ay) { board[aIdx].status[3] = true; board[bIdx].status[2] = true; } // a left
    }

    void AddExtraConnections()
    {
        if (extraDoorChance <= 0f) return;

        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            int idx = x + y * size.x;
            if (!board[idx].visited) continue;

            // Up neighbor (x, y-1)
            if (y - 1 >= 0)
            {
                int n = x + (y - 1) * size.x;
                if (board[n].visited && !board[idx].status[0] && Random.value < extraDoorChance)
                    ConnectCells(idx, n);
            }
            // Down neighbor (x, y+1)
            if (y + 1 < size.y)
            {
                int n = x + (y + 1) * size.x;
                if (board[n].visited && !board[idx].status[1] && Random.value < extraDoorChance)
                    ConnectCells(idx, n);
            }
            // Right neighbor (x+1, y)
            if (x + 1 < size.x)
            {
                int n = (x + 1) + y * size.x;
                if (board[n].visited && !board[idx].status[2] && Random.value < extraDoorChance)
                    ConnectCells(idx, n);
            }
            // Left neighbor (x-1, y)
            if (x - 1 >= 0)
            {
                int n = (x - 1) + y * size.x;
                if (board[n].visited && !board[idx].status[3] && Random.value < extraDoorChance)
                    ConnectCells(idx, n);
            }
        }
    }

    // ---------- BOX SPAWNING ----------
    void SpawnBoxesInRooms()
    {
        if (boxPrefabs == null || boxPrefabs.Length == 0) return;
        if (boxSpawnChance <= 0f) return;

        foreach (var room in allRoomInstances)
        {
            if (room == null) continue;
            if (skipStartRoomForBoxes && room == startRoomInstance) continue;

            if (Random.value > boxSpawnChance) continue;

            int count = Mathf.Clamp(Random.Range(boxesPerRoomRange.x, boxesPerRoomRange.y + 1), 0, 32);
            if (count <= 0) continue;

            if (!TryGetWorldBounds(room, out Bounds b))
            {
                SpawnOneBoxAt(room.position + Vector3.up * 2f);
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 p = RandomPointInBoundsXZ(b, spawnMargin);
                Vector3 rayStart = p + Vector3.up * 5f;

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, boxGroundMask, QueryTriggerInteraction.Ignore))
                    SpawnOneBoxAt(hit.point);
                else
                    SpawnOneBoxAt(p);
            }
        }
    }

    void SpawnOneBoxAt(Vector3 worldPos)
    {
        // Flat floor height (change if your floor isn't at 0)
        const float floorY = 0f;

        var prefab = boxPrefabs[Random.Range(0, boxPrefabs.Length)];
        if (!prefab) return;

        // Spawn roughly at floor (we'll correct precisely below)
        worldPos.y = floorY;

        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        var box = Instantiate(prefab, worldPos, rot, transform);

        // Adjust so the bottom of the box sits on the floor
        if (TryGetWorldBounds(box.transform, out Bounds b))
        {
            // Move so bounds.min.y == floorY
            float deltaY = floorY - b.min.y;
            box.transform.position += new Vector3(0f, deltaY + 0.01f, 0f); // +tiny lift to avoid z-fighting
        }
    }

    Vector3 RandomPointInBoundsXZ(Bounds b, float margin)
    {
        float minX = b.min.x + margin;
        float maxX = b.max.x - margin;
        float minZ = b.min.z + margin;
        float maxZ = b.max.z - margin;

        if (minX > maxX) { float m = (b.min.x + b.max.x) * 0.5f; minX = maxX = m; }
        if (minZ > maxZ) { float m = (b.min.z + b.max.z) * 0.5f; minZ = maxZ = m; }

        float x = Random.Range(minX, maxX);
        float z = Random.Range(minZ, maxZ);
        float y = b.center.y + b.extents.y; // we raycast down anyway
        return new Vector3(x, y, z);
    }

    // ---------- Helpers ----------
    bool TryGetWorldBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasAny = false;

        var rends = root.GetComponentsInChildren<Renderer>();
        foreach (var r in rends)
        {
            if (!hasAny) { bounds = r.bounds; hasAny = true; }
            else bounds.Encapsulate(r.bounds);
        }

        if (!hasAny)
        {
            var cols = root.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                if (!hasAny) { bounds = c.bounds; hasAny = true; }
                else bounds.Encapsulate(c.bounds);
            }
        }

        return hasAny;
    }

    // NEW: keep only one door leaf per shared doorway.
    // Rule: the LEFT/UP room "owns" the door; RIGHT/DOWN neighbor disables theirs.
    void DeduplicateSharedDoors()
    {
        if (roomRefs == null || board == null) return;

        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            int idx = x + y * size.x;
            if (!board[idx].visited) continue;

            var room = roomRefs[idx];
            if (room == null) continue;

            // status indices: 0 Up, 1 Down, 2 Right, 3 Left

            // If connected RIGHT, disable the LEFT door on the right neighbor.
            if (board[idx].status[2] && x + 1 < size.x)
            {
                int rightIdx = (x + 1) + y * size.x;
                if (board[rightIdx].visited && roomRefs[rightIdx] != null)
                {
                    room.SetDoorEnabled(2, true);               // ensure THIS room keeps its Right leaf
                    roomRefs[rightIdx].SetDoorEnabled(3, false); // neighbor loses its Left leaf
                }
            }

            // If connected DOWN, disable the UP door on the below neighbor.
            if (board[idx].status[1] && y + 1 < size.y)
            {
                int downIdx = x + (y + 1) * size.x;
                if (board[downIdx].visited && roomRefs[downIdx] != null)
                {
                    room.SetDoorEnabled(1, true);               // ensure THIS room keeps its Down leaf
                    roomRefs[downIdx].SetDoorEnabled(0, false);  // neighbor loses its Up leaf
                }
            }
        }
    }
}
