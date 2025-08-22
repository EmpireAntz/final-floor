using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light lanternLight;
    public float minIntensity = 1f;
    public float maxIntensity = 2f;
    public float flickerSpeed = 0.1f;

    void Update()
    {
        if (lanternLight != null)
        {
            lanternLight.intensity = Mathf.Lerp(
                lanternLight.intensity,
                Random.Range(minIntensity, maxIntensity),
                flickerSpeed
            );
        }
    }
}
