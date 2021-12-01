using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection.Emit;

namespace WebhookFileMover.Controllers
{
    public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly TypeInfo[]? _types;

        public GenericControllerFeatureProvider()
        {
        }

        public GenericControllerFeatureProvider(params TypeInfo[] types)
        {
            _types = types;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            // Get the list of entities that we want to support for the generic controller
            foreach (var entityType in GenericTypeControllerFeatureProvider.IncludedEntities2.TypeInfos)
            {
                var typeName = $"{entityType.Name}Controller";
                // Check to see if there is a "real" controller for this class
                if (feature.Controllers.All(t => t.Name != typeName))
                {
                    // Create a generic controller for this type
                    var controllerType = typeof(GenericReceiverController<>).MakeGenericType(entityType.AsType())
                        .GetTypeInfo();
                    feature.Controllers.Add(controllerType);
                }
            }

            if (_types != null)
                foreach (var entityType in _types)
                {
                    string typeName = $"{entityType.Name}Controller";

                    if (feature.Controllers.All(t => t.Name != typeName))
                    {
                        // Create a generic controller for this type
                        var controllerType = typeof(GenericReceiverController<>).MakeGenericType(entityType.AsType())
                            .GetTypeInfo();
                        feature.Controllers.Add(controllerType);
                    }
                    
                }
        }
    }
}