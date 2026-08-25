using Serilog.Core;
using Serilog.Events;

namespace NineTailedFox.Atomic.LoggerKit
{
	/// <summary>
	/// 缩写版 SourceContext 丰富器
	/// <para>
	/// 当 <c>SourceContext</c> 长度超过阈值时，将除末段（类名）外的所有命名空间段缩写为首字母。<br/>
	/// 例如：<c>MyApp.Services.UserService</c> → <c>M.S.UserService</c><br/>
	/// 结果写入新属性 <c>ShortSourceContext</c>。
	/// </para>
	/// </summary>
	public class AbbreviatedSourceContextEnricher : ILogEventEnricher
	{
		/// <summary>
		/// 输出属性名，默认 "ShortSourceContext"
		/// </summary>
		private readonly string _propertyName;

		/// <summary>
		/// 触发缩写的 SourceContext 最大长度阈值，超过此值才执行缩写
		/// </summary>
		private readonly int _maxLength;

		/// <summary>
		/// 创建缩写丰富器实例
		/// </summary>
		/// <param name="propertyName">输出属性名，默认 "ShortSourceContext"</param>
		/// <param name="maxLength">触发缩写的长度阈值，默认 30</param>
		public AbbreviatedSourceContextEnricher(string propertyName = "ShortSourceContext", int maxLength = 30)
		{
			_propertyName = propertyName;
			_maxLength = maxLength;
		}

		/// <inheritdoc />
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			if (!logEvent.Properties.TryGetValue("SourceContext", out var value) ||
				value is not ScalarValue { Value: string sourceContext })
			{
				return;
			}

			// 未超过阈值，直接使用原始值
			var processedContext = sourceContext.Length <= _maxLength
				? sourceContext
				: Abbreviate(sourceContext);

			logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(_propertyName, processedContext));
		}

		/// <summary>
		/// 将命名空间各段缩写为首字母，保留末段（类名）完整
		/// </summary>
		private static string Abbreviate(string sourceContext)
		{
			var parts = sourceContext.Split('.');
			if (parts.Length <= 1) return sourceContext;

			// 除末段外，其余各段仅保留首字母
			for (var i = 0; i < parts.Length - 1; i++)
			{
				if (parts[i].Length > 0)
				{
					parts[i] = parts[i][0].ToString();
				}
			}

			return string.Join(".", parts);
		}
	}
}
