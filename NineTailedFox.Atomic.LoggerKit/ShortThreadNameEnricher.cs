using Serilog.Core;
using Serilog.Events;

namespace NineTailedFox.Atomic.LoggerKit
{
	public class ShortThreadNameEnricher(int maxLength = 12) : ILogEventEnricher
	{
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			var processedName = "-"; // .NET 默认线程通常没有名字，用短横线占位

			// 读取由 Serilog.Enrichers.Thread 提供的原始 ThreadName
			if (logEvent.Properties.TryGetValue("ThreadName", out var value) && 
				value is ScalarValue scalar && scalar.Value is string threadName && !string.IsNullOrWhiteSpace(threadName))
			{
				processedName = threadName;

				// 如果名字超出最大长度，进行截断并加上 ...
				if (threadName.Length > maxLength)
				{
					processedName = threadName[..(maxLength - 3)] + "...";
				}
			}

			// 将处理后的短线程名存入新属性 ShortThreadName
			var property = propertyFactory.CreateProperty("ShortThreadName", processedName);
			logEvent.AddOrUpdateProperty(property);
		}
	
	}
}