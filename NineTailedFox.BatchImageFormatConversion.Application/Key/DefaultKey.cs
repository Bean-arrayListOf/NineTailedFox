using System.Security.Cryptography;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;

namespace NineTailedFox.BatchImageFormatConversion.Application.Key
{
	public class DefaultKey : IKey
	{
		private readonly IReadOnlyList<string> Keys;

		public DefaultKey()
		{
			var list = new List<string>();
			
			// ntf-key-kK9wX2zW9fW4tP1nP1aI3hY7sD8gB1bK4rP7rD0gD1uE3aR0gK
			// ntf-key-dL0mD3qR3iY7lL9cQ1sZ8eN5cS5aC5lM8lQ1jT4wH5xJ4mU9tR
			// ntf-key-yP9tT5xZ4xB1vI2lQ8oR0sY0nS2cA8kN9pH1tG7qU7aV3zF6kG
			// ntf-key-eP3cR9dC0hO8pW5gX6tU5fK3rF3eA8gI2lB5pQ2rV2mQ7sW2mT
			// ntf-key-jJ5cM7iS0wY6dJ5rY7vW3zS5gR6pD4cX8hU8uY6iV1dE0qA9fJ
			
			list.Add("d51058c45ef1e40a5d0753ec265d78d81c6ab71c47554c331d050f0de0e05fc59429f3747c543da34af67c5dcc1916e197d07f719242ed8b0fc251f7f0758307");
			list.Add("f093a7476c0b034be8a7757224ded61fef81c0f79fa585014d8072ad19eeef7c9c958abab6eb52156d91dddda63d146f6122fa6e98f9f700752fef250afaa185");
			list.Add("32517be8ef2eb5ce47ffe113655e23c9a7476ecefa80bdde1a309cce0d6339e74ffaec4ab0e025b2b9568c980b911d0f9da5edb3906ba2a7b9a3b8f76e90a11b");
			list.Add("143e5b080c276c399a2c0282d9d2fd5b30a19c210110e2091e947005df849f427796dd314ddf7bcc215affd2bbb2bd0918cdd3a3295804ce4630fedd5a7b4b0d");
			list.Add("2a39e5984677e483b2afb1a3a8b79d88f6b6b8c232573f68275386fb714057ef515a74624a4e1634f1a4751b4e47f3918f88567cd49a4968f18d420929482ea7");

			Keys = list;
		}
		
		public IReadOnlyList<string> Get()
		{
			return Keys;
		}

		public bool Verify(string key)
		{
			return Keys.Contains(GetFullHash(key));
		}

		public string GetFullHash(string str)
		{
			var hash = str;
			for (int i = 0; i < 5; i++)
			{
				hash = GetSHA512(hash);
			}

			return hash;
		}
		
		public string GetSHA512(string str)
		{
			var dataBytes = Encoding.UTF8.GetBytes(str);

			using var sha512 = SHA512.Create();
			var hashBytes = sha512.ComputeHash(dataBytes);
			
			var hashString = new StringBuilder();
			foreach (var b in hashBytes)
			{
				hashString.Append(b.ToString("x2"));
			}

			return hashString.ToString();
		}
	}
}