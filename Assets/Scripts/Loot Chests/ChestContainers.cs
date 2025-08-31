using System.Collections.Generic;
using UnityEngine;

public class ChestContainer : MonoBehaviour
{
    [Range(1,4)] public int capacity = 4; // 2x2 grid max
    public List<SimpleItem> items = new List<SimpleItem>();
    public System.Action OnChanged;
}
