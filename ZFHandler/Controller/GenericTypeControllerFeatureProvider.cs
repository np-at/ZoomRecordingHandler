using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ZFHandler.Controller
{
    public class GenericTypeControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly IEnumerable<Type> _candidates;

        public GenericTypeControllerFeatureProvider(IEnumerable<Type> candidates)
        {
            _candidates = candidates;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            // var candidates = currentAssembly.GetExportedTypes()
            //     .Where(x => x.GetCustomAttributes<GeneratedControllerAttribute>().Any());

            
            foreach (var candidate in _candidates) {
                try
                {
                    feature.Controllers.Add(typeof(WebReceiver<>).MakeGenericType(candidate).GetTypeInfo());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
    }

    public class GenericControllerRouteConvention : IControllerModelConvention
    {
        public GenericControllerRouteConvention()
        {
        }

        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.IsGenericType)
            {
                var genericType = controller.ControllerType.GenericTypeArguments[0];
                var customNameAttribute = genericType.GetCustomAttribute<CreateReceiverAttribute>();

                if (customNameAttribute?.Route != null)
                {
                    controller.Selectors.Add(new SelectorModel()
                    {
                        AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(customNameAttribute.Route))
                    });
                }
            }
        }
    }
}