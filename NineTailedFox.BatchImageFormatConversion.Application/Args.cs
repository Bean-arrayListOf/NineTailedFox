using System.ComponentModel.DataAnnotations;
using CommandLine;

namespace NineTailedFox.BatchImageFormatConversion.Application
{
	public class Args
	{
		[Option('i', "input", Required = true, HelpText = "输入文件夹路径")]
		public string? InputFile { get; set; }

		[Option('o', "output", Required = true, HelpText = "输出文件夹路径")]
		public string? OutputFile { get; set; }

		[Option('f', "format", Required = true, HelpText = "输出图片格式")]
		public string? OutputFormat { get; set; }

		[Option('m', "magick-path", Required = false, Default = null, HelpText = "ImageMagick 路径")]
		public string? MagickPath { get; set; }

		[Option('e', "exiftool-path", Required = false, Default = null, HelpText = "ExifTool 路径")]
		public string? ExifToolPath { get; set; }

		[Option('s', "supported-extensions", Required = false, Default = null,HelpText = "允许批处理的图片扩展名(多个用空格分隔)")]
		public IEnumerable<string>? SupportedExtensions { get; set; }

		[Option('d', "delete-source", Required = false, Default = false,
			HelpText = "转换成功后是否删除物理源文件。警告：此操作不可逆，请确保数据已备份！")]
		public bool DeleteSource { get; set; }

		// 🚀 新增参数 1：保持目录结构
		[Option('k', "keep-structure", Required = false, Default = false,
			HelpText = "是否保持原有的子目录层级结构。如果为 false，所有文件将平铺输出到目标根目录。")]
		public bool KeepStructure { get; set; }

		// 🚀 新增参数 2：多线程并发
		[Option('t', "threads", Required = false, Default = 1, HelpText = "最大并发线程数。利用多核 CPU 加速处理（建议设为核心数，如 4 或 8）。")]
		public int Threads { get; set; }

		[Option('c', "cache-path", Required = false, Default = null,
			HelpText = "自定义临时缓存文件夹路径（选填）。如果不指定，系统将自动使用 OS 默认的临时文件夹（如 /tmp 或 AppData/Local/Temp）。")]
		public string? CachePath { get; set; }

		[Option("file-sort", Required = false, Default = FileSort.NameBy, HelpText = "自定义排序,[Name,NameBy,Time,TimeBy]")]
		public FileSort FSort { get; set; }
		
		[Option("push-immich", Required = false, Default = false, HelpText = "是否上传immich")]
		public bool PushImmich { get; set; }
		
		[Option("immich-url", Required = false, Default = false, HelpText = "immich URL")]
		public bool ImmichUrl { get; set; }
		[Option("immich-key", Required = false, Default = false, HelpText = "immich Key")]
		public bool ImmichKey { get; set; }
	}
}