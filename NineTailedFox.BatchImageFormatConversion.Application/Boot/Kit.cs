using System.Reflection;

namespace NineTailedFox.BatchImageFormatConversion.Application.Boot
{
	internal static class Kit
	{
		public static readonly Assembly AppAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
		public static readonly AssemblyName AppAssemblyName = AppAssembly.GetName();
		public static readonly string AppName = AppAssemblyName.Name ?? "UnknownApp";
		public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
	}
}