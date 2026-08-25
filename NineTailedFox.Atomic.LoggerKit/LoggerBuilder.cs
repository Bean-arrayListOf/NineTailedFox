using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace NineTailedFox.Atomic.LoggerKit
{
	public class LoggerBuilder
	{
		private LoggerConfiguration LoggerConfig;

		public LoggerBuilder(LoggerConfiguration config)
		{
			LoggerConfig = config;
		}

		public LoggerBuilder()
		{
			LoggerConfig = new();
		}

		public LoggerBuilder UseDefaultTemplate()
		{
			LoggerConfig.WriteTo.Console(LoggerKit.Template);
			return this;
		}

		public LoggerBuilder UseLevel(LogEventLevel minimumLevel)
		{
			LoggerConfig.MinimumLevel.Is(minimumLevel);
			return this;
		}

		public LoggerBuilder UseEnrichWith(ILogEventEnricher lec)
		{
			LoggerConfig.Enrich.With(lec);
			return this;
		}

		public LoggerBuilder UseSQLite(string path)
		{
			LoggerConfig.WriteTo.SQLite(path);
			return this;
		}

		public Logger CreateConfig()
		{
			return LoggerConfig.CreateLogger();
		}

		public LoggerConfiguration GetLoggerConfig()
		{
			return LoggerConfig;
		}
	}
}