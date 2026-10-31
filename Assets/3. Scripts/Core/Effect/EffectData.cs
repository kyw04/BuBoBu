using System;
using UnityEngine;

// 소리 + 파티클 한 묶음. 인스펙터에서 비워 둔 항목은 재생하지 않음
[Serializable]
public struct EffectData
{
    public AudioClip sound;
    public ParticleSystem particle; // 프리팹

    public void Play(Vector3 position) => Play(position, Quaternion.identity);

    public void Play(Vector3 position, Quaternion rotation)
    {
        if (Application.isBatchMode) return; // 데디케이티드 서버는 연출 불필요

        if (sound != null)
            EffectPool.Instance.PlaySound(sound, position);
        if (particle != null)
            EffectPool.Instance.PlayParticle(particle, position, rotation);
    }
}
