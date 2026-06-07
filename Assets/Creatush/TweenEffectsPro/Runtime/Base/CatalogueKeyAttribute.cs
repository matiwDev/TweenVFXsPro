using UnityEngine;

namespace Creatush.TweenEffectsPro
{
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
