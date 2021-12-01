using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using WebhookFileMover.Extensions;
using WebhookFileMover.Models;

namespace WebhookFileMover.Controllers
{
    public class GenericTypeControllerFeatureProvider
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
                    // typeof(ZoomWebhook).GetTypeInfo()
                };
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        public class GenericControllerNameAttribute : Attribute, IControllerModelConvention
        {
            public void Apply(ControllerModel controller)
            {
                if (controller.ControllerType.GetGenericTypeDefinition() == typeof(GenericReceiverController<>))
                {
                    var entityType = controller.ControllerType.GenericTypeArguments[0];
                    controller.ControllerName = ServiceRegistrationExtensions.EndpointMapping.TryGetValue(entityType, out string? customControllerName) ? customControllerName : entityType.Name;
                }
            }
        }


        // [Route("[controller]")]
        // [GenericControllerNameAttribute]
        // public class GenericController<T> : ControllerBase
        // {
        //     [HttpGet]
        //     public IActionResult IndexAsync()
        //     {
        //         return Content($"GET from a {typeof(T).Name} controller.");
        //     }
        //
        //     [HttpPost]
        //     public IActionResult Create([FromBody] IEnumerable<T> items)
        //     {
        //         return Content($"POST to a {typeof(T).Name} controller.");
        //     }
        // }
        
        
    }
}