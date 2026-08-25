namespace NineTailedFox.BatchImageFormatConversion.Application.Coder
{
	public interface ICoder
	{
		/// <summary> 目标格式扩展名标识（不带点） </summary>
		string Format { get; }

		/// <summary>
		/// 构建该格式对应的 ImageMagick 命令行参数列表
		/// </summary>
		/// <param name="magickPath">ImageMagick 可执行文件路径</param>
		/// <param name="inputPath">输入文件路径</param>
		/// <param name="outputPath">输出文件路径</param>
		/// <returns>命令参数列表</returns>
		List<string> BuildCommand(string magickPath, string inputPath, string outputPath);
	}
}