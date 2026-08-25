namespace NineTailedFox.BatchImageFormatConversion.Application.Key
{
	public interface IKey
	{
		public bool Verify(string key);
		public IReadOnlyList<string> Get();
	}
}