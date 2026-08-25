namespace NineTailedFox.BatchImageFormatConversion.Application.Boot
{
	/// <summary>
	/// 绿色沙盒数据目录配置
	/// <para>
	/// 管理托管程序集（Lib）和非托管原生库（Native）的搜索目录列表。<br/>
	/// 目录来源优先级：约定目录 → 环境变量追加目录。
	/// </para>
	/// </summary>
	public static class DataPaths
	{
		/// <summary>
		/// 数据目录名称，格式为 "{AppName}_Data"
		/// </summary>
		public static readonly string DataDirectoryName = $"{Kit.AppName}_Data";

		/// <summary>
		/// 托管程序集搜索目录列表（只读）
		/// </summary>
		public static IReadOnlyCollection<string> LibDirs { get; }

		/// <summary>
		/// 非托管原生库搜索目录列表（只读）
		/// </summary>
		public static IReadOnlyCollection<string> NativeDirs { get; }

		static DataPaths()
		{
			LibDirs = BuildDirList(GetDefaultLibDirs(), $"{Kit.AppName}_LIB");
			NativeDirs = BuildDirList(GetDefaultNativeDirs(), $"{Kit.AppName}_NATIVE");

#if DEBUG
			Console.WriteLine("DataDirectory Name: [{0}]", DataDirectoryName);
			Console.WriteLine("[{0}]: [{1}]", nameof(LibDirs), string.Join(", ", LibDirs));
			Console.WriteLine("[{0}]: [{1}]", nameof(NativeDirs), string.Join(", ", NativeDirs));
#endif
		}

		/// <summary>
		/// 默认托管程序集目录（dll/lib/library/plugin/runtime 等约定子目录）
		/// </summary>
		private static List<string> GetDefaultLibDirs()
		{
			return
			[
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "dll"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "dlls"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "lib"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "libs"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "library"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "librarys"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "plugin"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "plugins"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "runtime"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "runtimes")
			];
		}

		/// <summary>
		/// 默认非托管原生库目录（native/natives 等约定子目录）
		/// </summary>
		private static List<string> GetDefaultNativeDirs()
		{
			return
			[
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "native"),
				Path.Combine(Kit.BaseDirectory, DataDirectoryName, "natives")
			];
		}

		/// <summary>
		/// 构建目录列表：约定目录 + 环境变量追加目录（仅保留实际存在的目录）
		/// </summary>
		/// <param name="defaultDirs">默认约定目录列表</param>
		/// <param name="envVarName">环境变量名，多个路径用 <see cref="Path.PathSeparator"/> 分隔</param>
		private static List<string> BuildDirList(List<string> defaultDirs, string envVarName)
		{
			var dirs = new List<string>(defaultDirs);

			var envValue = Environment.GetEnvironmentVariable(envVarName);

#if DEBUG
			Console.WriteLine("{0}: [{1}]", envVarName, envValue ?? "is null");
#endif

			if (envValue == null) return dirs;

			var extraDirs = envValue
				.Split(Path.PathSeparator)
				.Select(Path.GetFullPath)
				.Where(Directory.Exists);

			dirs.AddRange(extraDirs);
			return dirs;
		}
	}
}
