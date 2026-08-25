namespace NineTailedFox.BatchImageFormatConversion.Application.Coder
{
	public static class CoderRegistry
	{
		private static readonly Dictionary<string, ICoder> Coders = new(StringComparer.OrdinalIgnoreCase)
		{
			["webp"] = new WebpCoder(),
			["avif"] = new AvifCoder(),
			["heif"] = new HeifCoder(),
			["heic"] = new HeifCoder(),
			["png"] = new PngCoder()
		};

		/// <summary>
		/// 获取指定格式的 Coder，若未预设则回退至 DefaultCoder
		/// </summary>
		public static ICoder GetCoder(string format)
		{
			var cleanFormat = format.TrimStart('.').ToLowerInvariant();
			return Coders.TryGetValue(cleanFormat, out var coder) 
				? coder 
				: new DefaultCoder(cleanFormat);
		}

		/// <summary>
		/// 允许动态注册或覆盖自定义 Coder
		/// </summary>
		public static void RegisterCoder(ICoder coder)
		{
			Coders[coder.Format.ToLowerInvariant()] = coder;
		}
	}
}