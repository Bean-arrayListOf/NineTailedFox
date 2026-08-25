using DotNetEnv.Configuration;
using Hocon.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using NineTailedFox.Atomic.AppKit;
using Tomlyn.Extensions.Configuration;

namespace NineTailedFox.Atomic.ConfigKit
{
	public static class ConfigKit
	{
		private static readonly IConfigurationRoot RootConfig;

		static ConfigKit()
		{
			var config = new ConfigurationBuilder();

			config.ReadDataConfig(ConfigFileType.Json);
			config.ReadDataConfig(ConfigFileType.Xml);
			config.ReadDataConfig(ConfigFileType.Yaml);
			config.ReadDataConfig(ConfigFileType.Toml);
			config.ReadDataConfig(ConfigFileType.Hocon);
			config.ReadDataConfig(ConfigFileType.Env);
			config.ReadDataConfig(ConfigFileType.Ini);
			
			config.ReadConfig(ConfigFileType.Json);
			config.ReadConfig(ConfigFileType.Xml);
			config.ReadConfig(ConfigFileType.Yaml);
			config.ReadConfig(ConfigFileType.Toml);
			config.ReadConfig(ConfigFileType.Hocon);
			config.ReadConfig(ConfigFileType.Env);
			config.ReadConfig(ConfigFileType.Ini);

			config.AddEnvironmentVariables();

			RootConfig = config.Build();
		}

		// public static ConfigurationBuilder ReadBaseJson(this ConfigurationBuilder cb,ConfigFileType type)
		// {
		// 	
		// }

		public static void ReadConfig(this ConfigurationBuilder cb, ConfigFileType type)
		{
			var rootPath = Path.Combine(AppInfo.BaseDirectory);
			cb.ReadConfigFile(rootPath, type);
		}

		public static void ReadDataConfig(this ConfigurationBuilder cb, ConfigFileType type)
		{
			var rootPath = Path.Combine(AppInfo.AppDataDirectory, "config");
			cb.ReadConfigFile(rootPath, type);
		}

		public static void ReadConfigFile(this ConfigurationBuilder cb, string rootPath, ConfigFileType type)
		{
			
			switch (type)
			{
				case ConfigFileType.Json:
					var configJson = Path.Combine(rootPath, $"{AppInfo.AppName}.json");
					if (File.Exists(configJson))
					{
						cb.AddJsonFile(configJson);
					}
					break;
				case ConfigFileType.Xml:
					var configXml = Path.Combine(rootPath, $"{AppInfo.AppName}.xml");
					if (File.Exists(configXml))
					{
						cb.AddXmlFile(configXml);
					}
					break;
				case ConfigFileType.Env:
					var configEnv = Path.Combine(rootPath, $"{AppInfo.AppName}.env");
					if (File.Exists(configEnv))
					{
						cb.AddDotNetEnv(configEnv);
					}
					break;
				case ConfigFileType.Ini:
					var configIni = Path.Combine(rootPath, $"{AppInfo.AppName}.ini");
					if (File.Exists(configIni))
					{
						cb.AddIniFile(configIni);
					}
					break;
				case ConfigFileType.Yaml:
					var configYaml = Path.Combine(rootPath, $"{AppInfo.AppName}.yaml");
					// if (File.Exists(configYaml))
					// {
					// 	cb.AddYamlFile(configYaml);
					// }
					//
					// var configYml = Path.Combine(rootPath, $"{AppInfo.AppName}.yml");
					// if (File.Exists(configYml))
					// {
					// 	cb.AddYamlFile(configYml);
					// }
					break;
				case ConfigFileType.Toml:
					var configToml = Path.Combine(rootPath, $"{AppInfo.AppName}.toml");
					if (File.Exists(configToml))
					{
						cb.AddTomlFile(configToml);
					}
					break;
				case ConfigFileType.Hocon:
					var configHocon = Path.Combine(rootPath, $"{AppInfo.AppName}.hocon");
					if (File.Exists(configHocon))
					{
						cb.AddHoconFile(configHocon);
					}
					
					var configConf = Path.Combine(rootPath, $"{AppInfo.AppName}.conf");
					if (File.Exists(configConf))
					{
						cb.AddHoconFile(configConf);
					}
					break;
				default:
					break;
			}
		}

		// public static string? Get(string key)
		// {
		// 	return RootConfig[key];
		// }
		//
		// public static string GetDefault(string key, string defaultValue)
		// {
		// 	return Get(key) ?? defaultValue;
		// }

		public static List<string>? Gets(string key,char? separator = null)
		{
			var env = Get<string>(key);

			var spl = env?.Split(separator ?? Path.PathSeparator);

			return spl?.ToList();
		}

		public static List<string> Gets(string key, List<string> defaultValue, char? separator = null)
		{
			return Gets(key, separator) ?? defaultValue;
		}

		public static T? Get<T>(string key)
		{
			return RootConfig.GetValue<T>(key);
		}

		public static T Get<T>(string key, T defaultValue)
		{
			return Get<T>(key) ?? defaultValue;
		}

		public static string? GetString(string key)
		{
			return Get<string>(key)?
				.Replace("@{AppName}",AppInfo.AppName)
				.Replace("@{AppFullName}",AppInfo.AppFullName)
				.Replace("@{AppCultureName}",AppInfo.AppCultureName)
				.Replace("@{AppVersion}",AppInfo.AppVersion.ToString())
				.Replace("@{AppBuildVersion}",AppInfo.AppBuildVersion.ToString())
				.Replace("@{AppMajorVersion}",AppInfo.AppMajorVersion.ToString())
				.Replace("@{AppMajorRevisionVersion}",AppInfo.AppMajorRevisionVersion.ToString())
				.Replace("@{AppMinorVersion}",AppInfo.AppMinorVersion.ToString())
				.Replace("@{AppMinorRevisionVersion}",AppInfo.AppMinorRevisionVersion.ToString())
				.Replace("@{AppRevisionVersion}",AppInfo.AppRevisionVersion.ToString())
				.Replace("@{BaseDirectory}",AppInfo.BaseDirectory)
				.Replace("@{BaseDir}",AppInfo.BaseDirectory)
				.Replace("@{AppDataDirectory}",AppInfo.AppDataDirectory)
				.Replace("@{AppDataDir}",AppInfo.AppDataDirectory);
		}
		
		public static string GetString(string key,string defaultValue)
		{
			return GetString(key) ?? defaultValue;
		}

		public static int? GetInt(string key)
		{
			return Get<int>(key);
		}

		public static int GetInt(string key, int defaultValue)
		{
			return GetInt(key) ?? defaultValue;
		}

		public static bool? GetBool(string key)
		{
			return Get<bool>(key);
		}

		public static bool GetBool(string key, bool defaultValue)
		{
			return GetBool(key) ?? defaultValue;
		}
		
		
			
	}
}