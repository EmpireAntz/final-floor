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

    [Header("Layout")]
    public Vector2Int size = new Vector2Int(5, 5);
    public int startPos = 0;
    public Rule[] rooms;
    public Vector2 offset = new Vector2(10f, 10f);

    [Header("Player")]
    public Transform player;
    public float playerSpawnHeight = 0.1f;
    public LayerMask groundMask;

    [Header("Extra Connections")]
    [Range(0f, 1f)]
    public float extraDoorChance = 0.25f;

    [Header("Boxes")]
    public GameObject[] boxPrefabs;
    [Range(0f, 1f)] public float boxSpawnChance = 0.15f;
    public Vector2Int boxesPerRoomRange = new Vector2Int(1, 2);
    public bool skipStartRoomForBoxes = true;
    public float spawnMargin = 1.0f;
    public LayerMask boxGroundMask;

    [Header("Starting Room Props")]
    public GameObject tablePrefab;
    public GameObject swordPrefab;
    public LayerMask propGroundMask; // set to Ground

    [Header("Sword Placement (on table)")]
    public float swordSurfaceLift = 0.01f;   // tiny lift to avoid z-fighting
    public float swordYawOnTable = 30f;      // rotate around table up (degrees)
    public Vector2 swordEdgeOffset = new Vector2(0.2f, 0.05f); // (right, forward) meters
    public bool useSwordSocketIfPresent = true; // if the table has a "SwordSocket"


    // Internals
    private List<Cell> board;
    private Transform startRoomInstance;
    private readonly List<Transform> allRoomInstances = new List<Transform>();
    private RoomBehaviour[] roomRefs;

    void Start()
    {
        MazeGenerator();
    }

    // -------- Core Generation --------

    void MazeGenerator()
    {
        board = new List<Cell>();
        for (int i = 0; i < size.x; i++)
            for (int j = 0; j < size.y; j++)
                board.Add(new Cell());

        int currentCell = startPos;
        Stack<int> path = new Stack<int>();
        int guard = 0;

        while (guard++ < 5000)
        {
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

        AddExtraConnections();
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        startRoomInstance = null;
        allRoomInstances.Clear();
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
                    new Vector3(i * offset.x, 0f, -j * offset.y),
                    Quaternion.identity,
                    transform
                );

                var roomBehaviour = roomGO.GetComponent<RoomBehaviour>();
                if (!roomBehaviour)
                    Debug.LogWarning($"{roomGO.name} has no RoomBehaviour component.");
                else
                    roomBehaviour.UpdateRoom(currentCell.status);

                roomGO.name += $" {i}-{j}";
                allRoomInstances.Add(roomGO.transform);

                if (idx == startPos) startRoomInstance = roomGO.transform;
                roomRefs[idx] = roomBehaviour;
            }
        }

        // Spawn & orient player
        if (player && startRoomInstance)
        {
            Vector3 spawnPos = FindStartSpawnPosition();
            Vector3 faceDir  = GetFacingTowardOpenDoor(spawnPos);
            Quaternion lookRot = Quaternion.LookRotation(faceDir, Vector3.up);

            var cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            player.SetPositionAndRotation(spawnPos + Vector3.up * 0.02f, lookRot);
            if (cc) cc.enabled = true;

            var look = FindObjectOfType<MouseMovement>();
            if (look) look.SyncToTransforms();
        }

        // Door dedupe, then content
        DeduplicateSharedDoors();
        SpawnBoxesInRooms();

        // NEW: Spawn start-room props (table + sword)
        SpawnStartRoomProps();
    }

    // -------- Neighbors / Connections --------

    List<int> CheckNeighbors(int cell)
    {
        List<int> neighbors = new List<int>();

        if (cell - size.x >= 0 && !board[(cell - size.x)].visited) neighbors.Add((cell - size.x));        // up
        if (cell + size.x < board.Count && !board[(cell + size.x)].visited) neighbors.Add((cell + size.x)); // down
        if ((cell + 1) % size.x != 0 && !board[(cell + 1)].visited) neighbors.Add((cell + 1));             // right
        if (cell % size.x != 0 && !board[(cell - 1)].visited) neighbors.Add((cell - 1));                   // left

        return neighbors;
    }

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

            if (y - 1 >= 0)
            {
                int n = x + (y - 1) * size.x;
                if (board[n].visited && !board[idx].status[0] && Random.value < extraDoorChance)
                    ConnectCells(idx, n);
            }
            if (y + 1 < size.y)
            {
                int n = x + (y + 1) * size.x;
                if (board[n].visited && !board[idx].status[1] && Random.value < extraDoorChance)
                    ConnectCells(idx, n);
            }
            if (x + 1 < size.x)
            {
                int n = (x + 1) + y * size.x;
                if (board[n].visited && !board[idx].status[2] && Random.value < extraDoorChance)
                    ConnectCells(idx, n);
            }
            if (x - 1 >= 0)
            {
                int n = (x - 1) + y * size.x;
                if (board[n].visited && !board[idx].status[3] && Random.value < extraDoorChance)
                    ConnectCells(idx, n);
            }
        }
    }

    // -------- Boxes --------

    void SpawnBoxesInRooms()
    {
        if (boxPrefabs == null || boxPrefabs.Length == 0) return;
        if (boxSpawnChance <= 0f) return;

        foreach (var room in allRoomInstances)
        {
            if (!room) continue;
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
        const float floorY = 0f;
        var prefab = boxPrefabs[Random.Range(0, boxPrefabs.Length)];
        if (!prefab) return;

        worldPos.y = floorY;
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        var box = Instantiate(prefab, worldPos, rot, transform);

        if (TryGetWorldBounds(box.transform, out Bounds b))
        {
            float deltaY = floorY - b.min.y;
            box.transform.position += new Vector3(0f, deltaY + 0.01f, 0f);
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
        float y = b.center.y + b.extents.y;
        return new Vector3(x, y, z);
    }

    // -------- Start Room Props (table + sword) --------

    void SpawnStartRoomProps()
    {
        if (!startRoomInstance)
        {
            Debug.LogWarning("[StartProps] No startRoomInstance. Skipping table/sword.");
            return;
        }
        if (!tablePrefab)
        {
            Debug.LogWarning("[StartProps] tablePrefab is NULL. Assign it in the Inspector.");
            return;
        }

        // 1) Find socket inside START ROOM
        Transform socket = startRoomInstance.Find("StartTableSocket");
        if (!socket)
            Debug.LogWarning("[StartProps] No 'StartTableSocket' found under start room; using room center.");

        // 2) Base position (socket -> bounds center -> transform)
        Vector3 basePos = socket ? socket.position :
                         (TryGetWorldBounds(startRoomInstance, out Bounds roomB) ? roomB.center : startRoomInstance.position);

        // 3) Grounding via raycast
        Vector3 rayStart = basePos + Vector3.up * 5f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, propGroundMask, QueryTriggerInteraction.Ignore))
            basePos = hit.point;

        // 4) Spawn table
        Quaternion tableRot = socket ? socket.rotation : Quaternion.identity;
        GameObject table = Instantiate(tablePrefab, basePos, tableRot, startRoomInstance);
        table.name = "[StartRoom] Table";

        // 4a) Lift table to sit on floor
        if (TryGetWorldBounds(table.transform, out Bounds tb))
        {
            float lift = basePos.y - tb.min.y + 0.01f;
            table.transform.position += new Vector3(0f, lift, 0f);
        }

        // 5) Spawn sword (prefer table's SwordSocket)
        if (swordPrefab)
        {
            // Prefer a socket on the table if present
            Transform swordSocket = useSwordSocketIfPresent ? table.transform.Find("SwordSocket") : null;

            Vector3 swordPos;
            Quaternion swordRot;

            if (swordSocket)
            {
                // Exact authoring via socket
                swordPos = swordSocket.position;
                swordRot = swordSocket.rotation;
            }
            else
            {
                // Compute a clean placement from table bounds
                // Start at the top surface, then slide toward an edge
                Vector3 tableUp = table.transform.up;
                Vector3 tableRight = table.transform.right;
                Vector3 tableForward = table.transform.forward;

                // Top center of the table (world)
                Vector3 topCenter = new Vector3(tb.center.x, tb.max.y, tb.center.z);

                // Slide along surface toward an edge for better look
                Vector3 slide = tableRight * swordEdgeOffset.x + tableForward * swordEdgeOffset.y;

                swordPos = topCenter + slide + tableUp * swordSurfaceLift;

                // Align sword's up to the table's up, then yaw around table up
                // We’ll instantiate identity first, then set rotation
                swordRot = Quaternion.identity;
            }

            GameObject sword = Instantiate(swordPrefab, swordPos, swordRot, table.transform);

            // If we didn't use a dedicated socket, do the alignment now
            if (!swordSocket)
            {
                Transform s = sword.transform;
                Vector3 tableUp = table.transform.up;

                // Align sword's up-vector to table's up-vector
                Quaternion alignUp = Quaternion.FromToRotation(s.up, tableUp) * s.rotation;
                s.rotation = alignUp;

                // Optional: yaw around table up to get the angle you like (e.g., 30°)
                s.Rotate(tableUp, swordYawOnTable, Space.World);
            }

            // Optional safety: keep it from sliding if it has physics
            var rb = sword.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
        }
    }

    // -------- Helpers --------

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

    void DeduplicateSharedDoors()
    {
        if (roomRefs == null || board == null) return;

        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            int idx = x + y * size.x;
            if (!board[idx].visited) continue;

            var room = roomRefs[idx];
            if (!room) continue;

            // Right neighbor: keep this Right leaf, disable neighbor's Left
            if (board[idx].status[2] && x + 1 < size.x)
            {
                int rightIdx = (x + 1) + y * size.x;
                if (board[rightIdx].visited && roomRefs[rightIdx])
                {
                    room.SetDoorEnabled(2, true);
                    roomRefs[rightIdx].SetDoorEnabled(3, false);
                }
            }
            // Down neighbor: keep this Down leaf, disable neighbor's Up
            if (board[idx].status[1] && y + 1 < size.y)
            {
                int downIdx = x + (y + 1) * size.x;
                if (board[downIdx].visited && roomRefs[downIdx])
                {
                    room.SetDoorEnabled(1, true);
                    roomRefs[downIdx].SetDoorEnabled(0, false);
                }
            }
        }
    }

    Vector3 CellCenterWorld(int idx)
    {
        int x = idx % size.x;
        int y = idx / size.x;
        return new Vector3(
            x * offset.x + 0.5f * offset.x,
            0f,
            -y * offset.y - 0.5f * offset.y
        );
    }

    Vector3 GetFacingTowardOpenDoor(Vector3 fromPos)
    {
        Vector3 center = CellCenterWorld(startPos);
        bool[] s = board[startPos].status;

        var targets = new List<Vector3>(4);
        if (s[0]) targets.Add(center + new Vector3(0f, 0f,  0.5f * offset.y)); // Up => +Z
        if (s[1]) targets.Add(center + new Vector3(0f, 0f, -0.5f * offset.y)); // Down => -Z
        if (s[2]) targets.Add(center + new Vector3( 0.5f * offset.x, 0f, 0f)); // Right => +X
        if (s[3]) targets.Add(center + new Vector3(-0.5f * offset.x, 0f, 0f)); // Left => -X

        float best = float.PositiveInfinity;
        Vector3 bestDir = Vector3.forward;

        foreach (var t in targets)
        {
            Vector3 dir = t - fromPos;
            dir.y = 0f;
            float d2 = dir.sqrMagnitude;
            if (d2 > 0.0001f && d2 < best)
            {
                best = d2;
                bestDir = dir.normalized;
            }
        }

        return bestDir;
    }

    Vector3 FindStartSpawnPosition()
    {
        Vector3 spawnPos;

        var marker = startRoomInstance.Find("SpawnPoint");
        if (marker)
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
            spawnPos = hit.point;
        else
            spawnPos.y = playerSpawnHeight;

        return spawnPos;
    }

    // -------- Visual Gizmos (optional) --------
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (startRoomInstance)
        {
            var socket = startRoomInstance.Find("StartTableSocket");
            if (socket)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(socket.position, 0.25f);
                Gizmos.DrawLine(socket.position, socket.position + Vector3.up * 0.6f);
            }
        }
    }
#endif
}
