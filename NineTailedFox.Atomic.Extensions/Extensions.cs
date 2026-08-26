using System.Runtime.CompilerServices;
using System.Text;

namespace NineTailedFox.Atomic.Extensions
{
	/// <summary>
	/// 语言级扩展方法集合（语法糖 / 常用快捷操作）
	/// </summary>
	public static class Extensions
	{
		private static readonly char[] HexChars = "0123456789ABCDEF".ToCharArray();
		
		/// <summary>
		/// 程序集默认编码（UTF-8，无 BOM）
		/// </summary>
		public static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
		
		/// <summary>
		/// 将字符串序列拼接为单个字符串，以指定分隔符分隔
		/// </summary>
		/// <param name="values">待拼接的字符串序列</param>
		/// <param name="separator">分隔符，默认为 ", "</param>
		/// <returns>拼接后的字符串；若序列为空则返回空字符串</returns>
		/// <exception cref="ArgumentNullException"><paramref name="values"/> 为 null</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string JoinToString(this IEnumerable<string?> values, string separator = ", ")
		{
			ArgumentNullException.ThrowIfNull(values);
			return string.Join(separator, values);
		}

		// ================================================================================
		// 字符串 ↔ byte[]（UTF-8 序列化 / 反序列化）
		// ================================================================================

		/// <summary>
		/// 将字符串按默认编码（UTF-8）序列化为 byte 数组
		/// </summary>
		/// <param name="text">源字符串</param>
		/// <returns>编码后的 byte 数组；若为 null 或空则返回空数组</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] DecodeFromBytes(this string? text) => string.IsNullOrEmpty(text) ? [] : DefaultEncoding.GetBytes(text);

		/// <summary>
		/// 将 byte 数组按默认编码（UTF-8）反序列化为字符串
		/// </summary>
		/// <param name="bytes">源字节数组</param>
		/// <returns>解码后的字符串；若为 null 或空则返回空字符串</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string EncodeToString(this byte[]? bytes) => bytes == null || bytes.Length == 0 ? string.Empty : DefaultEncoding.GetString(bytes);

		public static string EncodeToBase64(this string plainText)
		{
			var bytes = DefaultEncoding.GetBytes(plainText);
			return Convert.ToBase64String(bytes);
		}

		public static string DecodeFromBase64(this string base64Text)
		{
			var bytes = Convert.FromBase64String(base64Text);
			return DefaultEncoding.GetString(bytes);
		}
		
		
		public static string EncodeToHex(this byte[] bytes, char replace = ':')
		{
			if (bytes.Length == 0) return string.Empty;

			// 计算最终字符串长度：每个字节占2个字符，(字节数 - 1) 个分隔符
			var len = bytes.Length * 2 + (bytes.Length - 1);
			var result = new char[len];

			for (var i = 0; i < bytes.Length; i++)
			{
				var b = bytes[i];
				var index = i * 3; // 每个字节占用3个位置（2个Hex字符 + 1个分隔符）

				// 高4位
				result[index] = HexChars[b >> 4];
				// 低4位
				result[index + 1] = HexChars[b & 0x0F];

				// 如果不是最后一个字节，添加分隔符
				if (i < bytes.Length - 1)
				{
					result[index + 2] = replace;
				}
			}

			return new string(result);
		}

		public static bool IsEmpty<T>(this T[] args)
		{
			if (args.Length == 0)
			{
				return true;
			}

			return args.Length != 0;
		}

		public static bool IsNotEmpty<T>(this T[] args)
		{
			return !args.IsEmpty();
		}
		
		// 这就是 C# 版的 apply 神器！
		public static T Apply<T>(this T obj, Action<T> action)
		{
			action(obj);
			return obj;
		}

		public static void Out(this object? arg)
		{
			if (arg!=null)
			{
				Console.WriteLine(arg);
			}
		}

		public static void OutLine(this object? arg)
		{
			if (arg == null)
			{
				Console.WriteLine();
				return;
			}
			Console.WriteLine(arg);
		}

	}
}
