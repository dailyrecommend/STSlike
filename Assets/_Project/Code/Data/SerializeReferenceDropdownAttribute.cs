using UnityEngine;

namespace STSlike.Data
{
    /// <summary>
    /// Marks a [SerializeReference] field so the inspector shows a dropdown to
    /// pick the concrete type for each element (e.g. which ICardEffect).
    /// </summary>
    public sealed class SerializeReferenceDropdownAttribute : PropertyAttribute
    {
    }
}
