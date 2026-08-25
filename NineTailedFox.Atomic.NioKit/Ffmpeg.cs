using FFmpeg.AutoGen;
using FFMpegCore;

namespace NineTailedFox.Atomic.NioKit
{
	public static class Ffmpeg
	{
		public static void Run()
		{
			ConvertToAvifAsync("/Users/arraylistof/Pictures/IMG_2097.JPG", "/Users/arraylistof/Pictures/IMG_2097.avif")
				.Wait();
		}

		public static async Task ConvertToAvifAsync(string inputPath, string outputPath, int crf = 28, int preset = 6)
		{
			await FFMpegArguments
				  .FromFileInput(inputPath)
				  .OutputToFile(outputPath, overwrite: true,
					  options => options.WithCustomArgument("-c:v libsvtav1").WithCustomArgument("-pix_fmt yuv420p")
										.WithCustomArgument($"-crf {crf}").WithCustomArgument($"-preset {preset}")
										.WithCustomArgument("-frames:v 1")
				  ).ProcessAsynchronously();
		}
	}
}