namespace Public.Api.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Configuration;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.Extensions.Configuration;

    public class FeatureToggleConvention : IActionModelConvention
    {
        private readonly Dictionary<string, bool> _features;
        public FeatureToggleConvention([FromServices] IConfiguration configuration)
        {
            _features = configuration.GetSection(FeatureToggleOptions.ConfigurationKey).GetChildren().AsEnumerable()
                .ToDictionary(x => x.Key, y => Convert.ToBoolean(y.Value));
        }

        public void Apply(ActionModel action)
        {
            if (action.ApiExplorer.IsVisible.HasValue && !action.ApiExplorer.IsVisible.Value)
            {
                return;
            }

            var ns = action.DisplayName.Split('.');
            if (ToggleOslo(action, ns))
                return;

            if (ToggleTicketing(action, ns))
                return;

            action.ApiExplorer.IsVisible =  !_features.ContainsKey(action.ActionName) ||
                                            (_features.ContainsKey(action.ActionName) && _features[action.ActionName]);
        }

        private bool ToggleOslo(ActionModel action, string[] ns)
        {
            if (ns.Length <= 2 || ns[3] != "Oslo")
            {
                return false;
            }

            var featureName = $"Is{ns[2]}OsloApiEnabled";
            action.ApiExplorer.IsVisible = !_features.ContainsKey(featureName) ||
                                           (_features.ContainsKey(featureName) && _features[featureName]);
            return true;
        }

        private bool ToggleTicketing(ActionModel action, string[] ns)
        {
            if (ns.Length <= 1 || ns[2] != "Tickets")
            {
                return false;
            }

            const string featureName = "Ticketing";
            action.ApiExplorer.IsVisible = !_features.ContainsKey(featureName) ||
                                           (_features.ContainsKey(featureName) && _features[featureName]);
            return true;
        }
    }
}
