using UnityEngine;
using System.Collections.Generic;

public class EnemyFlashMulti : MonoBehaviour
{
    [Header("Flash Settings")]
    public Color hitColor = Color.red;     // tint color
    public float flashDuration = 0.15f;    // how long flash lasts
    public bool smoothFadeBack = true;     // if true, lerps back to original

    Renderer[] _renderers;
    List<Material> _materials = new List<Material>();
    List<Color> _originalColors = new List<Color>();

    float _flashEndTime;
    bool _isFlashing;

    void Awake()
    {
        // collect all renderers from this object + children
        _renderers = GetComponentsInChildren<Renderer>();

        foreach (var r in _renderers)
        {
            // force unique instance of each material so we don't modify shared
            var mats = r.materials;
            foreach (var m in mats)
            {
                _materials.Add(m);
                _originalColors.Add(m.color);
            }
        }
    }

    public void Flash()
    {
        _isFlashing = true;
        _flashEndTime = Time.time + flashDuration;

        // apply hitColor to all
        foreach (var m in _materials)
            m.color = hitColor;
    }

    void Update()
    {
        if (!_isFlashing) return;

        if (Time.time >= _flashEndTime)
        {
            if (smoothFadeBack)
            {
                // fade back over flashDuration
                for (int i = 0; i < _materials.Count; i++)
                {
                    _materials[i].color = Color.Lerp(
                        _materials[i].color,
                        _originalColors[i],
                        Time.deltaTime * (1f / flashDuration) * 8f // adjust speed
                    );
                }
            }
            else
            {
                // snap back immediately
                for (int i = 0; i < _materials.Count; i++)
                    _materials[i].color = _originalColors[i];
                _isFlashing = false;
            }
        }
    }
}
