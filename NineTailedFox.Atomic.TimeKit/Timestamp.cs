using System.Runtime.CompilerServices;

namespace NineTailedFox.Atomic.TimeKit
{
	/// <summary>
	/// Unix 时间戳工具类，提供时间戳获取与可读时长格式化
	/// </summary>
	public static class TimestampKit
	{
		/// <summary>
		/// 获取当前 UTC 时间的毫秒级 Unix 时间戳
		/// </summary>
		/// <returns>自 1970-01-01T00:00:00Z 起的毫秒数</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetUnixTimestamp()
		{
			return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		}

		/// <summary>
		/// 获取当前 UTC 时间的秒级 Unix 时间戳
		/// </summary>
		/// <returns>自 1970-01-01T00:00:00Z 起的秒数</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetUnixTimestampSeconds()
		{
			return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		}

		/// <summary>
		/// 将毫秒数格式化为智能的可读时长字符串
		/// <para>示例：0 → "0ms"，5 → "5ms"，15200 → "15s 200ms"，3720000 → "1h 2m"</para>
		/// </summary>
		/// <param name="milliseconds">毫秒数（负值取绝对值处理）</param>
		/// <returns>格式化的时长字符串</returns>
		public static string ToReadableDuration(this long milliseconds)
		{
			// 负值取绝对值，避免 TimeSpan 各分量为负导致输出异常
			var ts = TimeSpan.FromMilliseconds(Math.Abs(milliseconds));

			// 零值直接返回
			if (ts == TimeSpan.Zero) return "0ms";

			var parts = new List<string>();

			// 超过 24 小时时显示天数
			if (ts.Days > 0) parts.Add($"{ts.Days}d");
			if (ts.Hours > 0) parts.Add($"{ts.Hours}h");
			if (ts.Minutes > 0) parts.Add($"{ts.Minutes}m");
			if (ts.Seconds > 0) parts.Add($"{ts.Seconds}s");

			// 毫秒数有值，或者总时长不到 1 秒（parts 为空）时，补上 ms
			if (ts.Milliseconds > 0 || parts.Count == 0)
			{
				parts.Add($"{ts.Milliseconds}ms");
			}

			return string.Join(" ", parts);
		}
	}
}
