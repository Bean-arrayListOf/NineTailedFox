using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;
using NineTailedFox.Atomic.AppKit;
using NineTailedFox.Atomic.LoggerKit;
using NineTailedFox.Atomic.NioKit;
using NineTailedFox.Atomic.TimeKit;
using NineTailedFox.BatchImageFormatConversion.Application.Boot;
using NineTailedFox.BatchImageFormatConversion.Application.Key;
using Serilog;
using Serilog.Events;
using SQLitePCL;
using Path = System.IO.Path;

namespace NineTailedFox.BatchImageFormatConversion.Application
{
	internal class Program
	{
		public static int Main(string[] args)
		{
			AssemblyLoader.Register();
			InitLogger();
			IKey key = new DefaultKey();

			var keyStr = Environment.GetEnvironmentVariable("NTF_KEY");
			if (keyStr==null)
			{
				throw new ArgumentNullException("环境变量内未找到[NTF_KEY], 无法验证秘钥.");
			}

			if (!key.Verify(keyStr))
			{
				throw new RuntimeBinderException($"验证秘钥失败,密钥库未找到此秘钥[{keyStr}]");
			}
			
			return Run(args);
			//Test().Wait();
			//return 0;
			//Fl();
			//Push();
			return 0;
		}

		private static int Run(string[] args)
		{
			
			var log = LoggerKit.GetLogger<Program>();
			var exitCode = new Access().Run(args.ToList());
			log.Information("运行结束[{0}] 代码[{exitCode}]",(TimestampKit.GetUnixTimestamp()-AppBootstrap.BootstrapTimestamp).ToReadableDuration(), exitCode);
			return exitCode;
		}

		private static void InitLogger()
		{
			var localDateTime = DateTimeOffset.FromUnixTimeMilliseconds(AppBootstrap.BootstrapTimestamp).UtcDateTime;
			LoggerKit.InitLoggerConfigIsSqlite(Path.Combine(AppInfo.AppLogDirectory,localDateTime.ToString("yyyy_MM_dd"),$"Log-{localDateTime.ToString("HH_mm_ss")}.log.db"),LogEventLevel.Verbose);
		}
		
		static void Fl()
		{
			var log = Log.ForContext<Program>();
			
			var inputDir = "/Users/arraylistof/Downloads/pixiv1/1";
			var outputDir = "/Users/arraylistof/Downloads/pixiv1/2";

			var rawExtensions = new List<string>();
			rawExtensions.Add("jpeg");
			rawExtensions.Add("jpg");
			rawExtensions.Add("png");
			rawExtensions.Add("tiff");
			
			var normalizedExtensions = rawExtensions
									   .Select(ext => ext.StartsWith('.') ? ext : "." + ext)
									   .ToHashSet(StringComparer.OrdinalIgnoreCase);
			
			var naturalComparer = StringComparer.Create(
				CultureInfo.InvariantCulture, 
				CompareOptions.NumericOrdering
			);

			var files = Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories)
								 .Select(x => new FileInfo(x))
								 .Where(x => normalizedExtensions.Contains(x.Extension))
								 .OrderByDescending(f => f.Name,naturalComparer)
								 .ToList();

			for (var i = 0; i < files.Count; i++)
			{
				var file = files[i];
				var fileName = file.Name;
				var aName = fileName.Split("_")[0];

				var outputFile = Path.Combine(outputDir, aName,fileName);
				if (!Directory.GetParent(outputFile)!.Exists)
				{
					Directory.GetParent(outputFile)!.Create();
				}

				file.CopyTo(outputFile);
				log.Information("[{0}/{1}]:{2} => {3}",(i+1).ToString(),files.Count.ToString(),file.FullName,outputFile);
			}
			
		}

		public static int Add(string path)
		{
			var shell = new ProcessRunner
			{
				OutputHandler = ProcessRunner.DefaultOutput,
				ErrorHandler = ProcessRunner.DefaultError,
				WaitTimeout = -1,
				AllowInfiniteWait = true
			};

			var file = new FileInfo(path);
			
			var fileName = file.Name.Split("_")[0];
			
			return shell.CallAsync(GetCodes(fileName,file.FullName)).Result;
		}

		public static void Push(string path)
		{
			var rawExtensions = new List<string>
			{
				"jpeg",
				"jpg",
				"png",
				"tiff",
				"heif",
				"gif"
			};

			var normalizedExtensions = rawExtensions
									   .Select(ext => ext.StartsWith('.') ? ext : "." + ext)
									   .ToHashSet(StringComparer.OrdinalIgnoreCase);
			
			var dirs = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
								.Select(x => new FileInfo(x))
								.Where(x => normalizedExtensions.Contains(x.Extension))
								.ToList();
			
			var shell = new ProcessRunner
			{
				OutputHandler = ProcessRunner.DefaultOutput,
				ErrorHandler = ProcessRunner.DefaultError,
				WaitTimeout = -1,
				AllowInfiniteWait = true
			};
			
			for (int i = 0; i < dirs.Count; i++)
			{
				var dir = dirs[i];
				var fileName = dir.Name.Split("_")[0];
				//var AName = $"Pixiv-{fileName}";
				//log.Information("[{3}/{4}]: [{0}] Push To [{1}] a [{2}]",Path.GetFileName(dir),"immich",$"Pixiv-{Path.GetFileName(dir)}",(i+1).ToString(),dirs.Length.ToString());
				var exitCode = shell.CallAsync(GetCodes(fileName,dir.FullName)).Result;
			}
		}

		public static List<string> GetCodes(string pid,string dir)
		{
			return [
				"/opt/homebrew/bin/immich",
				"-d",
				"/Users/arraylistof/.config/immich/CitrusCat",
				"upload",
				"--recursive",
				$"--album-name=Pixiv-{pid}",
				Path.GetFullPath(dir)
			];
		}
	}
}