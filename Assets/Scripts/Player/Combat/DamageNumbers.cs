using UnityEngine;

public class DamageNumbers : MonoBehaviour
{
    public static DamageNumbers Instance { get; private set; }
    public DamagePopup popupPrefab;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void Show(Vector3 worldPos, int amount, bool isCrit)
    {
        if (!Instance || !Instance.popupPrefab) return;

        var p = Instantiate(Instance.popupPrefab);
        // hard reset to world space
        p.transform.SetParent(null, true);
        p.transform.position   = worldPos;
        p.transform.rotation   = Quaternion.identity;
        p.transform.localScale = Vector3.one;

        // pass anchor position explicitly
        p.Init(amount, isCrit, worldPos);
    }
}
