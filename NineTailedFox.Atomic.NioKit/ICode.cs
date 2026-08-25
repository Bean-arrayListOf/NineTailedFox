namespace NineTailedFox.Atomic.NioKit
{
	public interface ICode
	{
		public void AddArg(string code);
		public void AddArgs(params string[] code);
		public void AddEnv(string key,string value);
		public void AddEnvs(Dictionary<string, string> env);
		public List<string> GetCode();
		public Dictionary<string, string> GetEnv();
	}
}