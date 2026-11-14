using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 인스펙터에서 인터페이스 타입을 할당할 수 있게 해주는 래퍼.
/// 사용 예: [SerializeField] private InterfaceReference&lt;ITintable&gt;[] tintTargets;
/// T를 구현한 컴포넌트(또는 ScriptableObject)만 할당되며,
/// GameObject를 드래그하면 T를 구현한 첫 번째 컴포넌트를 자동으로 찾아 넣는다.
/// </summary>
[Serializable]
public class InterfaceReference<T> where T : class
{
    [SerializeField] private Object target;

    public InterfaceReference() { }

    public InterfaceReference(T value) => Value = value;

    /// <summary>할당된 인터페이스. 비어 있거나 파괴되었으면 null.</summary>
    public T Value
    {
        get => target != null ? target as T : null;
        set
        {
            if (value != null && value is not Object)
                throw new ArgumentException($"{typeof(T).Name}은(는) UnityEngine.Object여야 직렬화할 수 있습니다.");
            target = value as Object;
        }
    }

    /// <summary>실제로 직렬화된 Unity 오브젝트.</summary>
    public Object Target => target;

    public bool HasValue => Value != null;

    public bool TryGetValue(out T value)
    {
        value = Value;
        return value != null;
    }

    public static implicit operator T(InterfaceReference<T> reference) => reference?.Value;
}
