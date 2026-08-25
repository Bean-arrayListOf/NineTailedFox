using System.Globalization;
using CommandLine;
using Microsoft.CSharp.RuntimeBinder;
using NineTailedFox.Atomic.AppKit;
using NineTailedFox.Atomic.ConfigKit;
using NineTailedFox.Atomic.Extensions;
using NineTailedFox.Atomic.NioKit;
using NineTailedFox.Atomic.TimeKit;
using NineTailedFox.BatchImageFormatConversion.Application.Coder;
using Serilog;
using Path = System.IO.Path;

namespace NineTailedFox.BatchImageFormatConversion.Application
{
	/// <summary>
	/// 批量图片格式转换核心执行器
	/// <para>
	/// 基于 ImageMagick（转码）+ ExifTool（元数据迁移）工具链，<br/>
	/// 支持多线程并发、目录结构保持、源文件删除、VFS 缓存沙盒隔离等特性。
	/// </para>
	/// </summary>
	public class Access
	{
		private static readonly ILogger Log = Serilog.Log.ForContext<Access>();

		/// <summary> 本次运行的唯一标识，用于隔离缓存沙盒 </summary>
		private static readonly string Uuid = Guid.NewGuid().ToString("N");

		/// <summary> 默认支持的图片扩展名列表 </summary>
		private static readonly List<string> DefaultExtensions = ConfigKit.Gets("DefaultExtensions", [".png", ".jpg", ".jpeg", ".heif", ".avif", ".webp"], ',');

		/// <summary> VFS 数据根目录 </summary>
		private static readonly string DataRootPath = AppInfo.AppDataDirectory;

		/// <summary>
		/// 解析命令行参数并执行批量转换
		/// </summary>
		/// <param name="args">命令行参数列表</param>
		/// <returns>进程退出码（0 成功，非 0 失败）</returns>
		public int Run(List<string> args)
		{
			var exitCode = 0;
			Parser.Default.ParseArguments<Args>(args)
				.WithParsedAsync(async parsedArgs => { exitCode = await RunArgsAsync(parsedArgs).ConfigureAwait(false); })
				.GetAwaiter()
				.GetResult();
			return args.Count == 0 ? 1 : exitCode;
		}

		/// <summary>
		/// 校验工具链路径并启动批量处理
		/// </summary>
		private async Task<int> RunArgsAsync(Args args)
		{
			var finalExtensions = args.SupportedExtensions == null || !args.SupportedExtensions.Any()
				? DefaultExtensions
				: args.SupportedExtensions.ToList();

			var magickPath = (args.MagickPath ?? ConfigKit.GetString("Myrtle:BIFC:MagickPath")) ?? ProcessRunner.FindExecutableInPath("magick");

			if (magickPath == null)
			{
				Log.Fatal("未指定也未找到 [{Tool}] 可执行文件，请通过 [--magick-path] 参数指定", "ImageMagick");
				return 2;
			}

			var exifToolPath = (args.ExifToolPath ?? ConfigKit.GetString("Myrtle:BIFC:ExifToolPath")) ?? ProcessRunner.FindExecutableInPath("exiftool");
			if (exifToolPath != null)
			{
				return await ProcessBatchAsync(
					ResolveVfsPath(args.InputFile!),
					ResolveVfsPath(args.OutputFile!),
					args.OutputFormat!,
					Path.GetFullPath(magickPath),
					Path.GetFullPath(exifToolPath),
					finalExtensions,
					args.DeleteSource,
					args.KeepStructure,
					args.Threads,
					args.FSort,
					args.PushImmich
				).ConfigureAwait(false);
			}

			Log.Fatal("未指定也未找到 [{Tool}] 可执行文件，请通过 [--exiftool-path] 参数指定", "ExifTool");
			return 2;
		}

		/// <summary>
		/// 批量处理核心：扫描文件 → 并发转码 → 原子交付 → 清理缓存
		/// </summary>
		private async Task<int> ProcessBatchAsync(
			string inputDir, string outputDir, string format,
			string magickPath, string exifToolPath,
			List<string> rawExtensions, bool deleteSource, bool keepStructure,
			int threads, FileSort fileSort, bool pushImmich)
		{
			// 1. 标准化扩展名并扫描目标文件
			var normalizedExtensions = rawExtensions
				.Select(ext => ext.StartsWith('.') ? ext : "." + ext)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			var naturalComparer = StringComparer.Create(
				CultureInfo.InvariantCulture,
				CompareOptions.NumericOrdering
			);

			if (!VfsDirectoryExists(inputDir))
			{
				Log.Error("VFS 输入目录不存在: [{InputDir}]", inputDir);
				return 1;
			}

			var rawFileList = VfsEnumerateFiles(inputDir, "*", SearchOption.AllDirectories)
				.Select(x => new FileInfo(x))
				.Where(x => normalizedExtensions.Contains(x.Extension));

			List<FileInfo> files = fileSort switch
			{
				FileSort.Name => rawFileList.OrderBy(f => f.Name, naturalComparer).ToList(),
				FileSort.NameBy => rawFileList.OrderByDescending(f => f.Name, naturalComparer).ToList(),
				FileSort.Time => rawFileList.OrderBy(f => f.CreationTime).ToList(),
				FileSort.TimeBy => rawFileList.OrderByDescending(f => f.CreationTime).ToList(),
				_ => throw new RuntimeBinderException($"未知排序 [{fileSort}]")
			};

			if (files.Count == 0)
			{
				Log.Warning("在 VFS 输入目录 [{InputDir}] 下未找到任何支持的文件，支持格式：[{Extensions}]",
					inputDir, rawExtensions.JoinToString(", "));
				return 0;
			}

			// 2. 获取对应的 ICoder 实例
			var coder = CoderRegistry.GetCoder(format);

			// 3. 构建固定 VFS 缓存沙盒路径: <DataRootPath>/cache/<AppName>/tmp/<Uuid>
			var cacheRoot = Path.Combine(DataRootPath, "cache", AppInfo.AppName, "tmp", Uuid);

			if (VfsDirectoryExists(cacheRoot)) 
				VfsDeleteDirectory(cacheRoot, recursive: true);
			VfsCreateDirectory(cacheRoot);

			Log.Information("========== 批量处理启动 ==========");
			Log.Information("{0}: {1}", AppInfo.AppName, (typeof(Access).Assembly.GetName().Version?.ToString() ?? "None"));
			Log.Information("VFS 根目录:[{DataRootPath}]", DataRootPath);
			Log.Information("目标格式编解码器:[{CoderType}]", coder.GetType().Name);
			Log.Information("总计文件:[{Total}] 并发线程:[{Threads}]", files.Count, threads);
			Log.Information("输出格式:[{Format}] 保留目录结构:[{KeepStructure}] 删除源文件:[{DeleteSource}]",
				format, keepStructure, deleteSource);
			Log.Information("支持格式:[{Extensions}]", rawExtensions.JoinToString(", "));
			Log.Information("ImageMagick:[{MagickPath}]", magickPath);
			Log.Information("ExifTool:[{ExifToolPath}]", exifToolPath);
			Log.Information("输出目录:[{OutputDir}]", outputDir);
			Log.Information("缓存目录:[{CacheDir}]", cacheRoot);
			Log.Information("==================================");

			// 4. 预创建共享 Shell 实例
			var shell = new ProcessRunner { OutputHandler = ProcessRunner.IgnoreOutput, WaitTimeout = -1, AllowInfiniteWait = true };

			var processedCount = 0;
			var failCount = 0;
			var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threads <= 0 ? 1 : threads };

			// ================================================================================
			// 核心并发：自包含流式处理
			// ================================================================================
			await Parallel.ForEachAsync(files, parallelOptions, async (file, _) =>
			{
				var startTimestamp = TimestampKit.GetUnixTimestamp();
				var input = file.FullName;
				var fileNameWithoutExt = Path.GetFileNameWithoutExtension(input);

				// 计算相对目录层级
				var relativeDir = keepStructure
					? Path.GetDirectoryName(Path.GetRelativePath(inputDir, input)) ?? ""
					: "";

				// 临时缓存路径（沙盒隔离）
				var tempDir = Path.Combine(cacheRoot, relativeDir);
				if (!VfsDirectoryExists(tempDir)) 
					VfsCreateDirectory(tempDir);
				
				var tempOutputFile = Path.Combine(tempDir, $"{fileNameWithoutExt}.{format}");

				// 正式输出路径
				var finalOutputDir = Path.Combine(outputDir, relativeDir);
				var finalOutputFile = Path.Combine(finalOutputDir, $"{fileNameWithoutExt}.{format}");

				// 工具链链式执行（利用 ICoder 动态组装指令）
				var chainResult = await shell.CallChainAsync(
				[
					coder.BuildCommand(magickPath, input, tempOutputFile),
					BuildExifToolCommand(exifToolPath, input, tempOutputFile)
				], _).ConfigureAwait(false);

				var currentIndex = Interlocked.Increment(ref processedCount);

				if (chainResult == 0)
				{
					// 全部成功：交付与清理
					try
					{
						if (!VfsDirectoryExists(finalOutputDir)) 
							VfsCreateDirectory(finalOutputDir);

						if (VfsFileExists(tempOutputFile))
							VfsMoveFile(tempOutputFile, finalOutputFile, overwrite: true);

						if (deleteSource && VfsFileExists(input))
							VfsDeleteFile(input);

						if (pushImmich)
						{
							var exitCode = Program.Add(finalOutputFile);
						}

						Log.Information("[{Index}/{Total}] 处理完成 [{FileName}] 耗时 [{Duration}]",
							currentIndex, files.Count, Path.GetFileName(input),
							(TimestampKit.GetUnixTimestamp() - startTimestamp).ToReadableDuration());
					}
					catch (Exception ex)
					{
						Interlocked.Increment(ref failCount);
						Log.Error(ex, "[{Index}/{Total}] 交付失败 [{FileName}] 临时路径 [{TempPath}] 耗时 [{Duration}]",
							currentIndex, files.Count, Path.GetFileName(input), tempOutputFile,
							(TimestampKit.GetUnixTimestamp() - startTimestamp).ToReadableDuration());
					}
				}
				else
				{
					// 工具链失败：记录错误并清理临时文件
					Interlocked.Increment(ref failCount);
					Log.Error("[{Index}/{Total}] 工具链执行失败 [退出码:{ExitCode}] [{FileName}] 耗时 [{Duration}]",
						currentIndex, files.Count, chainResult, Path.GetFileName(input),
						(TimestampKit.GetUnixTimestamp() - startTimestamp).ToReadableDuration());

					try 
					{ 
						if (VfsFileExists(tempOutputFile)) 
							VfsDeleteFile(tempOutputFile); 
					}
					catch { /* 防御性忽略临时文件擦除异常 */ }
				}
			});

			// ================================================================================
			// 收尾：清理本次运行的缓存沙盒
			// ================================================================================
			try
			{
				if (VfsDirectoryExists(cacheRoot))
				{
					VfsDeleteDirectory(cacheRoot, recursive: true);
					Log.Debug("已清理 VFS 缓存沙盒 [{CacheDir}]", cacheRoot);
				}
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "清理 VFS 缓存沙盒失败 [{CacheDir}]", cacheRoot);
			}

			Log.Information("批量处理完成 | 总计:[{Total}] 失败:[{Failed}]", files.Count, failCount);

			return failCount > 0 ? 1 : 0;
		}

		#region VFS 基础路由与操作层

		private static string ResolveVfsPath(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) return DataRootPath;
			return Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(DataRootPath, path));
		}

		private static bool VfsDirectoryExists(string path) => Directory.Exists(ResolveVfsPath(path));

		private static bool VfsFileExists(string path) => File.Exists(ResolveVfsPath(path));

		private static void VfsCreateDirectory(string path) => Directory.CreateDirectory(ResolveVfsPath(path));

		private static void VfsDeleteDirectory(string path, bool recursive) => Directory.Delete(ResolveVfsPath(path), recursive);

		private static void VfsDeleteFile(string path) => File.Delete(ResolveVfsPath(path));

		private static void VfsMoveFile(string source, string dest, bool overwrite)
		{
			var fullSrc = ResolveVfsPath(source);
			var fullDst = ResolveVfsPath(dest);
			if (overwrite && File.Exists(fullDst))
			{
				File.Delete(fullDst);
			}
			File.Move(fullSrc, fullDst);
		}

		private static IEnumerable<string> VfsEnumerateFiles(string dir, string searchPattern, SearchOption option)
		{
			return Directory.GetFiles(ResolveVfsPath(dir), searchPattern, option);
		}

		#endregion

		/// <summary>
		/// 构建 ExifTool 元数据迁移命令
		/// </summary>
		private static List<string> BuildExifToolCommand(string exiftoolPath, string input, string output) =>
			[exiftoolPath, "-TagsFromFile", input, "-overwrite_original", output];
	}
}