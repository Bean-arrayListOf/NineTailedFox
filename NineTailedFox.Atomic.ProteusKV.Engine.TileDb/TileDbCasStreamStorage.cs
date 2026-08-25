// ============================================================================
// File: Engine.TileDb/TileDbCasStreamStorage.cs
// ============================================================================
using System.Security.Cryptography;
using Serilog;
using NineTailedFox.Atomic.ProteusKV.Core;

namespace NineTailedFox.Atomic.ProteusKV.Engine.TileDb;

public sealed class TileDbCasStreamStorage : BaseKeyValueEngine
{
    private readonly string _storageRootUri;
    private readonly int _chunkSize;

    private readonly Dictionary<(string Db, string Key), IndexEntry> _indexArray = new();
    private readonly Dictionary<string, ChunkEntry> _chunksArray = new();
    private readonly Dictionary<(string RootHash, int Seq), ManifestEntry> _manifestArray = new();
    private readonly Dictionary<string, (string Salt, string Hash)> _authArray = new();

    public sealed record IndexEntry(string RootHash, int CompAlgo, long TotalSize, int ChunkCount, long UpdatedAt);
    private sealed class ChunkEntry { public int RefCount { get; set; } public required byte[] Payload { get; init; } }
    public sealed record ManifestEntry(string ChunkHash, int ChunkSize);

    public TileDbCasStreamStorage(string storageRootUri = "tiledb_cas_storage", int chunkSize = 64 * 1024, ILogger? logger = null)
        : base(logger)
    {
        _storageRootUri = storageRootUri;
        _chunkSize = chunkSize;
    }

    public override ValueTask InitializeAsync(CancellationToken ct = default) => ValueTask.CompletedTask;

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
        var tileTx = (TileDbCasTxContext)tx;

        string? oldRootHash = null;
        if (tileTx.TryGetIndex(db, key, out var stagedIndex))
            oldRootHash = stagedIndex.RootHash;
        else if (_indexArray.TryGetValue((db, key), out var committedIndex))
            oldRootHash = committedIndex.RootHash;

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

        foreach (var chunk in chunkList)
        {
            tileTx.StageChunk(chunk.ChunkHash, chunk.CompressedPayload);
            tileTx.StageManifest(rootHashStr, chunk.Seq, chunk.ChunkHash, chunk.RawSize);
        }

        tileTx.StageIndex(db, key, new IndexEntry(
            rootHashStr, 
            (int)Compression, 
            totalRawSize, 
            chunkList.Count, 
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        ));

        if (!string.IsNullOrEmpty(oldRootHash) && oldRootHash != rootHashStr)
        {
            tileTx.StageDecrementRootHash(oldRootHash);
        }

        Logger.Debug("[ProteusKV-TileDB] Staged ({Db}, {Key}) Size={Size}, Chunks={Chunks}", db, key, totalRawSize, chunkList.Count);
    }

    public override ValueTask<Stream?> OpenReadStreamAsync(string db, string key, ITransactionContext? tx = null, CancellationToken ct = default)
    {
        EnsureAuthenticated();
        var tileTx = tx as TileDbCasTxContext;

        IndexEntry? index = null;
        if (tileTx != null && tileTx.TryGetIndex(db, key, out var stagedIndex))
            index = stagedIndex;
        else if (_indexArray.TryGetValue((db, key), out var committedIndex))
            index = committedIndex;

        if (index == null) return ValueTask.FromResult<Stream?>(null);

        var manifestList = new List<(int Seq, string ChunkHash, int Size)>();
        int seq = 0;
        while (true)
        {
            if (tileTx != null && tileTx.TryGetManifest(index.RootHash, seq, out var stagedManifest))
                manifestList.Add((seq, stagedManifest.ChunkHash, stagedManifest.ChunkSize));
            else if (_manifestArray.TryGetValue((index.RootHash, seq), out var committedManifest))
                manifestList.Add((seq, committedManifest.ChunkHash, committedManifest.ChunkSize));
            else
                break;
            seq++;
        }

        Stream stream = new TileDbChunkedReadOnlyStream(
            this,
            tileTx,
            manifestList,
            (CompressionAlgorithm)index.CompAlgo,
            index.TotalSize
        );

        return ValueTask.FromResult<Stream?>(stream);
    }

    internal byte[] LoadChunkPayload(string chunkHash, TileDbCasTxContext? tx)
    {
        if (tx != null && tx.TryGetChunkPayload(chunkHash, out var stagedPayload)) return stagedPayload;
        if (_chunksArray.TryGetValue(chunkHash, out var chunk)) return chunk.Payload;
        throw new KeyNotFoundException($"TileDB chunk not found: {chunkHash}");
    }

    protected override ValueTask<bool> DeleteCoreAsync(string db, string key, ITransactionContext? tx, CancellationToken ct)
    {
        var tileTx = tx as TileDbCasTxContext;
        if (tileTx != null)
        {
            if (_indexArray.TryGetValue((db, key), out var idx))
            {
                tileTx.StageDeleteIndex(db, key);
                tileTx.StageDecrementRootHash(idx.RootHash);
                return ValueTask.FromResult(true);
            }
            return ValueTask.FromResult(false);
        }

        if (_indexArray.Remove((db, key), out var indexEntry))
        {
            DecrementAndGcRootHash(indexEntry.RootHash);
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    private void DecrementAndGcRootHash(string rootHash)
    {
        int seq = 0;
        while (_manifestArray.Remove((rootHash, seq), out var manifest))
        {
            if (_chunksArray.TryGetValue(manifest.ChunkHash, out var chunk))
            {
                chunk.RefCount--;
                if (chunk.RefCount <= 0) _chunksArray.Remove(manifest.ChunkHash);
            }
            seq++;
        }
    }

    public override ValueTask<ITransactionContext> BeginTransactionAsync(CancellationToken ct = default)
    {
        return ValueTask.FromResult<ITransactionContext>(new TileDbCasTxContext(this));
    }

    protected override ValueTask PersistUserCredentialsAsync(string username, string salt, string hash, CancellationToken ct)
    {
        _authArray[username] = (salt, hash);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask<(string Salt, string Hash)?> FetchUserCredentialsAsync(string username, CancellationToken ct)
    {
        return ValueTask.FromResult(_authArray.TryGetValue(username, out var cred) ? cred : ((string, string)?)null);
    }

    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public sealed class TileDbCasTxContext : ITransactionContext
    {
        private readonly TileDbCasStreamStorage _engine;
        private readonly Dictionary<(string, string), IndexEntry?> _stagedIndices = new();
        private readonly Dictionary<string, byte[]> _stagedChunks = new();
        private readonly Dictionary<string, int> _chunkRefDeltas = new();
        private readonly Dictionary<(string, int), ManifestEntry> _stagedManifests = new();
        private readonly List<string> _rootsToDecrement = new();
        private bool _completed;

        public TileDbCasTxContext(TileDbCasStreamStorage engine) => _engine = engine;

        public void StageIndex(string db, string key, IndexEntry entry) => _stagedIndices[(db, key)] = entry;
        public void StageDeleteIndex(string db, string key) => _stagedIndices[(db, key)] = null;

        public bool TryGetIndex(string db, string key, out IndexEntry? entry)
        {
            if (_stagedIndices.TryGetValue((db, key), out entry)) return true;
            entry = null;
            return false;
        }

        public void StageChunk(string chunkHash, byte[] payload)
        {
            _stagedChunks[chunkHash] = payload;
            _chunkRefDeltas[chunkHash] = _chunkRefDeltas.GetValueOrDefault(chunkHash, 0) + 1;
        }

        public bool TryGetChunkPayload(string chunkHash, out byte[] payload) => _stagedChunks.TryGetValue(chunkHash, out payload!);

        public void StageManifest(string rootHash, int seq, string chunkHash, int size) =>
            _stagedManifests[(rootHash, seq)] = new ManifestEntry(chunkHash, size);

        public bool TryGetManifest(string rootHash, int seq, out ManifestEntry manifest) =>
            _stagedManifests.TryGetValue((rootHash, seq), out manifest!);

        public void StageDecrementRootHash(string rootHash) => _rootsToDecrement.Add(rootHash);

        public ValueTask CommitAsync(CancellationToken ct = default)
        {
            if (_completed) return ValueTask.CompletedTask;

            foreach (var (chunkHash, payload) in _stagedChunks)
            {
                if (!_engine._chunksArray.TryGetValue(chunkHash, out var chunk))
                {
                    _engine._chunksArray[chunkHash] = new ChunkEntry { Payload = payload, RefCount = 0 };
                }
            }
            foreach (var (chunkHash, delta) in _chunkRefDeltas)
            {
                if (_engine._chunksArray.TryGetValue(chunkHash, out var chunk))
                {
                    chunk.RefCount += delta;
                }
            }

            foreach (var (key, manifest) in _stagedManifests) _engine._manifestArray[key] = manifest;
            foreach (var (key, index) in _stagedIndices)
            {
                if (index == null) _engine._indexArray.Remove(key);
                else _engine._indexArray[key] = index;
            }

            foreach (var oldRoot in _rootsToDecrement) _engine.DecrementAndGcRootHash(oldRoot);

            _completed = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask RollbackAsync(CancellationToken ct = default)
        {
            _stagedIndices.Clear();
            _stagedChunks.Clear();
            _chunkRefDeltas.Clear();
            _stagedManifests.Clear();
            _rootsToDecrement.Clear();
            _completed = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_completed) RollbackAsync();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TileDbChunkedReadOnlyStream : Stream
    {
        private readonly TileDbCasStreamStorage _engine;
        private readonly TileDbCasTxContext? _tx;
        private readonly List<(int Seq, string ChunkHash, int Size)> _manifest;
        private readonly CompressionAlgorithm _algorithm;
        private readonly long _totalLength;

        private long _position;
        private int _currentChunkSeq = -1;
        private byte[]? _currentChunkBuffer;

        public TileDbChunkedReadOnlyStream(
            TileDbCasStreamStorage engine,
            TileDbCasTxContext? tx,
            List<(int Seq, string ChunkHash, int Size)> manifest,
            CompressionAlgorithm algo,
            long totalLength)
        {
            _engine = engine;
            _tx = tx;
            _manifest = manifest;
            _algorithm = algo;
            _totalLength = totalLength;
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
            var compressedBytes = _engine.LoadChunkPayload(targetChunk.ChunkHash, _tx);
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
    }
}