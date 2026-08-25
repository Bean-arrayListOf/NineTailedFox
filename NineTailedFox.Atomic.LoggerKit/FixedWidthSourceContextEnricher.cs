using Serilog.Core;
using Serilog.Events;

namespace NineTailedFox.Atomic.LoggerKit
{
	public class FixedWidthSourceContextEnricher : ILogEventEnricher
	{
		private const int TargetWidth = 30;

		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			var formattedContext = new string(' ', TargetWidth); // 默认全空格

			if (logEvent.Properties.TryGetValue("SourceContext", out var value) &&
				value is ScalarValue { Value: string sourceContext })
			{
				// 方式 A（推荐）：截断头部，保留尾部（例如：...p.Services.OrderService）
				formattedContext = sourceContext.Length > TargetWidth ? sourceContext[^TargetWidth..] :
					// 方式 B：截断尾部，保留头部（例如：Microsoft.AspNetCore.Mvc.Facto...）
					// formattedContext = sourceContext.Substring(0, TargetWidth);
					// 左对齐，不足右侧补空格
					sourceContext.PadRight(TargetWidth);
			}

			// 向日志事件中注入新属性
			logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SourceContext30", formattedContext));
		}
	}
}