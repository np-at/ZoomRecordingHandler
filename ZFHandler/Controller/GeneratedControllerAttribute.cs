using System;

namespace ZFHandler.Controller
{
    
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GeneratedControllerAttribute : Attribute
    {
        public GeneratedControllerAttribute(string? route = null)
        {
            Route = route ?? string.Empty;
        }

        public string Route { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CreateReceiverAttribute : Attribute
    {
        public CreateReceiverAttribute(string? route = null)
        {
            Route = route ?? string.Empty;
        }
        public string Route { get; set; }
    }
}