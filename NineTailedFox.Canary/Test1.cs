using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HDF5CSharp;
using NineTailedFox.Atomic.AppKit;
using NineTailedFox.Atomic.Extensions;
using NineTailedFox.Atomic.LangKit;
using NineTailedFox.Atomic.LoggerKit;
using Serilog;

namespace NineTailedFox.Canary
{
	public class Test1
	{
		private void initLogger()
		{
			LoggerKit.InitLoggerConfig();
		}
		
		[Test]
		public void Main()
		{
			initLogger();

			AppInfo.AppDataProvider.GetFileInfo("");

			var log = Log.ForContext<Test1>();
			
			
			foreach (var i in Enumerable.Range(0,10))
			{
				log.Information("[{0}/{1}]: {2}",i,10,RandomKit.GenerateRandomBool());
			}

		}
	}
}