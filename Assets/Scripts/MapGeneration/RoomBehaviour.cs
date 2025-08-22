using System.Collections.Generic;
using UnityEngine;

public class RoomBehaviour : MonoBehaviour
{
    [Header("Room Sides (order: 0 Up, 1 Down, 2 Right, 3 Left)")]
    public GameObject[] walls = new GameObject[4];
    public GameObject[] doors = new GameObject[4];
    public GameObject[] builtInDoors = new GameObject[4];

    [Header("Torch Spawning (socket-based)")]
    public GameObject torchPrefab;                  //LitTorch prefab
    public bool spawnBothSides = true;              // spawn L & R, else prefer Right then Left
    public Transform[] leftTorchSockets  = new Transform[4]; // Up,Down,Right,Left
    public Transform[] rightTorchSockets = new Transform[4]; // Up,Down,Right,Left

    [Header("Debug (optional editor preview)")]
    public bool[] testStatus = new bool[4]; // set 4 bools in prefab to preview

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

        // ===== Per-side geometry =====
        // walls[]        => solid wall (no hole)
        // doors[]        => entryway/frame (hole in wall you placed)
        // builtInDoors[] => actual door leaf you placed
        for (int i = 0; i < 4; i++)
        {
            bool open = status[i]; // true => doorway on this side

            if (i < walls.Length && walls[i] != null)
                walls[i].SetActive(!open);   // solid wall ON when closed

            if (i < doors.Length && doors[i] != null)
                doors[i].SetActive(open);    // entryway/frame ON when open

            if (builtInDoors != null && i < builtInDoors.Length && builtInDoors[i] != null)
                builtInDoors[i].SetActive(open); // door leaf ON when open (for now)
        }

        // ===== Torch spawning =====
        for (int i = 0; i < spawnedTorches.Count; i++)
            if (spawnedTorches[i] != null) Destroy(spawnedTorches[i]);
        spawnedTorches.Clear();

        if (torchPrefab == null) return;

        for (int i = 0; i < 4; i++)
        {
            if (!status[i]) continue; // only spawn torches at open sides

            Transform L = (leftTorchSockets != null && i < leftTorchSockets.Length)  ? leftTorchSockets[i]  : null;
            Transform R = (rightTorchSockets != null && i < rightTorchSockets.Length) ? rightTorchSockets[i] : null;

            if (spawnBothSides)
            {
                if (L != null) spawnedTorches.Add(Instantiate(torchPrefab, L.position, L.rotation, transform));
                if (R != null) spawnedTorches.Add(Instantiate(torchPrefab, R.position, R.rotation, transform));
            }
            else
            {
                Transform S = (R != null) ? R : L; // prefer Right
                if (S != null) spawnedTorches.Add(Instantiate(torchPrefab, S.position, S.rotation, transform));
            }
        }
    }

   // Lets the generator enable/disable a single side's door leaf to avoid overlaps
    public void SetDoorEnabled(int sideIndex, bool enabled)
    {
        if (builtInDoors != null &&
            sideIndex >= 0 && sideIndex < builtInDoors.Length &&
            builtInDoors[sideIndex] != null)
        {
            builtInDoors[sideIndex].SetActive(enabled);
        }
    }
}
