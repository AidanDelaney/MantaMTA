using System.Web.Optimization;

namespace WebInterface
{
	public class BundleConfig
	{
		// For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
		public static void RegisterBundles(BundleCollection bundles)
		{
			BundleTable.EnableOptimizations = false;

			// Scripts
			bundles.Add(new ScriptBundle("~/resources/js/jquery").Include("~/Scripts/jquery-{version}.js"));
			bundles.Add(new ScriptBundle("~/resources/js/bootstrap").Include("~/Scripts/bootstrap.js"));
			bundles.Add(new ScriptBundle("~/resources/js/jqplot").Include("~/Scripts/jqplot/jquery.jqplot.min.js"));


			// Styles
			bundles.Add(new StyleBundle("~/resources/css/bootstrap").Include("~/Content/bootstrap.css",
																			 "~/Content/bootstrap-responsive.css",
																			 "~/Content/app.css",
																			 "~/Content/fonts.css"));
			bundles.Add(new ScriptBundle("~/resources/css/jqplot").Include("~/Scripts/jqplot/jquery.jqplot.min.css"));
		}
	}
}