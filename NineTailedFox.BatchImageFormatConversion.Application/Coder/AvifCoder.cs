namespace NineTailedFox.BatchImageFormatConversion.Application.Coder
{
	public class AvifCoder : ICoder
	{
		public string Format => "avif";

		public List<string> BuildCommand(string magickPath, string inputPath, string outputPath)
		{
			return [magickPath, inputPath, "-define", "heic:lossless=true", outputPath];
		}
	}
}