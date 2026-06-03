using UnityEngine;

namespace Creatush.TweenEffectsPro
{
    /// <summary>
    /// Apply to a string field alongside an EffectCatalogue reference to get
    /// a validated dropdown of available keys in the Inspector.
    ///
    /// Usage:
    ///   [SerializeField] private EffectCatalogue catalogue;
    ///   [CatalogueKey(nameof(catalogue))]
    ///   [SerializeField] private string effectKey;
    /// </summary>
    public class CatalogueKeyAttribute : PropertyAttribute
    {
        /// <summary>Name of the sibling EffectCatalogue field on the same component.</summary>
        public readonly string catalogueFieldName;

        public CatalogueKeyAttribute(string catalogueFieldName)
        {
            this.catalogueFieldName = catalogueFieldName;
        }
    }
}
