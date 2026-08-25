namespace NineTailedFox.BatchImageFormatConversion.Application.Coder
{
	public class PngCoder : ICoder
	{
		public string Format => "png";

		public List<string> BuildCommand(string magickPath, string inputPath, string outputPath)
		{
			return [magickPath, inputPath, outputPath];
		}
	}
}