namespace NineTailedFox.BatchImageFormatConversion.Application.Coder
{
	public class HeifCoder : ICoder
	{
		public string Format => "heif";

		public List<string> BuildCommand(string magickPath, string inputPath, string outputPath)
		{
			return [magickPath, inputPath, "-define", "heic:lossless=true", outputPath];
		}
	}
}