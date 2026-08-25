using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Attributes
{
    /// <summary>
    /// Declares that a controller action requires the caller to have a specific subscription feature.
    /// 
    /// Usage:
    ///   [RequireFeature("export_pdf")]
    ///   public IActionResult ExportPdf() { ... }
    /// 
    /// Internally uses <see cref="TypeFilterAttribute"/> to instantiate
    /// <see cref="Filters.FeatureAuthorizationFilter"/> with DI-resolved dependencies,
    /// passing the featureKey as a constructor argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireFeatureAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Initialises the attribute with the feature key to gate on.
        /// </summary>
        /// <param name="featureKey">
        /// The snake_case feature key (e.g. "export_pdf", "ai_suggestions").
        /// Must match a value in the Features table.
        /// </param>
        public RequireFeatureAttribute(string featureKey)
            : base(typeof(Filters.FeatureAuthorizationFilter))
        {
            Arguments = new object[] { featureKey };
        }
    }
}
