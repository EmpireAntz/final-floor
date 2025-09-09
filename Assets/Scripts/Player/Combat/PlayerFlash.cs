using UnityEngine;
using System.Collections.Generic;

public class PlayerFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    private Color hitColor = new Color(2f, 0f, 0f, 1f);
    public float flashDuration = 0.15f;
    public bool smoothFadeBack = true;

    Renderer[] _renderers;
    List<Material> _mats = new List<Material>();
    List<Color> _originalColors = new List<Color>();

    float _flashEnd;
    bool _flashing;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in _renderers)
        {
            foreach (var m in r.materials) {
                _mats.Add(m);
                _originalColors.Add(m.color);
            }
        }
    }

    public void Flash()
    {
        _flashing = true;
        _flashEnd = Time.time + flashDuration;
        foreach (var m in _mats) m.color = hitColor;
    }

    void Update()
    {
        if (!_flashing) return;

        if (Time.time >= _flashEnd)
        {
            if (smoothFadeBack)
            {
                for (int i = 0; i < _mats.Count; i++)
                {
                    _mats[i].color = Color.Lerp(
                        _mats[i].color,
                        _originalColors[i],
                        Time.deltaTime * (1f / flashDuration) * 8f
                    );
                }
            }
            else
            {
                for (int i = 0; i < _mats.Count; i++)
                    _mats[i].color = _originalColors[i];
                _flashing = false;
            }
        }
    }
}
