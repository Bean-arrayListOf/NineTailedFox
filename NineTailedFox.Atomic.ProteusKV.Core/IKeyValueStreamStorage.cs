namespace NineTailedFox.Atomic.ProteusKV.Core
{
	public interface IKeyValueStreamStorage : IKeyValueStorage
	{
		ValueTask PutStreamAsync(string db, string key, Stream sourceStream, ITransactionContext? tx = null, CancellationToken ct = default);
		ValueTask<Stream?> OpenReadStreamAsync(string db, string key, ITransactionContext? tx = null, CancellationToken ct = default);
	}
}