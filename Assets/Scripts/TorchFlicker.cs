using UnityEngine;

/// <summary>화톳불 조명이 살아 있는 것처럼 흔들리게 한다.</summary>
[RequireComponent(typeof(Light))]
public class TorchFlicker : MonoBehaviour
{
    [SerializeField] float baseIntensity = 3.2f;
    [SerializeField] float intensityJitter = 0.7f;
    [SerializeField] float positionJitter = 0.04f;
    [SerializeField] float speed = 5.5f;

    Light _light;
    Vector3 _basePosition;
    float _seed;

    void Awake()
    {
        _light = GetComponent<Light>();
        _basePosition = transform.localPosition;
        _seed = Random.value * 100f;
        baseIntensity = _light.intensity;
    }

    void Update()
    {
        float t = Time.time * speed + _seed;
        float n = Mathf.PerlinNoise(t, 0f) * 2f - 1f;

        _light.intensity = baseIntensity + n * intensityJitter;
        _light.transform.localPosition = _basePosition + new Vector3(
            (Mathf.PerlinNoise(t, 11f) - 0.5f) * positionJitter,
            (Mathf.PerlinNoise(t, 23f) - 0.5f) * positionJitter,
            (Mathf.PerlinNoise(t, 37f) - 0.5f) * positionJitter);
    }
}
