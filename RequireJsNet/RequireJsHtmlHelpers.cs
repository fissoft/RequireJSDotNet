/*
 * RequireJS.NET
 * Copyright Stefan Prodan
 *   http://stefanprodan.eu
 * Dual licensed under the MIT and GPL licenses:
 *   http://www.opensource.org/licenses/mit-license.php
 *   http://www.gnu.org/licenses/gpl.html
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

using RequireJsNet;
using RequireJsNet.Configuration;
using RequireJsNet.Helpers;
using RequireJsNet.Models;
using System.Web;
namespace RequireJS
{


    public static class RequireJsHtmlHelpers
    {
        #region cost

        private const string DefaultConfigPath = "~/RequireJS.config";
        private const string DefaultEntryPointRoot = "~/Scripts/";
        private const string DefaultArea = "Common";
        private const string ViewDataControllerKey = "RequireJsController";
        private const string ViewDataActionKey = "RequireJsAction";
        private const string ViewDataRequireJsAllowKey = "RequireJsAlwaysAllowed";

        #endregion

        /// <summary>
        /// Setup RequireJS to be used in layouts
        /// </summary>
        /// <example>
        /// @Html.RenderRequireJsSetup(Url.Content("~/Scripts"), Url.Content("~/Scripts/require.js"), "~/RequireJS.release.config")
        /// </example>
        /// <param name="html">
        /// HtmlHelper instance
        /// </param>
        /// <param name="baseUrl">
        /// scripts base url
        /// </param>
        /// <param name="requireUrl">
        /// requirejs.js url
        /// </param>
        /// <param name="urlArgs">
        /// url arguments
        /// </param>
        /// <param name="configPath">
        /// RequireJS.config server local path
        /// </param>
        /// <param name="entryPointRoot">
        /// Scripts folder relative path ex. ~/Scripts/
        /// </param>
        /// <param name="logger">
        /// Logger to output errors
        /// </param>
        /// <returns>
        /// The resulting <see cref="MvcHtmlString"/>.
        /// </returns>
        public static MvcHtmlString RenderRequireJsSetup(
            this HtmlHelper html, 
            string baseUrl, 
            string requireUrl, 
            string urlArgs = "",
            string configPath = "", 
            string entryPointRoot = "~/Scripts/", 
            IRequireJsLogger logger = null,
            bool loadOverrides = true)
        {
            if (string.IsNullOrEmpty(configPath))
            {
                configPath = DefaultConfigPath;
            }

            return html.RenderRequireJsSetup(baseUrl, requireUrl, urlArgs, new List<string> { configPath }, entryPointRoot, logger, loadOverrides);
        }

        /// <summary>
        /// Setup RequireJS to be used in layouts
        /// </summary>
        /// <param name="html">
        /// The html.
        /// </param>
        /// <param name="baseUrl">
        /// scripts base url
        /// </param>
        /// <param name="requireUrl">
        /// requirejs.js url
        /// </param>
        /// <param name="configsList">
        /// RequireJS.config files path
        /// </param>
        /// <param name="entryPointRoot">
        /// Scripts folder relative path ex. ~/Scripts/
        /// </param>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <returns>
        /// The <see cref="MvcHtmlString"/>.
        /// </returns>
        public static MvcHtmlString RenderRequireJsSetup(
            this HtmlHelper html,
            string baseUrl,
            string requireUrl,
            IList<string> configsList,
            string entryPointRoot = "~/Scripts/",
            IRequireJsLogger logger = null,
            bool loadOverrides = true)
        {
            return html.RenderRequireJsSetup(baseUrl, requireUrl, null, configsList, entryPointRoot, logger, loadOverrides);
        }

        /// <summary>
        /// Setup RequireJS to be used in layouts
        /// </summary>
        /// <param name="html">
        /// The html.
        /// </param>
        /// <param name="baseUrl">
        /// scripts base url
        /// </param>
        /// <param name="requireUrl">
        /// requirejs.js url
        /// </param>
        /// <param name="urlArgs">
        /// </param>
        /// <param name="configsList">
        /// RequireJS.config files path
        /// </param>
        /// <param name="entryPointRoot">
        /// Scripts folder relative path ex. ~/Scripts/
        /// </param>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <returns>
        /// The <see cref="MvcHtmlString"/>.
        /// </returns>
        public static MvcHtmlString RenderRequireJsSetup(
            this HtmlHelper html,
            string baseUrl,
            string requireUrl,
            string urlArgs,
            IList<string> configsList,
            string entryPointRoot = "~/Scripts/",
            IRequireJsLogger logger = null,
            bool loadOverrides = true)
        {
            var entryPointPath = html.RequireJsEntryPoint(entryPointRoot);

            if (entryPointPath == null)
            {
                return new MvcHtmlString(string.Empty);
            }

            if (configsList == null || !configsList.Any())
            {
                throw new Exception("No config files to load.");
            }

            var processedConfigs = configsList.Select(r =>
            {
                var resultingPath = html.ViewContext.HttpContext.MapPath(r);
                PathHelpers.VerifyFileExists(resultingPath);
                return resultingPath;
            }).ToList();

            var loader = new ConfigLoader(processedConfigs, logger, new ConfigLoaderOptions { LoadOverrides = loadOverrides });
            var resultingConfig = loader.Get();
            var overrider = new ConfigOverrider();
            overrider.Override(resultingConfig, entryPointPath.ToString().ToModuleName());
            var outputConfig = new JsonConfig
            {
                BaseUrl = baseUrl,
                Locale = html.CurrentCulture(),
                UrlArgs = urlArgs,
                Paths = resultingConfig.Paths.PathList.ToDictionary(r => r.Key, r => r.Value),
                Shim = resultingConfig.Shim.ShimEntries.ToDictionary(
                        r => r.For,
                        r => new JsonRequireDeps
                                 {
                                     Dependencies = r.Dependencies.Select(x => x.Dependency).ToList(),
                                     Exports = r.Exports
                                 }),
                Map = resultingConfig.Map.MapElements.ToDictionary(
                         r => r.For,
                         r => r.Replacements.ToDictionary(x => x.OldKey, x => x.NewKey))
            };

            var options = new JsonRequireOptions
            {
                Locale = html.CurrentCulture(),
                PageOptions = html.ViewBag.PageOptions,
                WebsiteOptions = html.ViewBag.GlobalOptions
            };

            var configBuilder = new JavaScriptBuilder();
            configBuilder.AddStatement(JavaScriptHelpers.SerializeAsVariable(options, "requireConfig"));
            configBuilder.AddStatement(JavaScriptHelpers.SerializeAsVariable(outputConfig, "require"));

            var requireRootBuilder = new JavaScriptBuilder(); 
            var requireEntryPointBuilder = new JavaScriptBuilder();
            if (ViewDataRequireJsAllowKey != entryPointPath.ToHtmlString())
            {
                requireRootBuilder.AddAttributesToStatement("src", requireUrl);
                requireEntryPointBuilder.AddStatement(
                    JavaScriptHelpers.MethodCall(
                        "require",
                        (object) new[] {entryPointPath.ToString()}));
                return new MvcHtmlString(
                    string.Join("", configBuilder.Render()
                        , Environment.NewLine
                        , requireRootBuilder.Render()
                        , Environment.NewLine
                        , requireEntryPointBuilder.Render()));
            }
            return new MvcHtmlString(
                string.Join("",configBuilder.Render()));
        }

        /// <summary>
        /// Returns entry point script relative path
        /// </summary>
        /// <param name="html">
        /// The HtmlHelper instance.
        /// </param>
        /// <param name="root">
        /// Relative root path ex. ~/Scripts/
        /// </param>
        /// <returns>
        /// The <see cref="MvcHtmlString"/>.
        /// </returns>
        public static MvcHtmlString RequireJsEntryPoint(this HtmlHelper html, string root)
        {
           
            var rootUrl = string.Empty;
            var withBaseUrl = true;
          

            if (root != DefaultEntryPointRoot)
            {
                withBaseUrl = false;
                rootUrl = UrlHelper.GenerateContentUrl(root, html.ViewContext.HttpContext);
            }
            string entryPointTmpl, entryPoint, filePath;
            var server = html.ViewContext.HttpContext.Server;
            var routingInfo = html.GetRoutingInfo();
            //对于用户自行指定
            if (html.ViewData.ContainsKey(ViewDataControllerKey) || html.ViewData.ContainsKey(ViewDataActionKey))
            {
                entryPointTmpl = string.Format("Controllers/{{0}}/{0}/{1}", 
                    (html.ViewData[ViewDataControllerKey] ?? routingInfo.Controller), 
                    (html.ViewData[ViewDataActionKey] ?? routingInfo.Action));
                 entryPoint = string.Format(entryPointTmpl, routingInfo.Area).ToModuleName();
                 filePath = server.MapPath(string.Format("{0}{1}.js", root, entryPoint));

                if (File.Exists(filePath))
                {
                    var computedEntry = GetEntryPoint(server, filePath, root);
                    return new MvcHtmlString(withBaseUrl ? computedEntry : string.Format("{0}{1}.js", rootUrl, computedEntry));
                }
            }

            // search for controller/action.js in current area
             entryPointTmpl = string.Format("Controllers/{{0}}/{0}/{1}", routingInfo.Controller, routingInfo.Action);
             entryPoint = string.Format(entryPointTmpl, routingInfo.Area).ToModuleName();
             filePath = server.MapPath(string.Format("{0}{1}.js", root, entryPoint));

            if (File.Exists(filePath))
            {
                var computedEntry = GetEntryPoint(server, filePath, root);
                return new MvcHtmlString(withBaseUrl ? computedEntry : string.Format("{0}{1}.js", rootUrl, computedEntry));
            }

            // search for controller/action.js in common area
            entryPoint = string.Format(entryPointTmpl, DefaultArea).ToModuleName();
            filePath = server.MapPath(string.Format("{0}{1}.js", root, entryPoint));

            if (File.Exists(filePath))
            {
                var computedEntry = GetEntryPoint(server, filePath, root);
                return new MvcHtmlString(withBaseUrl ? computedEntry : string.Format("{0}{1}.js", rootUrl, computedEntry));
            }

            // search for controller/controller-action.js in current area
            entryPointTmpl = string.Format("Controllers/{{0}}/{0}/{0}-{1}", routingInfo.Controller, routingInfo.Action);
            entryPoint = string.Format(entryPointTmpl, routingInfo.Area).ToModuleName();
            filePath = server.MapPath(string.Format("{0}{1}.js", root, entryPoint));

            if (File.Exists(filePath))
            {
                var computedEntry = GetEntryPoint(server, filePath, root);
                return new MvcHtmlString(withBaseUrl ? computedEntry : string.Format("{0}{1}.js", rootUrl, computedEntry));
            }

            // search for controller/controller-action.js in common area
            entryPoint = string.Format(entryPointTmpl, DefaultArea).ToModuleName();
            filePath = server.MapPath(string.Format("{0}{1}.js", root, entryPoint));

            if (File.Exists(filePath))
            {
                var computedEntry = GetEntryPoint(server, filePath, root);
                return new MvcHtmlString(withBaseUrl ? computedEntry : string.Format("{0}{1}.js", rootUrl, computedEntry));
            }

            //load for default
            entryPointTmpl = string.Format("Controllers/{{0}}/{0}/__default", routingInfo.Controller);
            entryPoint = string.Format(entryPointTmpl, routingInfo.Area).ToModuleName();
            filePath = server.MapPath(string.Format("{0}{1}.js", root, entryPoint));

            if (File.Exists(filePath))
            {
                var computedEntry = GetEntryPoint(server, filePath, root);
                return new MvcHtmlString(withBaseUrl ? computedEntry : string.Format("{0}{1}.js", rootUrl, computedEntry));
            }
            if ("true".Equals(html.ViewData[ViewDataRequireJsAllowKey]))
            {
                return new MvcHtmlString(ViewDataRequireJsAllowKey);
            }
            return null;
        }

        private static string GetEntryPoint(HttpServerUtilityBase server, string filePath, string root)
        {
            
            var fileName = PathHelpers.GetExactFilePath(filePath);
            var folder = server.MapPath(root);
            return PathHelpers.GetRequireRelativePath(folder, fileName);
        }

        public static string CurrentCulture(this HtmlHelper html)
        {
            // split the ro-Ro string by '-' so it returns eg. ro / en
            return System.Threading.Thread.CurrentThread.CurrentCulture.Name.Split('-')[0];
        }

        public static Dictionary<string, int> ToJsonDictionary<TEnum>()
        {
            var enumType = typeof(TEnum);
            return Enum.GetNames(enumType).ToDictionary(r => r, r => Convert.ToInt32(Enum.Parse(enumType, r)));
        }

    }
}