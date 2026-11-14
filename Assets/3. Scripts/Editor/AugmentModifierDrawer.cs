using UnityEditor;
using UnityEngine;

// AugmentModifier: target에 맞는 스탯 타입 필드만 표시
[CustomPropertyDrawer(typeof(AugmentModifier))]
public class AugmentModifierDrawer : PropertyDrawer
{
    private static float Line => EditorGUIUtility.singleLineHeight;
    private static float Space => EditorGUIUtility.standardVerticalSpacing;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var rect = new Rect(position.x, position.y, position.width, Line);
        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            foreach (var child in GetVisibleFields(property))
            {
                rect.y += Line + Space;
                EditorGUI.PropertyField(rect, child);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return Line;
        return Line + GetVisibleFields(property).Length * (Line + Space);
    }

    private static SerializedProperty[] GetVisibleFields(SerializedProperty property)
    {
        var target = property.FindPropertyRelative(nameof(AugmentModifier.target));
        var statField = (AugmentTarget)target.enumValueIndex switch
        {
            AugmentTarget.Player => nameof(AugmentModifier.playerStat),
            AugmentTarget.Gun => nameof(AugmentModifier.gunStat),
            AugmentTarget.Bullet => nameof(AugmentModifier.bulletStat),
            _ => null
        };

        var type = property.FindPropertyRelative(nameof(AugmentModifier.type));
        var value = property.FindPropertyRelative(nameof(AugmentModifier.value));

        return statField != null
            ? new[] { target, property.FindPropertyRelative(statField), type, value }
            : new[] { target, type, value };
    }
}
