namespace NineTailedFox.Mods.ModLoader
{
	public interface IModProcessRunner
	{
		public int Call(List<string> commandTokens);
		public List<int> CallMany(List<List<string>> commands, bool failFast = false);
		public Task<int> CallAsync(List<string> commandTokens, CancellationToken cancellationToken = default);
		public Task<List<int>> CallManyAsync(
			List<List<string>> commands,
			bool failFast = false,
			CancellationToken cancellationToken = default);

		public int CallChain(List<List<string>> commands);

		public Task<int> CallChainAsync(
			List<List<string>> commands,
			CancellationToken cancellationToken = default);
	}
}