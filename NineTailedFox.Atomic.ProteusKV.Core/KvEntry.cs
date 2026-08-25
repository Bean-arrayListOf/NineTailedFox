namespace NineTailedFox.Atomic.ProteusKV.Core
{
	public sealed class KvEntry
	{
		public required string Db { get; init; }
		public required string Key { get; init; }
		public required byte[] Value { get; init; }
	}
}