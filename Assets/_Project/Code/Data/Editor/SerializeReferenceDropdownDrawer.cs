using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using STSlike.Domain.Effects;

namespace STSlike.Data.Editor
{
    /// <summary>
    /// Draws each element of a [SerializeReference] field marked with
    /// [SerializeReferenceDropdown] as a type-picker dropdown (listing every
    /// concrete ICardEffect) plus the selected type's fields below it. The
    /// dropdown stays clickable after a type is chosen, so effects can be
    /// swapped in place.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializeReferenceDropdownAttribute))]
    public sealed class SerializeReferenceDropdownDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float height = line; // dropdown line

            if (HasType(property))
            {
                foreach (SerializedProperty child in GetChildren(property))
                    height += spacing + EditorGUI.GetPropertyHeight(child, true);
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Header line: element label + type dropdown.
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, line);
            EditorGUI.LabelField(labelRect, label);

            var dropdownRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                Mathf.Max(80f, position.width - EditorGUIUtility.labelWidth),
                line);

            string currentName = TypeName(property.managedReferenceFullTypename) ?? "(select effect type)";
            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(currentName), FocusType.Keyboard))
                ShowMenu(property);

            // Field lines: the selected effect's serialized fields (e.g. Amount).
            if (HasType(property))
            {
                float y = position.y + line + spacing;
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (SerializedProperty child in GetChildren(property))
                    {
                        float h = EditorGUI.GetPropertyHeight(child, true);
                        EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), child, true);
                        y += h + spacing;
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private static bool HasType(SerializedProperty property)
            => !string.IsNullOrEmpty(property.managedReferenceFullTypename);

        private static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, end))
                        yield break;
                    yield return iterator.Copy();
                }
                while (iterator.NextVisible(false));
            }
        }

        private static void ShowMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("(none)"), false, () => Assign(property, null));

            var types = TypeCache.GetTypesDerivedFrom<ICardEffect>()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
                .OrderBy(t => t.Name);

            foreach (Type type in types)
            {
                Type captured = type;
                menu.AddItem(new GUIContent(type.Name), false, () => Assign(property, captured));
            }

            menu.ShowAsContext();
        }

        private static void Assign(SerializedProperty property, Type type)
        {
            property.serializedObject.Update();
            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            property.serializedObject.ApplyModifiedProperties();
        }

        private static string TypeName(string managedReferenceFullTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceFullTypename))
                return null;

            // Format is "<assembly> <namespace>.<Type>"; show just the type name.
            int space = managedReferenceFullTypename.IndexOf(' ');
            string full = space >= 0
                ? managedReferenceFullTypename.Substring(space + 1)
                : managedReferenceFullTypename;

            int dot = full.LastIndexOf('.');
            return dot >= 0 ? full.Substring(dot + 1) : full;
        }
    }
}
