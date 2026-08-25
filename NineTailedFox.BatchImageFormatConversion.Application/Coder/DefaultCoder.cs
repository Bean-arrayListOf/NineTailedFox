namespace NineTailedFox.BatchImageFormatConversion.Application.Coder
{
	public class DefaultCoder : ICoder
	{
		public string Format { get; }

		public DefaultCoder(string format)
		{
			Format = format.ToLowerInvariant();
		}

		public List<string> BuildCommand(string magickPath, string inputPath, string outputPath)
		{
			return [magickPath, inputPath, outputPath];
		}
	}
}