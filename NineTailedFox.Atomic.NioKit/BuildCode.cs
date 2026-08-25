using System.Collections.Generic;

namespace NineTailedFox.Atomic.NioKit
{
	public class BuildCode : ICode
	{
		private readonly string Command;
		private readonly List<string> Args;
		private readonly Dictionary<string, string> Envs;

		public BuildCode(string command)
		{
			Command = command;
			Args = new List<string>();
			Envs = new Dictionary<string, string>();
		}
		
		public void AddArg(string code)
		{
			Args.Add(code);
		}
		
		public void AddArgs(params string[] code)
		{
			Args.AddRange(code);
		}
		public void AddEnv(string key,string value)
		{
			Envs.Add(key, value);
		}
		public void AddEnvs(Dictionary<string, string> env)
		{
			foreach (var e in env)
			{
				Envs.Add(e.Key,e.Value);
			}
		}
		public List<string> GetCode()
		{
			var code = new List<string> { Command };
			code.AddRange(Args);
			return code;
		}
		public Dictionary<string, string> GetEnv()
		{
			return Envs;
		}
	}
}