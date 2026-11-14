using System;
using UnityEngine;

/// <summary>
/// [SerializeReference] 필드(인터페이스/추상 클래스, 단일 또는 List/배열)에
/// 구현 클래스를 고르는 드롭다운을 인스펙터에 띄운다.
/// 사용 예: [SerializeReference, SubclassSelector] private List&lt;ITigger&gt; triggers = new();
/// 구현 클래스는 [Serializable]이어야 하고 매개변수 없는 생성자가 있어야 하며,
/// MonoBehaviour/ScriptableObject가 아닌 일반 C# 클래스여야 한다.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SubclassSelectorAttribute : PropertyAttribute
{
}
