using System.Runtime.CompilerServices;

namespace NineTailedFox.BatchImageFormatConversion.Application.Boot
{
	internal static class AppBootstrap
	{
		internal static readonly long BootstrapTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		[ModuleInitializer]
		public static void Init()
		{
			AssemblyLoader.Register();
		}
	}
}