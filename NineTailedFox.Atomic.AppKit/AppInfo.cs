using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace NineTailedFox.Atomic.AppKit
{
	public static class AppInfo
	{
		private static readonly Assembly AppAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
		private static readonly AssemblyName AppAssemblyName = AppAssembly.GetName();
		
		public static readonly string AppName = AppAssemblyName.Name ?? "UnknownApp";
		public static readonly string AppFullName = AppAssemblyName.FullName;
		public static readonly string AppCultureName = AppAssemblyName.CultureName ?? "en-US";
		
		public static readonly Version AppVersion = AppAssemblyName.Version ?? new Version(0,0,0,0);
		public static readonly int AppBuildVersion = AppVersion.Build;
		public static readonly int AppMajorVersion = AppVersion.Major;
		public static readonly int AppMajorRevisionVersion = AppVersion.MajorRevision;
		public static readonly int AppMinorVersion = AppVersion.Minor;
		public static readonly int AppMinorRevisionVersion = AppVersion.MinorRevision;
		public static readonly int AppRevisionVersion = AppVersion.Revision;
		
		public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		public static readonly string AppDataDirectory = Path.Combine(BaseDirectory, $"{AppName}_Data");
		public static readonly IFileProvider AppDataProvider = GetVfs(AppDataDirectory);
		public static readonly List<string> AppConfigPaths = [Path.Combine(AppDataDirectory,"config"),Path.Combine(BaseDirectory),Path.Combine(BaseDirectory,"config")];
		public static readonly string AppLogDirectory = Path.Combine(AppDataDirectory, "log");

		private static IFileProvider GetVfs(string basePath)
		{
			if (!Directory.Exists(basePath))
			{
				Directory.CreateDirectory(basePath);
			}

			return new PhysicalFileProvider(basePath,ExclusionFilters.None);
		}
	}
}