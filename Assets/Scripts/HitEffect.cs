using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class HitEffect : MonoBehaviour {
    private CinemachineImpulseSource impulseSource;

    void Start() {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void OnHit() {
        impulseSource?.GenerateImpulse();
    }
}
