using System.Collections.Generic;
using UnityEngine;

public class RoomBehaviour : MonoBehaviour
{
    [Header("Room Sides (order: 0 Up, 1 Down, 2 Right, 3 Left)")]
    public GameObject[] walls = new GameObject[4];
    public GameObject[] doors = new GameObject[4];

    [Header("Torch Spawning (socket-based)")]
    public GameObject torchPrefab;                  // your LitTorch prefab
    public bool spawnBothSides = true;              // spawn L & R, else prefer Right then Left
    public Transform[] leftTorchSockets  = new Transform[4]; // Up,Down,Right,Left
    public Transform[] rightTorchSockets = new Transform[4]; // Up,Down,Right,Left

    [Header("Debug (optional editor preview)")]
    public bool[] testStatus = new bool[4];         // set 4 bools in prefab if you want to preview

    private readonly List<GameObject> spawnedTorches = new List<GameObject>();

    void Start()
    {
        // Optional: lets you preview in the editor if you fill testStatus (size must be 4)
        if (testStatus != null && testStatus.Length == 4)
            UpdateRoom(testStatus);
    }

    public void UpdateRoom(bool[] status)
    {
        if (status == null || status.Length < 4)
        {
            Debug.LogWarning($"{name}: UpdateRoom called with invalid status array.");
            return;
        }

        // Toggle doors/walls
        for (int i = 0; i < 4; i++)
        {
            if (i < doors.Length && doors[i] != null)
                doors[i].SetActive(status[i]);

            if (i < walls.Length && walls[i] != null)
                walls[i].SetActive(!status[i]);
        }

        // Remove previously spawned torches
        for (int i = 0; i < spawnedTorches.Count; i++)
            if (spawnedTorches[i] != null) Destroy(spawnedTorches[i]);
        spawnedTorches.Clear();

        if (torchPrefab == null) return;

        // Spawn torches at sockets for each open doorway
        for (int i = 0; i < 4; i++)
        {
            if (!status[i]) continue; // no door on this side

            Transform L = (leftTorchSockets  != null && i < leftTorchSockets.Length)  ? leftTorchSockets[i]  : null;
            Transform R = (rightTorchSockets != null && i < rightTorchSockets.Length) ? rightTorchSockets[i] : null;

            if (spawnBothSides)
            {
                if (L != null) spawnedTorches.Add(Instantiate(torchPrefab, L.position, L.rotation, transform));
                if (R != null) spawnedTorches.Add(Instantiate(torchPrefab, R.position, R.rotation, transform));
            }
            else
            {
                // Prefer Right, else Left
                Transform S = (R != null) ? R : L;
                if (S != null) spawnedTorches.Add(Instantiate(torchPrefab, S.position, S.rotation, transform));
            }
        }
    }
}
