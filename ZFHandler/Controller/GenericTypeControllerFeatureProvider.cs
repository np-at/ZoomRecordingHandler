using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using ZoomFileManager.Models;

namespace ZFHandler.Controller
{
    /// <summary>/// This is just a marker attribute used to allow us to identifier which entities to expose in the API
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ApiEntityAttribute : Attribute
    {
    } // These are two example entities that will be supported by the generic controller [ApiEntityAttribute]public class Animals { } [ApiEntityAttribute]public class Insects { }

    public static class IncludedEntities
    {
        public static IReadOnlyList<TypeInfo> Types;

        static IncludedEntities()
        {
            var assembly = typeof(IncludedEntities).GetTypeInfo().Assembly;
            var typeList = new List<TypeInfo>();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(ApiEntityAttribute), true).Length > 0)
                {
                    typeList.Add(type.GetTypeInfo());
                }
            }

            Types = typeList;
        }
    }

    public static class IncludedEntities2
    {
        public static IReadOnlyList<TypeInfo> TypeInfos;

        static IncludedEntities2()
        {
            TypeInfos = new List<TypeInfo>
            {
                typeof(Zoominput).GetTypeInfo()
            };
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GenericControllerNameAttribute : Attribute, IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.GetGenericTypeDefinition() == typeof(GenericController<>))
            {
                var entityType = controller.ControllerType.GenericTypeArguments[0];
                controller.ControllerName = entityType.Name;
            }
        }
    }


    [Route("[controller]")]
    [GenericControllerNameAttribute]
    public class GenericController<T> : ControllerBase
    {
        [HttpGet]
        public IActionResult IndexAsync()
        {
            return Content($"GET from a {typeof(T).Name} controller.");
        }

        [HttpPost]
        public IActionResult Create([FromBody] IEnumerable<T> items)
        {
            return Content($"POST to a {typeof(T).Name} controller.");
        }
    }

    public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            // Get the list of entities that we want to support for the generic controller
            foreach (var entityType in IncludedEntities2.TypeInfos)
            {
                var typeName = $"{entityType.Name}Controller";
                // Check to see if there is a "real" controller for this class
                if (feature.Controllers.All(t => t.Name != typeName))
                {
                    // Create a generic controller for this type
                    var controllerType = typeof(WebReceiver<>).MakeGenericType(entityType.AsType()).GetTypeInfo();
                    feature.Controllers.Add(controllerType);
                }
            }
        }
    }

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


            foreach (var candidate in _candidates)
            {
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