namespace NineTailedFox.Atomic.NioKit
{
	public class Path
	{
		private readonly string _rootPath;
		
		public Path(params string[] more)
		{
			_rootPath = System.IO.Path.Combine(more);
		}
		
		public static Path Get(params string[] more)
		{
			return new Path(more);
		}

		public FileStream NewInputStream()
		{
			return File.OpenRead(_rootPath);
		}

		public FileStream NewOutputStream()
		{
			return File.OpenWrite(_rootPath);
		}

		public Path CreateFile()
		{
			using var _ = File.Create(_rootPath);
			return this;
		}

		public Path CreateDirectory()
		{
			Directory.CreateDirectory(_rootPath);
			return this;
		}
		
		
	}
}