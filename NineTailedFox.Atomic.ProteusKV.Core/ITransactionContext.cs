namespace NineTailedFox.Atomic.ProteusKV.Core
{
	public interface ITransactionContext : IAsyncDisposable
	{
		ValueTask CommitAsync(CancellationToken ct = default);
		ValueTask RollbackAsync(CancellationToken ct = default);
	}
}