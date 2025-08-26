using System.Collections.Generic;
using UnityEngine;

public class EquipmentBinder : MonoBehaviour
{
    [Header("Refs")]
    public Inventory inventory;

    [Tooltip("Root of your armature (parent of Hips). If empty, auto-detected from Animator.")]
    public Transform skeletonRoot;

    [Tooltip("Right-hand bone (e.g., mixamorig:RightHand). If empty, auto from Animator.")]
    public Transform rightHandBone;

    [Tooltip("Left-hand bone (optional). If empty, auto from Animator.")]
    public Transform leftHandBone;

    [Header("Debug")]
    public bool debugLogging = true;

    // Spawned instances per slot
    private readonly Dictionary<EquipSlot, GameObject> spawned = new();
    private readonly Dictionary<EquipSlot, ItemData>   current  = new();

    // Bone lookup by name on the player
    private Dictionary<string, Transform> boneMap;

    // Animator (for auto bone wiring + static accessory targets)
    private Animator anim;

    void Awake()
    {
        if (!inventory) inventory = FindObjectOfType<Inventory>();
        anim = GetComponentInChildren<Animator>();

        AutoWireBones();
        BuildBoneMap();
    }

    void OnEnable()
    {
        if (inventory) inventory.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (inventory) inventory.OnChanged -= Refresh;
        ClearAll();
    }

    void AutoWireBones()
    {
        if (anim == null) return;

        // skeletonRoot = parent of hips (or hips root)
        if (!skeletonRoot)
        {
            var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (hips) skeletonRoot = hips.root;
        }

        // hands
        if (!rightHandBone)
        {
            rightHandBone = anim.GetBoneTransform(HumanBodyBones.RightHand);
        }
        if (!leftHandBone)
        {
            leftHandBone = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        }

    }

    void BuildBoneMap()
    {
        boneMap = new Dictionary<string, Transform>(512);
        if (!skeletonRoot) return;
        foreach (var t in skeletonRoot.GetComponentsInChildren<Transform>(true))
            if (!boneMap.ContainsKey(t.name)) boneMap.Add(t.name, t);
    }

    public void Refresh()
    {
        // Hands (weapons)
        BindHand(EquipSlot.HandRight, rightHandBone);
        BindHand(EquipSlot.HandLeft,  leftHandBone);

        // Armor (skinned or static)
        BindWearable(EquipSlot.Head);
        BindWearable(EquipSlot.Chest);
        BindWearable(EquipSlot.Legs);
        BindWearable(EquipSlot.Feet);
        BindWearable(EquipSlot.Back);
        // Add EquipSlot.Arms here if you added one:
        // BindWearable(EquipSlot.Arms);
    }

    // ====== HANDS ======
    void BindHand(EquipSlot slot, Transform handBone)
    {
        var item = FindEquipped(slot);
        if (current.TryGetValue(slot, out var cur) && cur == item) return;

        DestroyIfExists(slot);
        current[slot] = item;

        if (item == null || handBone == null || item.heldPrefab == null)
        {
            return;
        }

        var inst = Instantiate(item.heldPrefab, handBone, false);
        inst.transform.localPosition    = item.localPosition;
        inst.transform.localEulerAngles = item.localEulerAngles;
        inst.transform.localScale       = item.localScale;

        var rb = inst.GetComponent<Rigidbody>(); if (rb) rb.isKinematic = true;
        foreach (var c in inst.GetComponentsInChildren<Collider>(true)) c.enabled = false;

        spawned[slot] = inst;
    }

    // ====== ARMOR / WEARABLES ======
    void BindWearable(EquipSlot slot)
    {
        var item = FindEquipped(slot);
        if (current.TryGetValue(slot, out var cur) && cur == item) return;

        DestroyIfExists(slot);
        current[slot] = item;

        if (item == null)
        {
            return;
        }

        if (!item.skinnedPrefab)
        {
            // Fallback: allow static accessory prefabs parented to a relevant bone
            var targetBone = GetBoneForSlot(slot);
            if (!targetBone)
            {
                return;
            }

            var staticInst = InstantiateFallbackStatic(item.skinnedPrefab, targetBone);
            spawned[slot] = staticInst;
            return;
        }

        // Normal path: skinned prefab → retarget to our skeleton
        var inst = Instantiate(item.skinnedPrefab, transform, false);
        int smrs = RetargetSkinnedToSkeleton(inst);
        spawned[slot] = inst;
    }

    // Returns count of SMRs processed
    int RetargetSkinnedToSkeleton(GameObject instRoot)
    {
        if (boneMap == null || boneMap.Count == 0) BuildBoneMap();
        int count = 0;

        var smrs = instRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in smrs)
        {
            if (!smr) continue;

            // bones
            var srcBones = smr.bones;
            var dstBones = new Transform[srcBones.Length];
            int matched = 0;
            for (int i = 0; i < srcBones.Length; i++)
            {
                var b = srcBones[i];
                if (b && boneMap.TryGetValue(b.name, out var mapped))
                {
                    dstBones[i] = mapped; matched++;
                }
                else
                {
                    dstBones[i] = FindBoneFallback(b ? b.name : null);
                }
            }
            smr.bones = dstBones;

            // root bone
            Transform newRoot = null;
            if (smr.rootBone && boneMap.TryGetValue(smr.rootBone.name, out var mappedRoot))
                newRoot = mappedRoot;
            if (!newRoot) newRoot = skeletonRoot;
            smr.rootBone = newRoot;


            smr.updateWhenOffscreen = false;
            count++;
        }
        return count;
    }

    Transform FindBoneFallback(string name)
    {
        if (string.IsNullOrEmpty(name) || skeletonRoot == null) return skeletonRoot;
        // very slow path; only used when not in map
        foreach (var t in skeletonRoot.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return skeletonRoot;
    }

    GameObject InstantiateFallbackStatic(GameObject prefab, Transform parentBone)
    {
        // If your "skinnedPrefab" accidentally has no SkinnedMeshRenderer, we still show it
        var inst = Instantiate(prefab, parentBone, false);
        foreach (var c in inst.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        var rb = inst.GetComponent<Rigidbody>(); if (rb) rb.isKinematic = true;
        return inst;
    }

    Transform GetBoneForSlot(EquipSlot slot)
    {
        if (anim == null) return null;

        // Use Animator human bones to pick a sensible anchor for STATIC accessories
        switch (slot)
        {
            case EquipSlot.Head:
                return anim.GetBoneTransform(HumanBodyBones.Head);
            case EquipSlot.Chest:
                // Prefer UpperChest if present, else Chest, else Spine
                return anim.GetBoneTransform(HumanBodyBones.UpperChest)
                    ?? anim.GetBoneTransform(HumanBodyBones.Chest)
                    ?? anim.GetBoneTransform(HumanBodyBones.Spine);
            case EquipSlot.Back:
                return anim.GetBoneTransform(HumanBodyBones.Spine);
            case EquipSlot.Legs:
                return anim.GetBoneTransform(HumanBodyBones.Hips);
            case EquipSlot.Feet:
                // Static “boots” anchored to hips looks odd; skinned prefabs are recommended for Feet
                return anim.GetBoneTransform(HumanBodyBones.Hips);
            default:
                return null;
        }
    }

    SimpleItem FindEquippedItem(EquipSlot slot)
    {
        for (int i = 0; i < inventory.equipment.Count; i++)
        {
            var it = inventory.equipment[i];
            if (!Inventory.IsEmpty(it) && it.data.equipSlot == slot) return it;
        }
        return null;
    }

    ItemData FindEquipped(EquipSlot slot) => FindEquippedItem(slot)?.data;

    void DestroyIfExists(EquipSlot slot)
    {
        if (spawned.TryGetValue(slot, out var go) && go)
            Destroy(go);
        spawned[slot] = null;
    }

    void ClearAll()
    {
        foreach (var kv in spawned)
            if (kv.Value) Destroy(kv.Value);
        spawned.Clear();
        current.Clear();
    }

    static string NameOf(Object o) => o ? o.name : "(null)";
}
