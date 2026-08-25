using Microsoft.Extensions.FileProviders;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace NineTailedFox.Atomic.LoggerKit
{
	/// <summary>
	/// Serilog 日志初始化工具类
	/// <para>
	/// 提供统一的日志管线配置入口，集成以下 Enricher：<br/>
	/// - <see cref="AbbreviatedSourceContextEnricher"/>：缩写命名空间（SourceContext → ShortSourceContext）<br/>
	/// - <see cref="CustomLevelEnricher"/>：自定义 5 字符级别标识（CustomLevel）<br/>
	/// - <see cref="ShortThreadNameEnricher"/>：截断线程名（ThreadName → ShortThreadName）
	/// </para>
	/// </summary>
	public static class LoggerKit
	{
		/// <summary>
		/// 默认日志输出模板
		/// <para>格式示例：<c>2025-06-28 14:30:00.123 [INFO ] M.S.UserService - User logged in</c></para>
		/// </summary>
		public const string OutputTemplate = "{@t:yyyy-MM-dd HH:mm:ss.fff} [{@l,-11:u}] {ShortSourceContext,-30} - {@m}\n{@x}";

		/// <summary>
		/// 基于 <see cref="OutputTemplate"/> 构建的表达式模板（Code 配色主题）
		/// </summary>
		public static readonly ExpressionTemplate Template =
			new(template: OutputTemplate, theme: TemplateTheme.Code);

		/// <summary>
		/// 为 LoggerConfiguration 接入完整的 Enricher 管线与控制台输出
		/// </summary>
		/// <param name="loggerConfiguration">Serilog 配置实例</param>
		/// <returns>同一配置实例（支持链式调用）</returns>
		public static LoggerConfiguration Output(this LoggerConfiguration loggerConfiguration)
		{
			return new LoggerBuilder(loggerConfiguration)
						 .UseEnrichWith(new AbbreviatedSourceContextEnricher())
						 .UseDefaultTemplate()
						 .GetLoggerConfig();
			
			// return loggerConfiguration
			// 	.Enrich.With(
			// 		new AbbreviatedSourceContextEnricher()
			// 	)
			// 	.WriteTo.Console(Template);
		}

		/// <summary>
		/// 初始化全局日志配置并设置 <see cref="Log.Logger"/>
		/// </summary>
		/// <param name="minimumLevel">最低日志级别，默认 Information</param>
		public static void InitLoggerConfig(LogEventLevel minimumLevel = LogEventLevel.Information)
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Is(minimumLevel)
				.Output()
				.CreateLogger();
		}
		
		public static void InitLoggerConfigIsSqlite(string sqlitePath,LogEventLevel minimumLevel = LogEventLevel.Information)
		{
			var parent = Directory.GetParent(sqlitePath);
			if (!parent!.Exists)
			{
				parent.Create();
			}
			
			Log.Logger = new LoggerConfiguration()
						 .MinimumLevel.Is(minimumLevel)
						 .Output()
						 .WriteTo.SQLite(sqlitePath)
						 .CreateLogger();
		}

		/// <summary>
		/// 安全关闭并刷新全局日志，释放底层 Sink 资源
		/// <para>建议在应用退出时调用（如 ApplicationStopping）</para>
		/// </summary>
		public static void ShutdownLogger()
		{
			Log.CloseAndFlush();
		}

		public static ILogger GetLogger<T>()
		{
			return Serilog.Log.ForContext<T>();
		}
	}
}
