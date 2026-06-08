using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class HitEffect : MonoBehaviour {
    [Header("Settings")]
    public float FlashDuration = 0.1f;
    public float Intensity = 5f;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private readonly List<FlashTarget> flashTargets = new();
    private CinemachineImpulseSource impulseSource;

    private struct FlashTarget
    {
        public Renderer Renderer;
        public int MaterialIndex;
        public Color OriginalColor;
        public MaterialPropertyBlock PropertyBlock;
    }

    void Start() {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    void Awake() {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var targetRenderer in renderers) {
            if (targetRenderer is ParticleSystemRenderer) continue;

            var materials = targetRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) {
                var material = materials[i];
                if (material == null || material.HasProperty(EmissionColorId) == false) continue;

                flashTargets.Add(new FlashTarget {
                    Renderer = targetRenderer,
                    MaterialIndex = i,
                    OriginalColor = material.GetColor(EmissionColorId),
                    PropertyBlock = new MaterialPropertyBlock()
                });
            }
        }
    }

    public void OnHit() {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine() {
        impulseSource?.GenerateImpulse();

        Color flashColor = Color.white * Intensity;

        foreach (var target in flashTargets) {
            target.Renderer.GetPropertyBlock(target.PropertyBlock, target.MaterialIndex);
            target.PropertyBlock.SetColor(EmissionColorId, flashColor);
            target.Renderer.SetPropertyBlock(target.PropertyBlock, target.MaterialIndex);
        }

        yield return new WaitForSeconds(FlashDuration);

        foreach (var target in flashTargets) {
            target.Renderer.GetPropertyBlock(target.PropertyBlock, target.MaterialIndex);
            target.PropertyBlock.SetColor(EmissionColorId, target.OriginalColor);
            target.Renderer.SetPropertyBlock(target.PropertyBlock, target.MaterialIndex);
        }
    }
}
