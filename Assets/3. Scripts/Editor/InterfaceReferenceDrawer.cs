using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(InterfaceReference<>), true)]
public class InterfaceReferenceDrawer : PropertyDrawer
{
    private const string TargetField = "target";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var targetProp = property.FindPropertyRelative(TargetField);
        var interfaceType = GetInterfaceType();

        label = EditorGUI.BeginProperty(position, label, property);
        var shownLabel = new GUIContent($"{label.text} ({interfaceType?.Name ?? "?"})", label.tooltip);

        EditorGUI.BeginChangeCheck();
        var picked = EditorGUI.ObjectField(position, shownLabel, targetProp.objectReferenceValue, typeof(Object), true);
        if (EditorGUI.EndChangeCheck())
        {
            var resolved = Resolve(picked, interfaceType);
            if (picked != null && resolved == null)
                Debug.LogWarning($"[InterfaceReference] '{picked.name}'에 {interfaceType?.Name}을(를) 구현한 컴포넌트가 없습니다.", picked);
            else
                targetProp.objectReferenceValue = resolved;
        }

        EditorGUI.EndProperty();
    }

    // 드래그한 오브젝트에서 인터페이스를 구현한 대상을 찾는다.
    private static Object Resolve(Object picked, Type interfaceType)
    {
        if (picked == null || interfaceType == null) return null;
        if (interfaceType.IsInstanceOfType(picked)) return picked;

        var go = picked as GameObject ?? (picked as Component)?.gameObject;
        if (go == null) return null;

        foreach (var component in go.GetComponents<Component>())
            if (component != null && interfaceType.IsInstanceOfType(component))
                return component;
        return null;
    }

    // 필드 타입(단일 / 배열 / List)에서 InterfaceReference<T>의 T를 꺼낸다.
    private Type GetInterfaceType()
    {
        var type = fieldInfo.FieldType;
        if (type.IsArray) type = type.GetElementType();
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            type = type.GetGenericArguments()[0];

        while (type != null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(InterfaceReference<>))
                return type.GetGenericArguments()[0];
            type = type.BaseType;
        }
        return null;
    }
}
