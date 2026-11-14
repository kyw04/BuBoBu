using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    private static readonly Dictionary<Type, Type[]> CandidateCache = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.LabelField(position, label.text, "[SubclassSelector]는 [SerializeReference] 필드에만 사용");
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        // 첫 줄 오른쪽(값 영역)에 타입 선택 드롭다운. 폴드아웃보다 먼저 그려야 클릭을 먼저 받는다.
        var buttonRect = new Rect(
            position.x + EditorGUIUtility.labelWidth + 2f, position.y,
            position.width - EditorGUIUtility.labelWidth - 2f, EditorGUIUtility.singleLineHeight);

        var currentType = property.managedReferenceValue?.GetType();
        var buttonText = new GUIContent(currentType != null ? ObjectNames.NicifyVariableName(currentType.Name) : "(None)");

        if (EditorGUI.DropdownButton(buttonRect, buttonText, FocusType.Keyboard))
            ShowTypeMenu(property, currentType);

        // 선택된 타입의 필드들을 폴드아웃으로 표시
        EditorGUI.PropertyField(position, property, label, true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUI.GetPropertyHeight(property, label, true);

    private static void ShowTypeMenu(SerializedProperty property, Type currentType)
    {
        var baseType = GetFieldType(property);
        var menu = new GenericMenu();
        var serializedObject = property.serializedObject;
        var propertyPath = property.propertyPath;

        menu.AddItem(new GUIContent("(None)"), currentType == null,
            () => SetValue(serializedObject, propertyPath, null));

        if (baseType == null)
        {
            menu.AddDisabledItem(new GUIContent("필드 타입을 찾을 수 없음"));
        }
        else
        {
            var candidates = GetCandidates(baseType);
            if (candidates.Length == 0)
                menu.AddDisabledItem(new GUIContent($"{baseType.Name} 구현 클래스 없음"));

            foreach (var type in candidates)
            {
                var path = string.IsNullOrEmpty(type.Namespace)
                    ? ObjectNames.NicifyVariableName(type.Name)
                    : $"{type.Namespace.Replace('.', '/')}/{ObjectNames.NicifyVariableName(type.Name)}";
                menu.AddItem(new GUIContent(path), type == currentType,
                    () => SetValue(serializedObject, propertyPath, Activator.CreateInstance(type)));
            }
        }

        menu.ShowAsContext();
    }

    private static void SetValue(SerializedObject serializedObject, string propertyPath, object value)
    {
        serializedObject.Update();
        var property = serializedObject.FindProperty(propertyPath);
        property.managedReferenceValue = value;
        property.isExpanded = value != null;
        serializedObject.ApplyModifiedProperties();
    }

    // "Assembly-CSharp Namespace.Outer/Inner" 형식의 필드 타입 문자열을 Type으로 변환
    private static Type GetFieldType(SerializedProperty property)
    {
        var typeName = property.managedReferenceFieldTypename;
        if (string.IsNullOrEmpty(typeName)) return null;

        var split = typeName.IndexOf(' ');
        var assemblyName = typeName.Substring(0, split);
        var className = typeName.Substring(split + 1).Replace('/', '+');
        return Type.GetType($"{className}, {assemblyName}");
    }

    private static Type[] GetCandidates(Type baseType)
    {
        if (CandidateCache.TryGetValue(baseType, out var cached)) return cached;

        var result = TypeCache.GetTypesDerivedFrom(baseType)
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
            .Where(t => !typeof(UnityEngine.Object).IsAssignableFrom(t))
            .Where(t => t.IsSerializable || t.IsValueType)
            .Where(t => t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.Name)
            .ToArray();

        CandidateCache[baseType] = result;
        return result;
    }
}
