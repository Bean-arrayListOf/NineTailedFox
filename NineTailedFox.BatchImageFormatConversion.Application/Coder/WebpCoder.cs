namespace NineTailedFox.BatchImageFormatConversion.Application.Coder
{
	public class WebpCoder : ICoder
	{
		public string Format => "webp";

		public List<string> BuildCommand(string magickPath, string inputPath, string outputPath)
		{
			return [magickPath, inputPath, "-define", "webp:lossless=true", outputPath];
		}
	}
}