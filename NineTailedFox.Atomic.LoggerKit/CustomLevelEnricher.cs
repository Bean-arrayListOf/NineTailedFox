using Serilog.Core;
using Serilog.Events;

namespace NineTailedFox.Atomic.LoggerKit
{
	public class CustomLevelEnricher : ILogEventEnricher
	{
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			// 根据不同级别，映射为你想要的精准 5 位字符字符串
			var customLevel = logEvent.Level switch
			{
				LogEventLevel.Verbose => "VERBO",
				LogEventLevel.Debug => "DEBUG",
				LogEventLevel.Information => "INFO ", // 4字+1空格
				LogEventLevel.Warning => "WARN ", // 4字+1空格
				LogEventLevel.Error => "ERROR", // 5字
				LogEventLevel.Fatal => "FATAL", // 5字
				_ => logEvent.Level.ToString()
			};

			// 将自定义属性注入到日志上下文中
			var levelProperty = propertyFactory.CreateProperty("CustomLevel", customLevel);
			logEvent.AddPropertyIfAbsent(levelProperty);
		}
	}
}