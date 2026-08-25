using System.IO.Compression;

namespace NineTailedFox.Atomic.ProteusKV.Core
{
	public static class CompressionUtility
	{
		public static byte[] Compress(byte[] data, CompressionAlgorithm algorithm)
		{
			if (algorithm == CompressionAlgorithm.None || data.Length == 0) return data;
			using var ms = new MemoryStream();
			using (Stream compressor = algorithm switch
				   {
					   CompressionAlgorithm.GZip => new GZipStream(ms, CompressionLevel.Fastest),
					   CompressionAlgorithm.Brotli => new BrotliStream(ms, CompressionLevel.Fastest),
					   CompressionAlgorithm.Deflate => new DeflateStream(ms, CompressionLevel.Fastest),
					   _ => throw new NotSupportedException($"Unsupported algorithm: {algorithm}")
				   })
			{
				compressor.Write(data, 0, data.Length);
			}
			return ms.ToArray();
		}

		public static byte[] Decompress(byte[] data, CompressionAlgorithm algorithm)
		{
			if (algorithm == CompressionAlgorithm.None || data.Length == 0) return data;
			using var ms = new MemoryStream(data);
			using var output = new MemoryStream();
			using (Stream decompressor = algorithm switch
				   {
					   CompressionAlgorithm.GZip => new GZipStream(ms, CompressionMode.Decompress),
					   CompressionAlgorithm.Brotli => new BrotliStream(ms, CompressionMode.Decompress),
					   CompressionAlgorithm.Deflate => new DeflateStream(ms, CompressionMode.Decompress),
					   _ => throw new NotSupportedException($"Unsupported algorithm: {algorithm}")
				   })
			{
				decompressor.CopyTo(output);
			}
			return output.ToArray();
		}
	}
}