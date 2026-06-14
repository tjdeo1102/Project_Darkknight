using UnityEngine;

[RequireComponent(typeof(Light))]
public sealed class AtmosphericLightPulse : MonoBehaviour
{
    [SerializeField, Min(0f)] private float baseIntensity = 1.75f;
    [SerializeField, Min(0f)] private float intensityVariation = 0.1f;
    [SerializeField, Min(0f)] private float baseRange = 7.5f;
    [SerializeField, Min(0f)] private float rangeVariation = 0.25f;
    [SerializeField, Min(0.01f)] private float pulseSpeed = 0.65f;

    private Light m_light;
    private float m_noiseSeed;

    private void Awake()
    {
        m_light = GetComponent<Light>();
        m_noiseSeed = Mathf.Abs(transform.GetInstanceID()) * 0.0137f;
        Apply(0f);
    }

    private void Update()
    {
        Apply(Time.time);
    }

    private void OnDisable()
    {
        if (m_light == null) return;
        m_light.intensity = baseIntensity;
        m_light.range = baseRange;
    }

    private void Apply(float time)
    {
        if (m_light == null) return;

        var primary = SignedNoise(time * pulseSpeed);
        var detail = SignedNoise(time * pulseSpeed * 2.17f + 13.1f);
        var pulse = primary * 0.75f + detail * 0.25f;

        m_light.intensity = Mathf.Max(0f, baseIntensity + pulse * intensityVariation);
        m_light.range = Mathf.Max(0f, baseRange + primary * rangeVariation);
    }

    private float SignedNoise(float sample)
    {
        return Mathf.PerlinNoise(m_noiseSeed, sample) * 2f - 1f;
    }
}
