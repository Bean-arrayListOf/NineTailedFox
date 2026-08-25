using System.Data.Common;

namespace NineTailedFox.Atomic.ProteusKV.Core
{
	public sealed class AdoTransactionContext : ITransactionContext
	{
		public DbTransaction Transaction { get; }
		private bool _completed;

		public AdoTransactionContext(DbTransaction transaction) => Transaction = transaction;

		public async ValueTask CommitAsync(CancellationToken ct = default)
		{
			if (_completed) return;
			await Transaction.CommitAsync(ct);
			_completed = true;
		}

		public async ValueTask RollbackAsync(CancellationToken ct = default)
		{
			if (_completed) return;
			await Transaction.RollbackAsync(ct);
			_completed = true;
		}

		public async ValueTask DisposeAsync()
		{
			if (!_completed)
			{
				try { await Transaction.RollbackAsync(); } catch { /* Ignore */ }
			}
			await Transaction.DisposeAsync();
		}
	}
}