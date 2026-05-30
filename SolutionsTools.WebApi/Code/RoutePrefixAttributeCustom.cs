using System;
using System.Configuration;
using System.Web.Http.Routing;

namespace SolutionsTools.WebApi
{
    public class RoutePrefixAttributeCustom : Attribute, IRoutePrefix
    {
        public RoutePrefixAttributeCustom(string prefixParam) {
            var useApiPrefix = ConfigurationManager.AppSettings["UseApiPrefix"];

            if (useApiPrefix.Equals("N"))
            {
                Prefix = string.Format("api/{0}", prefixParam);
            }
            else
            {
                Prefix = prefixParam;
            }
        }

        protected RoutePrefixAttributeCustom() { }

        public virtual string Prefix { get; }
    }

}