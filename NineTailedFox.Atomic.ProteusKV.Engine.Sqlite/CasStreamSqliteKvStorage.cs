// ============================================================================
// File: Engine.Sqlite/CasStreamSqliteKvStorage.cs
// ============================================================================
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Serilog;
using NineTailedFox.Atomic.ProteusKV.Core;

namespace NineTailedFox.Atomic.ProteusKV.Engine.Sqlite
{
	public sealed class CasStreamSqliteKvStorage : BaseKeyValueEngine
	{
		private readonly string _connectionString;
		private readonly int _chunkSize;
		private SqliteConnection? _connection;

		public CasStreamSqliteKvStorage(string connectionString = "Data Source=kv_cas.db", int chunkSize = 64 * 1024, ILogger? logger = null)
			: base(logger)
		{
			_connectionString = connectionString;
			_chunkSize = chunkSize;
		}

		public override async ValueTask InitializeAsync(CancellationToken ct = default)
		{
			_connection = new SqliteConnection(_connectionString);
			await _connection.OpenAsync(ct);

			await using (var pragma = _connection.CreateCommand())
			{
				pragma.CommandText = "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON; PRAGMA synchronous = NORMAL;";
				await pragma.ExecuteNonQueryAsync(ct);
			}

			const string ddl = """
							   CREATE TABLE IF NOT EXISTS sys_auth (
							       username   TEXT PRIMARY KEY,
							       salt       TEXT NOT NULL,
							       hash       TEXT NOT NULL
							   );
							   CREATE TABLE IF NOT EXISTS sys_index (
							       v_db        TEXT NOT NULL,
							       v_key       TEXT NOT NULL,
							       root_hash   TEXT NOT NULL,
							       comp_algo   INTEGER NOT NULL,
							       total_size  INTEGER NOT NULL,
							       chunk_count INTEGER NOT NULL,
							       updated_at  INTEGER NOT NULL,
							       PRIMARY KEY (v_db, v_key)
							   );
							   CREATE INDEX IF NOT EXISTS idx_sys_index_db ON sys_index (v_db);
							   CREATE TABLE IF NOT EXISTS sys_chunks (
							       chunk_hash  TEXT PRIMARY KEY,
							       ref_count   INTEGER NOT NULL,
							       payload     BLOB NOT NULL
							   );
							   CREATE TABLE IF NOT EXISTS sys_manifest (
							       root_hash   TEXT NOT NULL,
							       seq         INTEGER NOT NULL,
							       chunk_hash  TEXT NOT NULL,
							       chunk_size  INTEGER NOT NULL,
							       PRIMARY KEY (root_hash, seq)
							   );
							   """;
			await using var cmd = _connection.CreateCommand();
			cmd.CommandText = ddl;
			await cmd.ExecuteNonQueryAsync(ct);
		}

		public override async ValueTask PutStreamAsync(string db, string key, Stream sourceStream, ITransactionContext? tx = null, CancellationToken ct = default)
		{
			EnsureAuthenticated();
			if (tx == null)
			{
				await ExecuteInTransactionAsync(async autoTx => await PutStreamCoreAsync(db, key, sourceStream, autoTx, ct), ct);
			}
			else
			{
				await PutStreamCoreAsync(db, key, sourceStream, tx, ct);
			}
		}

		private async ValueTask PutStreamCoreAsync(string db, string key, Stream sourceStream, ITransactionContext tx, CancellationToken ct)
		{
			var activeTx = ((AdoTransactionContext)tx).Transaction as SqliteTransaction;
			var conn = activeTx!.Connection as SqliteConnection;

			string? oldRootHash = null;
			const string selectOld = "SELECT root_hash FROM sys_index WHERE v_db = @db AND v_key = @k;";
			await using (var cmd = conn!.CreateCommand())
			{
				cmd.Transaction = activeTx;
				cmd.CommandText = selectOld;
				cmd.Parameters.AddWithValue("@db", db);
				cmd.Parameters.AddWithValue("@k", key);
				oldRootHash = (string?)await cmd.ExecuteScalarAsync(ct);
			}

			using var overallSha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
			var buffer = new byte[_chunkSize];
			var chunkList = new List<(string ChunkHash, int Seq, int RawSize, byte[] CompressedPayload)>();

			int bytesRead;
			int seq = 0;
			long totalRawSize = 0;

			while ((bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, _chunkSize), ct)) > 0)
			{
				totalRawSize += bytesRead;
				overallSha256.AppendData(buffer, 0, bytesRead);

				var chunkHashBytes = SHA256.HashData(buffer.AsSpan(0, bytesRead));
				var chunkHashStr = Convert.ToHexString(chunkHashBytes);

				var rawSlice = buffer.AsSpan(0, bytesRead).ToArray();
				var compressedPayload = CompressionUtility.Compress(rawSlice, Compression);

				chunkList.Add((chunkHashStr, seq++, bytesRead, compressedPayload));
			}

			var rootHashStr = Convert.ToHexString(overallSha256.GetHashAndReset());

			const string upsertChunkSql = """
										  INSERT INTO sys_chunks (chunk_hash, ref_count, payload)
										  VALUES (@hash, 1, @payload)
										  ON CONFLICT(chunk_hash) DO UPDATE SET ref_count = ref_count + 1;
										  """;
			const string insertManifestSql = """
											 INSERT OR REPLACE INTO sys_manifest (root_hash, seq, chunk_hash, chunk_size)
											 VALUES (@root, @seq, @chunk, @size);
											 """;

			foreach (var chunk in chunkList)
			{
				await using (var cmd = conn.CreateCommand())
				{
					cmd.Transaction = activeTx;
					cmd.CommandText = upsertChunkSql;
					cmd.Parameters.AddWithValue("@hash", chunk.ChunkHash);
					cmd.Parameters.AddWithValue("@payload", chunk.CompressedPayload);
					await cmd.ExecuteNonQueryAsync(ct);
				}

				await using (var cmd = conn.CreateCommand())
				{
					cmd.Transaction = activeTx;
					cmd.CommandText = insertManifestSql;
					cmd.Parameters.AddWithValue("@root", rootHashStr);
					cmd.Parameters.AddWithValue("@seq", chunk.Seq);
					cmd.Parameters.AddWithValue("@chunk", chunk.ChunkHash);
					cmd.Parameters.AddWithValue("@size", chunk.RawSize);
					await cmd.ExecuteNonQueryAsync(ct);
				}
			}

			const string upsertIndexSql = """
										  INSERT INTO sys_index (v_db, v_key, root_hash, comp_algo, total_size, chunk_count, updated_at)
										  VALUES (@db, @k, @root, @algo, @size, @cnt, @time)
										  ON CONFLICT(v_db, v_key) DO UPDATE SET
										      root_hash   = excluded.root_hash,
										      comp_algo   = excluded.comp_algo,
										      total_size  = excluded.total_size,
										      chunk_count = excluded.chunk_count,
										      updated_at  = excluded.updated_at;
										  """;
			await using (var cmd = conn.CreateCommand())
			{
				cmd.Transaction = activeTx;
				cmd.CommandText = upsertIndexSql;
				cmd.Parameters.AddWithValue("@db", db);
				cmd.Parameters.AddWithValue("@k", key);
				cmd.Parameters.AddWithValue("@root", rootHashStr);
				cmd.Parameters.AddWithValue("@algo", (int)Compression);
				cmd.Parameters.AddWithValue("@size", totalRawSize);
				cmd.Parameters.AddWithValue("@cnt", chunkList.Count);
				cmd.Parameters.AddWithValue("@time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
				await cmd.ExecuteNonQueryAsync(ct);
			}

			if (!string.IsNullOrEmpty(oldRootHash) && oldRootHash != rootHashStr)
			{
				await DecrementRootHashReferencesAsync(oldRootHash, activeTx, conn, ct);
			}

			Logger.Debug("[ProteusKV-SQLite] Written ({Db}, {Key}) Size={Size}, Chunks={Chunks}, RootHash={Root}",
				db, key, totalRawSize, chunkList.Count, rootHashStr);
		}

		public override async ValueTask<Stream?> OpenReadStreamAsync(string db, string key, ITransactionContext? tx = null, CancellationToken ct = default)
		{
			EnsureAuthenticated();
			var activeTx = (tx as AdoTransactionContext)?.Transaction as SqliteTransaction;
			var conn = activeTx?.Connection as SqliteConnection ?? _connection!;

			const string indexSql = "SELECT root_hash, comp_algo, total_size FROM sys_index WHERE v_db = @db AND v_key = @k;";
			string? rootHash;
			int compAlgo;
			long totalSize;

			await using (var cmd = conn.CreateCommand())
			{
				cmd.Transaction = activeTx;
				cmd.CommandText = indexSql;
				cmd.Parameters.AddWithValue("@db", db);
				cmd.Parameters.AddWithValue("@k", key);
				await using var reader = await cmd.ExecuteReaderAsync(ct);
				if (!await reader.ReadAsync(ct)) return null;

				rootHash = reader.GetString(0);
				compAlgo = reader.GetInt32(1);
				totalSize = reader.GetInt64(2);
			}

			return new SqliteCasChunkedReadOnlyStream(_connectionString, rootHash, (CompressionAlgorithm)compAlgo, totalSize);
		}

		private static async ValueTask DecrementRootHashReferencesAsync(string rootHash, SqliteTransaction tx, SqliteConnection conn, CancellationToken ct)
		{
			const string decrSql = """
								   UPDATE sys_chunks
								   SET ref_count = ref_count - 1
								   WHERE chunk_hash IN (SELECT chunk_hash FROM sys_manifest WHERE root_hash = @root);
								   """;
			await using (var cmd = conn.CreateCommand())
			{
				cmd.Transaction = tx;
				cmd.CommandText = decrSql;
				cmd.Parameters.AddWithValue("@root", rootHash);
				await cmd.ExecuteNonQueryAsync(ct);
			}

			const string gcSql = "DELETE FROM sys_chunks WHERE ref_count <= 0;";
			await using (var cmd = conn.CreateCommand())
			{
				cmd.Transaction = tx;
				cmd.CommandText = gcSql;
				await cmd.ExecuteNonQueryAsync(ct);
			}

			const string cleanManifestSql = "DELETE FROM sys_manifest WHERE root_hash = @root;";
			await using (var cmd = conn.CreateCommand())
			{
				cmd.Transaction = tx;
				cmd.CommandText = cleanManifestSql;
				cmd.Parameters.AddWithValue("@root", rootHash);
				await cmd.ExecuteNonQueryAsync(ct);
			}
		}

		protected override async ValueTask<bool> DeleteCoreAsync(string db, string key, ITransactionContext? tx, CancellationToken ct)
		{
			var activeTx = (tx as AdoTransactionContext)?.Transaction as SqliteTransaction;
			var conn = activeTx?.Connection as SqliteConnection ?? _connection!;

			const string selectSql = "SELECT root_hash FROM sys_index WHERE v_db = @db AND v_key = @k;";
			string? rootHash;
			await using (var cmd = conn.CreateCommand())
			{
				cmd.Transaction = activeTx;
				cmd.CommandText = selectSql;
				cmd.Parameters.AddWithValue("@db", db);
				cmd.Parameters.AddWithValue("@k", key);
				rootHash = (string?)await cmd.ExecuteScalarAsync(ct);
			}

			if (string.IsNullOrEmpty(rootHash)) return false;

			const string delIndexSql = "DELETE FROM sys_index WHERE v_db = @db AND v_key = @k;";
			await using (var cmd = conn.CreateCommand())
			{
				cmd.Transaction = activeTx;
				cmd.CommandText = delIndexSql;
				cmd.Parameters.AddWithValue("@db", db);
				cmd.Parameters.AddWithValue("@k", key);
				await cmd.ExecuteNonQueryAsync(ct);
			}

			await DecrementRootHashReferencesAsync(rootHash, activeTx!, conn, ct);
			return true;
		}

		public override async ValueTask<ITransactionContext> BeginTransactionAsync(CancellationToken ct = default)
		{
			var tx = await _connection!.BeginTransactionAsync(ct);
			return new AdoTransactionContext(tx);
		}

		protected override async ValueTask PersistUserCredentialsAsync(string username, string salt, string hash, CancellationToken ct)
		{
			const string sql = "INSERT OR REPLACE INTO sys_auth (username, salt, hash) VALUES (@u, @s, @h);";
			await using var cmd = _connection!.CreateCommand();
			cmd.CommandText = sql;
			cmd.Parameters.AddWithValue("@u", username);
			cmd.Parameters.AddWithValue("@s", salt);
			cmd.Parameters.AddWithValue("@h", hash);
			await cmd.ExecuteNonQueryAsync(ct);
		}

		protected override async ValueTask<(string Salt, string Hash)?> FetchUserCredentialsAsync(string username, CancellationToken ct)
		{
			const string sql = "SELECT salt, hash FROM sys_auth WHERE username = @u;";
			await using var cmd = _connection!.CreateCommand();
			cmd.CommandText = sql;
			cmd.Parameters.AddWithValue("@u", username);
			await using var reader = await cmd.ExecuteReaderAsync(ct);
			if (await reader.ReadAsync(ct)) return (reader.GetString(0), reader.GetString(1));
			return null;
		}

		public override async ValueTask DisposeAsync()
		{
			if (_connection != null) await _connection.DisposeAsync();
		}

		private sealed class SqliteCasChunkedReadOnlyStream : Stream
		{
			private readonly string _connectionString;
			private readonly string _rootHash;
			private readonly CompressionAlgorithm _algorithm;
			private readonly long _totalLength;
			private readonly List<(int Seq, string ChunkHash, int Size)> _manifest = new();

			private SqliteConnection? _conn;
			private long _position;
			private int _currentChunkSeq = -1;
			private byte[]? _currentChunkBuffer;

			public SqliteCasChunkedReadOnlyStream(string connStr, string rootHash, CompressionAlgorithm algo, long totalLength)
			{
				_connectionString = connStr;
				_rootHash = rootHash;
				_algorithm = algo;
				_totalLength = totalLength;
				LoadManifest();
			}

			private void LoadManifest()
			{
				_conn = new SqliteConnection(_connectionString);
				_conn.Open();

				const string sql = "SELECT seq, chunk_hash, chunk_size FROM sys_manifest WHERE root_hash = @root ORDER BY seq ASC;";
				using var cmd = _conn.CreateCommand();
				cmd.CommandText = sql;
				cmd.Parameters.AddWithValue("@root", _rootHash);
				using var reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					_manifest.Add((reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (_position >= _totalLength) return 0;
				int totalRead = 0;
				while (count > 0 && _position < _totalLength)
				{
					var (chunkIdx, chunkOffset) = LocateChunk(_position);
					EnsureChunkLoaded(chunkIdx);

					int availableInChunk = _currentChunkBuffer!.Length - chunkOffset;
					int toRead = Math.Min(count, availableInChunk);

					Array.Copy(_currentChunkBuffer, chunkOffset, buffer, offset, toRead);
					_position += toRead;
					offset += toRead;
					count -= toRead;
					totalRead += toRead;
				}
				return totalRead;
			}

			private (int ChunkIndex, int ChunkOffset) LocateChunk(long absolutePosition)
			{
				long accumulated = 0;
				for (int i = 0; i < _manifest.Count; i++)
				{
					if (absolutePosition < accumulated + _manifest[i].Size)
						return (i, (int)(absolutePosition - accumulated));
					accumulated += _manifest[i].Size;
				}
				throw new ArgumentOutOfRangeException(nameof(absolutePosition));
			}

			private void EnsureChunkLoaded(int chunkIndex)
			{
				if (_currentChunkSeq == chunkIndex && _currentChunkBuffer != null) return;
				var targetChunk = _manifest[chunkIndex];
				const string sql = "SELECT payload FROM sys_chunks WHERE chunk_hash = @h;";
				using var cmd = _conn!.CreateCommand();
				cmd.CommandText = sql;
				cmd.Parameters.AddWithValue("@h", targetChunk.ChunkHash);

				var compressedBytes = (byte[])cmd.ExecuteScalar()!;
				_currentChunkBuffer = CompressionUtility.Decompress(compressedBytes, _algorithm);
				_currentChunkSeq = chunkIndex;
			}

			public override bool CanRead => true;
			public override bool CanSeek => true;
			public override bool CanWrite => false;
			public override long Length => _totalLength;
			public override long Position
			{
				get => _position;
				set => Seek(value, SeekOrigin.Begin);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				var target = origin switch
				{
					SeekOrigin.Begin => offset,
					SeekOrigin.Current => _position + offset,
					SeekOrigin.End => _totalLength + offset,
					_ => throw new ArgumentOutOfRangeException(nameof(origin))
				};
				if (target < 0 || target > _totalLength) throw new ArgumentOutOfRangeException(nameof(offset));
				_position = target;
				return _position;
			}

			public override void Flush() { }
			public override void SetLength(long value) => throw new NotSupportedException();
			public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					_conn?.Dispose();
					_currentChunkBuffer = null;
				}
				base.Dispose(disposing);
			}
		}
	}
}