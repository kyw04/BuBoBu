using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// 로컬 전용 파티클/사운드 풀. 처음 사용할 때 자동 생성되어 씬 전환 후에도 유지
public class EffectPool : MonoBehaviour
{
    private static EffectPool instance;

    public static EffectPool Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject(nameof(EffectPool));
                DontDestroyOnLoad(go);
                instance = go.AddComponent<EffectPool>();
            }
            return instance;
        }
    }

    private readonly Dictionary<ParticleSystem, ObjectPool<ParticleSystem>> particlePools = new();
    private ObjectPool<AudioSource> soundPool;

    private void Awake()
    {
        soundPool = new ObjectPool<AudioSource>(
            createFunc: () =>
            {
                var source = new GameObject("Sound").AddComponent<AudioSource>();
                source.transform.SetParent(transform);
                source.playOnAwake = false;
                return source;
            },
            actionOnGet: s => s.gameObject.SetActive(true),
            actionOnRelease: s => s.gameObject.SetActive(false),
            actionOnDestroy: s => Destroy(s.gameObject));
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void PlayParticle(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        if (!particlePools.TryGetValue(prefab, out var pool))
            particlePools[prefab] = pool = CreateParticlePool(prefab);

        ParticleSystem particle = pool.Get();
        particle.transform.SetPositionAndRotation(position, rotation);
        particle.Play(true);
    }

    public void PlaySound(AudioClip clip, Vector3 position)
    {
        AudioSource source = soundPool.Get();
        source.transform.position = position;
        source.clip = clip;
        source.Play();
        StartCoroutine(ReleaseSoundAfter(source, clip.length));
    }

    private System.Collections.IEnumerator ReleaseSoundAfter(AudioSource source, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        soundPool.Release(source);
    }

    private ObjectPool<ParticleSystem> CreateParticlePool(ParticleSystem prefab)
    {
        ObjectPool<ParticleSystem> pool = null;
        pool = new ObjectPool<ParticleSystem>(
            createFunc: () =>
            {
                ParticleSystem particle = Instantiate(prefab, transform);
                var main = particle.main;
                main.stopAction = ParticleSystemStopAction.Callback; // 재생이 끝나면 PooledParticle이 풀로 반환
                particle.gameObject.AddComponent<PooledParticle>().Init(pool);
                return particle;
            },
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => Destroy(p.gameObject));
        return pool;
    }

    private class PooledParticle : MonoBehaviour
    {
        private ObjectPool<ParticleSystem> pool;
        private ParticleSystem particle;

        public void Init(ObjectPool<ParticleSystem> owner)
        {
            pool = owner;
            particle = GetComponent<ParticleSystem>();
        }

        private void OnParticleSystemStopped() => pool.Release(particle);
    }
}
