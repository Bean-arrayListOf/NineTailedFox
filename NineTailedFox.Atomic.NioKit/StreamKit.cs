namespace NineTailedFox.Atomic.NioKit
{
	public static class StreamKit
	{
		public static async Task<string> ReadToEndAsync(this Stream stream)
		{
			using var text = new StreamReader(stream);
			return await text.ReadToEndAsync();
		}

		public static string ReadToEnd(this Stream stream)
		{
			return stream.ReadToEndAsync().GetAwaiter().GetResult();
		}
	}
}