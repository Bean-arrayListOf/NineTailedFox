namespace NineTailedFox.Atomic.ProteusKV.Core
{
	public interface IKeyValueStorage : IAsyncDisposable
	{
		ValueTask InitializeAsync(CancellationToken ct = default);
		ValueTask RegisterUserAsync(string username, string password, CancellationToken ct = default);
		ValueTask AuthenticateAsync(string username, string password, CancellationToken ct = default);

		ValueTask PutAsync(string db, string key, byte[] value, ITransactionContext? tx = null, CancellationToken ct = default);
		ValueTask PutBatchAsync(string db, IReadOnlyList<KeyValuePair<string, byte[]>> entries, ITransactionContext? tx = null, CancellationToken ct = default);
		ValueTask<byte[]?> GetAsync(string db, string key, ITransactionContext? tx = null, CancellationToken ct = default);
		ValueTask<bool> DeleteAsync(string db, string key, ITransactionContext? tx = null, CancellationToken ct = default);

		ValueTask<ITransactionContext> BeginTransactionAsync(CancellationToken ct = default);
		ValueTask ExecuteInTransactionAsync(Func<ITransactionContext, ValueTask> action, CancellationToken ct = default);
	}
}