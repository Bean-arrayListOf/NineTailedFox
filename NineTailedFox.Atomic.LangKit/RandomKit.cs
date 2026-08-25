using System.Security.Cryptography;
using System.Text;

namespace NineTailedFox.Atomic.LangKit
{
	public static class RandomKit
	{
		private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
		private static readonly char[] DefaultChars = ['A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z','0','1','2','3','4','5','6','7','8','9'];
		
		public static byte[] NextU8(uint bufferSize = 1024)
		{
			if (bufferSize == uint.MinValue)
			{
				throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size cannot be negative.");
			}
			
			var bytes = new byte[bufferSize];
			
			Rng.GetBytes(bytes);

			return bytes;
		}

		public static int NextI32(int min = int.MinValue,int max = int.MaxValue)
		{
			if (min > max)
				throw new ArgumentOutOfRangeException(nameof(min), "min 不能大于 max");

			// 使用安全的随机数生成器
			var buffer = new byte[4];
			Rng.GetBytes(buffer);
        
			// 将字节转换为 uint 以避免负数带来的模运算分布不均问题
			var randomUint = BitConverter.ToUInt32(buffer, 0);
        
			// 计算范围大小（注意处理溢出）
			var range = max - min + 1;
        
			// 使用取模运算（在实际密码学场景中，建议加上拒绝采样 Rejection Sampling 以消除模偏差）
			var result = (int)(randomUint % (uint)range + (uint)min);
			return result;
		}

		public static byte[] GenerateRandomBytes(int randomByteLength = 32)
		{
			Span<byte> randomByte = stackalloc byte[randomByteLength];
			RandomNumberGenerator.Fill(randomByte);
			return randomByte.ToArray();
		}

		public static byte[] GenerateRandomSha(HashMode mode = HashMode.Sha256,int randomByteLength = 32)
		{
			Span<byte> randomByte = stackalloc byte[randomByteLength];
			RandomNumberGenerator.Fill(randomByte);

			var bufferSize = mode switch
			{
				HashMode.Sha1 => SHA1.HashSizeInBytes,
				HashMode.Sha256 => SHA256.HashSizeInBytes,
				HashMode.Sha384 => SHA384.HashSizeInBytes,
				HashMode.Sha3_256 => SHA3_256.HashSizeInBytes,
				HashMode.Sha3_384 => SHA3_384.HashSizeInBytes,
				HashMode.Sha3_512 => SHA3_512.HashSizeInBytes,
				HashMode.Sha512 => SHA512.HashSizeInBytes,
				_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
			};

			Span<byte> hashBytes = stackalloc byte[bufferSize];
			SHA1.HashData(randomByte, hashBytes);

			return hashBytes.ToArray();
		}

		public static string GenerateRandomString(int length = 32)
		{
			var sb = new StringBuilder();
			
			for (var i = 0; i < length; i++)
			{
				sb.Append(DefaultChars[RandomNumberGenerator.GetInt32(0, DefaultChars.Length)]);
			}

			return sb.ToString();

		}

		public static bool GenerateRandomBool()
		{
			var randomSize = RandomNumberGenerator.GetInt32(0,2);
			return randomSize == 0;
		}
		
	}
}