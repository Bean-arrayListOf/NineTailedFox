namespace NineTailedFox.Canary
{
	public static class Kit
	{
		public static FileInfo GetFileInfo(this string path) => new(path);
	}
}