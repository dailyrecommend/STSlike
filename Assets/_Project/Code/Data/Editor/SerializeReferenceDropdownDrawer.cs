using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace STSlike.Data.Editor
{
    /// <summary>
    /// Draws each element of a [SerializeReference] field marked with
    /// [SerializeReferenceDropdown] as a type-picker dropdown (listing every
    /// concrete type the field accepts) plus the selected type's fields below
    /// it. The dropdown stays clickable after a type is chosen, so a choice can
    /// be swapped in place.
    ///
    /// What it offers comes from the field, not from a fixed list — a card's
    /// effects offer effects, a relic's behaviour offers relics.
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

            string currentName = TypeName(property.managedReferenceFullTypename) ?? "(select type)";
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

            Type contract = ContractOf(property);
            if (contract == null)
            {
                menu.AddDisabledItem(new GUIContent("Field type could not be read"));
                menu.ShowAsContext();
                return;
            }

            var types = TypeCache.GetTypesDerivedFrom(contract)
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
                .OrderBy(t => t.Name);

            foreach (Type type in types)
            {
                Type captured = type;
                menu.AddItem(new GUIContent(type.Name), false, () => Assign(property, captured));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// What this field will accept, read from the field itself rather than
        /// assumed.
        ///
        /// Unity reports it as "Assembly Type" in a single string, and the
        /// assembly it names is the one that declared the contract — so a card's
        /// effects offer effects, and a relic's behaviour offers relics, from
        /// the same drawer.
        /// </summary>
        private static Type ContractOf(SerializedProperty property)
        {
            string[] parts = property.managedReferenceFieldTypename.Split(' ');
            if (parts.Length != 2)
                return null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != parts[0])
                    continue;

                return assembly.GetType(parts[1]);
            }

            return null;
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
