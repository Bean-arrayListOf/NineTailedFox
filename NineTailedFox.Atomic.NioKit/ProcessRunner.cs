using System.Collections;
using System.Diagnostics;

namespace NineTailedFox.Atomic.NioKit
{
	/// <summary>
	/// 跨平台外部命令行（Shell）轻量级执行器
	/// <para>
	/// 支持同步/异步执行、自定义输出处理器、多命令串行、等待超时控制、可执行文件 PATH 查找。<br/>
	/// 输出处理器可通过实例属性 <see cref="OutputHandler"/> / <see cref="ErrorHandler"/> 自定义，
	/// 也可使用预置的 <see cref="IgnoreOutput"/>、<see cref="DefaultOutput"/>、<see cref="DefaultError"/>。
	/// </para>
	/// </summary>
	public class ProcessRunner
	{
		// ================================================================================
		// 预置事件处理器（全局静态只读，避免重复分配）
		// ================================================================================

		/// <summary>
		/// 哑处理器：静默模式，丢弃所有输出（适用于不关心命令输出的场景）
		/// <para>用法：<c>shell.OutputHandler = Shell.IgnoreOutput;</c></para>
		/// </summary>
		public static readonly DataReceivedEventHandler IgnoreOutput = (_, _) => { };

		/// <summary> 进程成功退出（退出码 0） </summary>
		public const int ExitSuccess = 0;

		/// <summary> 默认标准输出处理器：实时行写入 <see cref="Console.Out"/> </summary>
		public static readonly DataReceivedEventHandler DefaultOutput = (_, args) =>
		{
			// BeginOutputReadLine 传递的数据已剥离换行符，必须用 WriteLine 保持原样换行
			if (args.Data != null) Console.WriteLine(args.Data);
		};

		/// <summary> 默认错误输出处理器：实时行写入 <see cref="Console.Error"/> </summary>
		public static readonly DataReceivedEventHandler DefaultError = (_, args) =>
		{
			if (args.Data != null) Console.Error.WriteLine(args.Data);
		};

		// ================================================================================
		// 实例属性
		// ================================================================================

		/// <summary> 自定义标准输出处理器，为 null 时使用 <see cref="DefaultOutput"/> </summary>
		public DataReceivedEventHandler? OutputHandler { get; set; }

		/// <summary> 自定义错误输出处理器，为 null 时使用 <see cref="DefaultError"/> </summary>
		public DataReceivedEventHandler? ErrorHandler { get; set; }

		/// <summary> 工作目录，为 null 时使用当前进程活动路径（<see cref="Environment.CurrentDirectory"/>） </summary>
		public string? WorkingDirectory { get; set; }

		/// <summary>
		/// 进程等待超时（毫秒），-1 表示无限等待
		/// <para>默认值为 -1（无限等待），需配合 <see cref="AllowInfiniteWait"/> 使用</para>
		/// </summary>
		public int WaitTimeout { get; set; } = -1;

		/// <summary>
		/// 是否允许无限等待（安全开关）
		/// <para>
		/// 当 <see cref="WaitTimeout"/> 为 -1 时，必须将此设为 true 才允许执行，
		/// 否则抛出 <see cref="InvalidOperationException"/>。默认 false。
		/// </para>
		/// </summary>
		public bool AllowInfiniteWait { get; set; }

		// ================================================================================
		// 同步执行通道
		// ================================================================================

		/// <summary>
		/// 【同步】执行单条命令，阻塞等待至进程退出
		/// </summary>
		/// <param name="commandTokens">命令及参数列表，如 <c>["ping", "127.0.0.1", "-n", "4"]</c></param>
		/// <returns>进程退出码（通常 0 代表成功）</returns>
		/// <exception cref="InvalidOperationException">WaitTimeout 为 -1 但 AllowInfiniteWait 未开启</exception>
		/// <exception cref="TimeoutException">等待超时</exception>
		public int Call(List<string> commandTokens)
		{
			using var process = CreateProcess(commandTokens, out var outHandler, out var errHandler);
			try
			{
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				WaitForProcessExit(process);

				return process.ExitCode;
			}
			finally
			{
				process.OutputDataReceived -= outHandler;
				process.ErrorDataReceived -= errHandler;
			}
		}

		/// <summary>
		/// 【同步】按顺序串行执行多条命令
		/// </summary>
		/// <param name="commands">多条命令的参数列表集合</param>
		/// <param name="failFast">为 true 时，某条命令非零退出则立即中止后续命令</param>
		/// <returns>各命令的退出码列表</returns>
		public List<int> CallMany(List<List<string>> commands, bool failFast = false)
		{
			ArgumentNullException.ThrowIfNull(commands);

			var exitCodes = new List<int>(commands.Count);
			foreach (var code in commands.Select(Call))
			{
				exitCodes.Add(code);
				if (failFast && code != 0) break;
			}

			return exitCodes;
		}

		// ================================================================================
		// 异步执行通道
		// ================================================================================

		/// <summary>
		/// 【异步】执行单条命令，支持取消
		/// </summary>
		/// <param name="commandTokens">命令及参数列表</param>
		/// <param name="cancellationToken">取消令牌，取消时会尝试终止子进程</param>
		/// <returns>进程退出码</returns>
		/// <exception cref="InvalidOperationException">WaitTimeout 为 -1 但 AllowInfiniteWait 未开启</exception>
		/// <exception cref="TimeoutException">等待超时</exception>
		public async Task<int> CallAsync(List<string> commandTokens, CancellationToken cancellationToken = default)
		{
			using var process = CreateProcess(commandTokens, out var outHandler, out var errHandler);
			try
			{
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				var pi = process;

				// 注册取消回调：取消时尝试终止子进程树
				await using var registration = cancellationToken.Register(() =>
				{
					try
					{
						if (!pi.HasExited) pi.Kill(entireProcessTree: true);
					}
					catch
					{
						/* 进程可能已退出，忽略 */
					}
				});

				await WaitForProcessExitAsync(process, cancellationToken);

				return process.ExitCode;
			}
			finally
			{
				process.OutputDataReceived -= outHandler;
				process.ErrorDataReceived -= errHandler;
			}
		}

		/// <summary>
		/// 【异步】按顺序串行执行多条命令，支持取消
		/// </summary>
		/// <param name="commands">多条命令的参数列表集合</param>
		/// <param name="failFast">为 true 时，某条命令非零退出则立即中止后续命令</param>
		/// <param name="cancellationToken">取消令牌</param>
		/// <returns>各命令的退出码列表</returns>
		public async Task<List<int>> CallManyAsync(
			List<List<string>> commands,
			bool failFast = false,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(commands);

			var exitCodes = new List<int>(commands.Count);
			foreach (var command in commands)
			{
				var code = await CallAsync(command, cancellationToken).ConfigureAwait(false);
				exitCodes.Add(code);
				if (failFast && code != 0) break;
			}

			return exitCodes;
		}

		// ================================================================================
		// 内部核心（DRY：统一构建 Process 实例）
		// ================================================================================

		/// <summary>
		/// 统一构建并配置 <see cref="Process"/> 实例，绑定输出事件处理器
		/// </summary>
		private Process CreateProcess(List<string> commandTokens,
									  out DataReceivedEventHandler outHandler,
									  out DataReceivedEventHandler errHandler)
		{
			if (commandTokens is not { Count: > 0 })
			{
				throw new ArgumentException("执行命令参数列表不能为空。", nameof(commandTokens));
			}

			var psi = new ProcessStartInfo(commandTokens[0])
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = WorkingDirectory ?? Environment.CurrentDirectory,
			};
			
			

			// 安全填充参数（使用 ArgumentList 自动处理特殊字符转义）
			for (var i = 1; i < commandTokens.Count; i++)
			{
				psi.ArgumentList.Add(commandTokens[i]);
			}

			var process = new Process { StartInfo = psi };

			// 用户自定义优先，否则降级为默认处理器
			outHandler = OutputHandler ?? DefaultOutput;
			errHandler = ErrorHandler ?? DefaultError;

			process.OutputDataReceived += outHandler;
			process.ErrorDataReceived += errHandler;

			return process;
		}

		// ================================================================================
		// 等待机制（超时校验 + 安全开关）
		// ================================================================================

		/// <summary>
		/// 【同步】带超时校验的进程等待
		/// </summary>
		/// <exception cref="InvalidOperationException">WaitTimeout 为 -1 但 AllowInfiniteWait 未开启</exception>
		/// <exception cref="TimeoutException">等待超时</exception>
		private void WaitForProcessExit(Process process)
		{
			ValidateWaitTimeout();

			if (WaitTimeout == -1)
			{
				process.WaitForExit();
				return;
			}

			if (!process.WaitForExit(WaitTimeout))
			{
				throw new TimeoutException(
					$"进程 [{process.StartInfo.FileName}] 在 {WaitTimeout}ms 内未退出，已超时终止。");
			}
		}

		/// <summary>
		/// 【异步】带超时校验的进程等待
		/// </summary>
		/// <exception cref="InvalidOperationException">WaitTimeout 为 -1 但 AllowInfiniteWait 未开启</exception>
		/// <exception cref="TimeoutException">等待超时</exception>
		private async Task WaitForProcessExitAsync(Process process, CancellationToken cancellationToken)
		{
			ValidateWaitTimeout();

			if (WaitTimeout == -1)
			{
				await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
				return;
			}

			// 用 Task.WhenAny 实现异步超时，避免阻塞线程
			var exitTask = process.WaitForExitAsync(cancellationToken);
			var timeoutTask = Task.Delay(WaitTimeout, cancellationToken);

			var completedTask = await Task.WhenAny(exitTask, timeoutTask).ConfigureAwait(false);

			if (completedTask == timeoutTask)
			{
				throw new TimeoutException(
					$"进程 [{process.StartInfo.FileName}] 在 {WaitTimeout}ms 内未退出，已超时终止。");
			}

			await exitTask.ConfigureAwait(false);
		}

		/// <summary>
		/// 校验 WaitTimeout 合法性与无限等待安全开关
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">WaitTimeout 小于 -1</exception>
		/// <exception cref="InvalidOperationException">WaitTimeout 为 -1 但 AllowInfiniteWait 未开启</exception>
		private void ValidateWaitTimeout()
		{
			switch (WaitTimeout)
			{
				case < -1:
					throw new ArgumentOutOfRangeException(nameof(WaitTimeout),
						"WaitTimeout 必须 >= -1（-1 表示无限等待，>= 0 表示有限超时毫秒数）。");
				case -1 when !AllowInfiniteWait:
					throw new InvalidOperationException(
						"无限等待被拒绝：WaitTimeout 为 -1，但 AllowInfiniteWait 未设为 true。" +
						"请设置 AllowInfiniteWait = true 以显式允许无限等待，或指定一个有限的 WaitTimeout（毫秒）。");
			}
		}

		// ================================================================================
		// 静态工具方法
		// ================================================================================

		/// <summary>
		/// 在系统 PATH 环境变量中查找可执行文件的完整路径
		/// <para>
		/// 查找顺序：当前目录 → PATH 各路径 → Windows 下自动追加 PATHEXT 后缀尝试
		/// </para>
		/// </summary>
		/// <param name="fileName">要查找的文件名（如 "dotnet"、"git"、"git.exe"）</param>
		/// <returns>文件的完整绝对路径；未找到返回 null</returns>
		public static string? FindExecutableInPath(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName)) return null;

			// 1. 若传入的本身是有效路径且文件存在，直接返回完整路径
			if (File.Exists(fileName)) return System.IO.Path.GetFullPath(fileName);

			// 2. 读取 PATH 环境变量
			var pathEnvVar = Environment.GetEnvironmentVariable("PATH");
			if (string.IsNullOrWhiteSpace(pathEnvVar)) return null;

			// 3. 按操作系统分隔符拆分 PATH
			var searchPaths = pathEnvVar.Split(System.IO.Path.PathSeparator);

			// 4. Windows 专属：获取 PATHEXT 支持的可执行后缀列表
			var windowsExtensions = GetWindowsExecutableExtensions(fileName);

			foreach (var path in searchPaths)
			{
				// 去除路径两端可能存在的引号或空格
				var cleanPath = path.Trim('"', ' ');
				if (string.IsNullOrWhiteSpace(cleanPath)) continue;

				try
				{
					var fullPath = System.IO.Path.Combine(cleanPath, fileName);

					// 检查原名文件是否存在
					if (File.Exists(fullPath)) return fullPath;

					// Windows 下无后缀文件自动尝试 .exe/.cmd/.bat 等后缀
					if (windowsExtensions != null)
					{
						foreach (var ext in windowsExtensions)
						{
							var fullPathWithExt = fullPath + ext;
							if (File.Exists(fullPathWithExt)) return fullPathWithExt;
						}
					}
				}
				catch (Exception)
				{
					// 忽略非法路径字符异常，继续检查下一个
				}
			}

			return null;
		}

		/// <summary>
		/// 获取 Windows 可执行文件后缀列表（非 Windows 返回 null）
		/// </summary>
		private static string[]? GetWindowsExecutableExtensions(string fileName)
		{
			if (!OperatingSystem.IsWindows() || System.IO.Path.HasExtension(fileName)) return null;

			// 优先从 PATHEXT 环境变量读取（如 ".COM;.EXE;.BAT;.CMD;.VBS;..."）
			var pathExt = Environment.GetEnvironmentVariable("PATHEXT");
			return !string.IsNullOrWhiteSpace(pathExt) ? pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) :
				// PATHEXT 不可用时使用常见后缀兜底
				[".exe", ".cmd", ".bat"];
		}

		// ... existing code ...

		/// <summary>
		/// 【同步】链式执行多条命令，任一命令退出码非零时立即短路返回
		/// </summary>
		/// <param name="commands">多条命令的参数列表集合，按顺序串行执行</param>
		/// <returns>
		/// 全部成功时返回 <c>0</c>；
		/// 中途失败时返回首个非零退出码
		/// </returns>
		public int CallChain(List<List<string>> commands)
		{
			ArgumentNullException.ThrowIfNull(commands);

			return commands.Select(Call).FirstOrDefault(code => code != 0);
		}

		/// <summary>
		/// 【异步】链式执行多条命令，任一命令退出码非零时立即短路返回
		/// </summary>
		/// <param name="commands">多条命令的参数列表集合，按顺序串行执行</param>
		/// <param name="cancellationToken">取消令牌</param>
		/// <returns>
		/// 全部成功时返回 <c>0</c>；
		/// 中途失败时返回首个非零退出码
		/// </returns>
		public async Task<int> CallChainAsync(
			List<List<string>> commands,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(commands);

			foreach (var command in commands)
			{
				var code = await CallAsync(command, cancellationToken).ConfigureAwait(false);
				if (code != 0) return code;
			}

			return 0;
		}
	}
}