// ============================================================================
// File: Core/BaseKeyValueEngine.cs (核心抽象基类)
// ============================================================================
using System.Security.Authentication;
using System.Security.Cryptography;
using Serilog;

namespace NineTailedFox.Atomic.ProteusKV.Core
{
	public abstract class BaseKeyValueEngine : IKeyValueStreamStorage
	{
		protected readonly ILogger Logger;
		private string? _currentAuthenticatedUser;

		public CompressionAlgorithm Compression { get; set; } = CompressionAlgorithm.None;

		protected BaseKeyValueEngine(ILogger? logger = null)
		{
			Logger = (logger ?? Log.Logger).ForContext(GetType());
		}

		public abstract ValueTask InitializeAsync(CancellationToken ct = default);

		protected void EnsureAuthenticated()
		{
			if (string.IsNullOrEmpty(_currentAuthenticatedUser))
			{
				Logger.Warning("[ProteusKV-Auth] Unauthorized access attempt rejected");
				throw new AuthenticationException("Operation rejected: Session is not authenticated.");
			}
		}

		public async ValueTask RegisterUserAsync(string username, string password, CancellationToken ct = default)
		{
			Logger.Information("[ProteusKV-Auth] Registering user {Username}", username);
			var salt = RandomNumberGenerator.GetBytes(16);
			var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
			await PersistUserCredentialsAsync(username, Convert.ToBase64String(salt), Convert.ToBase64String(hash), ct);
		}

		public async ValueTask AuthenticateAsync(string username, string password, CancellationToken ct = default)
		{
			var credentials = await FetchUserCredentialsAsync(username, ct);
			if (credentials == null)
			{
				Logger.Warning("[ProteusKV-Auth] User not found: {Username}", username);
				throw new AuthenticationException("Invalid username or password.");
			}

			var salt = Convert.FromBase64String(credentials.Value.Salt);
			var expectedHash = Convert.FromBase64String(credentials.Value.Hash);
			var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

			if (!CryptographicOperations.FixedTimeEquals(expectedHash, computedHash))
			{
				Logger.Warning("[ProteusKV-Auth] Password mismatch for user: {Username}", username);
				throw new AuthenticationException("Invalid username or password.");
			}

			_currentAuthenticatedUser = username;
			Logger.Information("[ProteusKV-Auth] User {Username} authenticated successfully", username);
		}

		public async ValueTask PutAsync(string db, string key, byte[] value, ITransactionContext? tx = null, CancellationToken ct = default)
		{
			EnsureAuthenticated();
			using var ms = new MemoryStream(value);
			await PutStreamAsync(db, key, ms, tx, ct);
		}

		public async ValueTask PutBatchAsync(string db, IReadOnlyList<KeyValuePair<string, byte[]>> entries, ITransactionContext? tx = null, CancellationToken ct = default)
		{
			EnsureAuthenticated();
			if (tx != null)
			{
				foreach (var item in entries)
				{
					using var ms = new MemoryStream(item.Value);
					await PutStreamAsync(db, item.Key, ms, tx, ct);
				}
			}
			else
			{
				await ExecuteInTransactionAsync(async autoTx =>
				{
					foreach (var item in entries)
					{
						using var ms = new MemoryStream(item.Value);
						await PutStreamAsync(db, item.Key, ms, autoTx, ct);
					}
				}, ct);
			}
		}

		public async ValueTask<byte[]?> GetAsync(string db, string key, ITransactionContext? tx = null, CancellationToken ct = default)
		{
			EnsureAuthenticated();
			await using var stream = await OpenReadStreamAsync(db, key, tx, ct);
			if (stream == null) return null;

			using var ms = new MemoryStream();
			await stream.CopyToAsync(ms, ct);
			return ms.ToArray();
		}

		public async ValueTask<bool> DeleteAsync(string db, string key, ITransactionContext? tx = null, CancellationToken ct = default)
		{
			EnsureAuthenticated();
			return await DeleteCoreAsync(db, key, tx, ct);
		}

		public async ValueTask ExecuteInTransactionAsync(Func<ITransactionContext, ValueTask> action, CancellationToken ct = default)
		{
			await using var tx = await BeginTransactionAsync(ct);
			Logger.Verbose("[ProteusKV-Tx] Transaction started");
			try
			{
				await action(tx);
				await tx.CommitAsync(ct);
				Logger.Verbose("[ProteusKV-Tx] Transaction committed");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "[ProteusKV-Tx] Transaction failed, rolled back");
				await tx.RollbackAsync(ct);
				throw;
			}
		}

		public abstract ValueTask PutStreamAsync(string db, string key, Stream sourceStream, ITransactionContext? tx = null, CancellationToken ct = default);
		public abstract ValueTask<Stream?> OpenReadStreamAsync(string db, string key, ITransactionContext? tx = null, CancellationToken ct = default);
		public abstract ValueTask<ITransactionContext> BeginTransactionAsync(CancellationToken ct = default);
		public abstract ValueTask DisposeAsync();

		protected abstract ValueTask PersistUserCredentialsAsync(string username, string salt, string hash, CancellationToken ct);
		protected abstract ValueTask<(string Salt, string Hash)?> FetchUserCredentialsAsync(string username, CancellationToken ct);
		protected abstract ValueTask<bool> DeleteCoreAsync(string db, string key, ITransactionContext? tx, CancellationToken ct);
	}
}