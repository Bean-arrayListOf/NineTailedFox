using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace NineTailedFox.BatchImageFormatConversion.Application.Boot
{
	/// <summary>
	/// 绿色沙盒数据目录动态加载器（四通道管线 + 缓存防重复）
	/// <para>
	/// 通道 1: AssemblyLoadContext.Resolving — 托管程序集解析（现代 .NET 主通道）<br/>
	/// 通道 2: AssemblyLoadContext.ResolvingUnmanagedDll — 非托管原生库解析<br/>
	/// 通道 3: AppDomain.AssemblyResolve — 全局旧版本程序集兜底<br/>
	/// 通道 4: AppDomain.ResourceResolve — 卫星资源程序集兜底
	/// </para>
	/// </summary>
	public static class AssemblyLoader
	{
		private static int _isBound;

		/// <summary>
		/// 已加载的托管程序集缓存，避免同一 DLL 被 LoadFromAssemblyPath 重复加载引发异常
		/// Key: 程序集全路径  Value: 已加载的 Assembly
		/// </summary>
		private static readonly ConcurrentDictionary<string, Assembly> LoadedAssemblies = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// 已加载的非托管库缓存，避免同一 native DLL 被重复 Load
		/// Key: 库文件全路径  Value: 已加载的句柄
		/// </summary>
		private static readonly ConcurrentDictionary<string, IntPtr> LoadedNatives = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// 一键打通 .NET 10 的全套 4 大核心加载通道（防重复订阅）
		/// </summary>
		public static void Register()
		{
			// 使用原子操作确保高并发或多模块加载时，事件只被订阅一次
			if (Interlocked.Exchange(ref _isBound, 1) == 1) return;

			// 通道 1 & 2：现代 .NET 核心通道（托管 + 非托管 Native）
			AssemblyLoadContext.Default.Resolving += ResolveManaged;
			AssemblyLoadContext.Default.ResolvingUnmanagedDll += ResolveUnmanaged;

			// 通道 3 & 4：全局最后防线（旧版本全局程序集兜底 + 清单资源兜底）
			AppDomain.CurrentDomain.AssemblyResolve += ResolveLegacyGlobal;
			AppDomain.CurrentDomain.ResourceResolve += ResolveResource;

#if DEBUG
			Console.WriteLine("[DataDirectoryLoader] 4-Channel Sandbox Pipeline Linked Successfully.");
#endif
		}

		// ================================================================================
		// 通道 1：托管程序集解析（AssemblyLoadContext.Resolving）
		// ================================================================================
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Assembly? ResolveManaged(AssemblyLoadContext context, AssemblyName assemblyName)
			=> ScanManagedDirectories(assemblyName);

		// ================================================================================
		// 通道 3：全局旧版本程序集兜底（AppDomain.AssemblyResolve）
		// ================================================================================
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Assembly? ResolveLegacyGlobal(object? sender, ResolveEventArgs args) => args.RequestingAssembly == null ? null : ScanManagedDirectories(new AssemblyName(args.Name));

		// ================================================================================
		// 通道 4：卫星资源程序集兜底（AppDomain.ResourceResolve）
		// ================================================================================

		private static Assembly? ResolveResource(object? sender, ResolveEventArgs args)
		{
			// 解析被请求的资源程序集名称（如 "MyApp.resources, Version=..., Culture=en-US"）
			var requestedName = new AssemblyName(args.Name);

			foreach (var dir in DataPaths.LibDirs)
			{
				// 优先按 CultureName 子目录查找（如 en-US/MyApp.resources.dll）
				var resourcePath = !string.IsNullOrEmpty(requestedName.CultureName)
					? Path.Combine(dir, requestedName.CultureName, $"{requestedName.Name}.dll")
					: Path.Combine(dir, $"{requestedName.Name}.dll");

				if (!File.Exists(resourcePath)) continue;
#if DEBUG
				Console.WriteLine("[DataDirectoryLoader:{0}]: Load [{1}]", nameof(ResolveResource), resourcePath);
#endif
				return LoadManagedAssembly(resourcePath);
			}
#if DEBUG
			Console.WriteLine("[DataDirectoryLoader:{0}]: Not [{1}]", nameof(ResolveResource), args.Name);
#endif
			return null;
		}

		// ================================================================================
		// 通道 2：非托管原生库解析（AssemblyLoadContext.ResolvingUnmanagedDll）
		// ================================================================================

		private static IntPtr ResolveUnmanaged(Assembly assembly, string unmanagedDllName)
		{
			foreach (var nativeDir in DataPaths.NativeDirs)
			{
				// 优先尝试原始名称（可能已包含扩展名和路径）
				var dllPath = Path.Combine(nativeDir, unmanagedDllName);
				if (File.Exists(dllPath))
				{
#if DEBUG
					Console.WriteLine("[DataDirectoryLoader:{0}]: Load [{1}]", nameof(ResolveUnmanaged), dllPath);
#endif
					return LoadNativeLibrary(dllPath);
				}

				// 若无扩展名，则按当前操作系统自动补全
				if (Path.HasExtension(unmanagedDllName)) continue;
				var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll" :
					RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? ".so" : ".dylib";

				var dllPathWithExt = Path.Combine(nativeDir, unmanagedDllName + ext);
				if (!File.Exists(dllPathWithExt)) continue;
#if DEBUG
				Console.WriteLine("[DataDirectoryLoader:{0}]: Load [{1}]", nameof(ResolveUnmanaged),
					dllPathWithExt);
#endif
				return LoadNativeLibrary(dllPathWithExt);
			}
#if DEBUG
			Console.WriteLine("[DataDirectoryLoader:{0}]: Not [{1}]", nameof(ResolveUnmanaged), unmanagedDllName);
#endif
			return IntPtr.Zero;
		}

		// ================================================================================
		// 公共扫描逻辑 & 缓存加载
		// ================================================================================

		/// <summary>
		/// 遍历所有 Lib 目录，查找并加载匹配的托管程序集
		/// </summary>
		private static Assembly? ScanManagedDirectories(AssemblyName assemblyName)
		{
			foreach (var libDir in DataPaths.LibDirs)
			{
				// 带 CultureName 的资源程序集，优先从子目录查找
				var assemblyPath = !string.IsNullOrEmpty(assemblyName.CultureName)
					? Path.Combine(libDir, assemblyName.CultureName, $"{assemblyName.Name}.dll")
					: Path.Combine(libDir, $"{assemblyName.Name}.dll");

				if (!File.Exists(assemblyPath)) continue;
#if DEBUG
				Console.WriteLine("[DataDirectoryLoader:{0}]: Load [{1}]", nameof(ScanManagedDirectories),
					assemblyPath);
#endif
				return LoadManagedAssembly(assemblyPath);
			}
#if DEBUG
			Console.WriteLine("[DataDirectoryLoader:{0}]: Not [{1}]", nameof(ScanManagedDirectories), assemblyName);
#endif
			return null;
		}

		/// <summary>
		/// 带缓存的托管程序集加载，防止同一路径被重复 LoadFromAssemblyPath
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Assembly LoadManagedAssembly(string assemblyPath)
		{
			return LoadedAssemblies.GetOrAdd(assemblyPath, path =>
				AssemblyLoadContext.Default.LoadFromAssemblyPath(path));
		}

		/// <summary>
		/// 带缓存的非托管库加载，防止同一路径被重复 NativeLibrary.Load
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint LoadNativeLibrary(string dllPath) => LoadedNatives.GetOrAdd(dllPath, NativeLibrary.Load);
	}
}
