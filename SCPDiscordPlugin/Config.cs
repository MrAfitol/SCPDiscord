using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PluginAPI.Helpers;
using YamlDotNet.Serialization;
using Resources = SCPDiscord.Properties.Resources;

namespace SCPDiscord
{
	public static class Config
	{
		public class ConfigParseException : Exception
		{
			public ConfigParseException(Exception e) : base(e.Message, e) { }
		}

		public static bool ready;

		private static readonly Dictionary<string, string> configStrings = new Dictionary<string, string>
		{
			{ "bot.ip",             "127.0.0.1" },

			{ "settings.language",  "english"   },
			{ "settings.timestamp", ""          }
		};

		private static readonly Dictionary<string, bool> configBools = new Dictionary<string, bool>
		{
			{ "settings.playercount",                 true  },
			{ "settings.verbose",                     true  },
			{ "settings.debug",                       true  },
			{ "settings.metrics",                     true  },
			{ "settings.configvalidation",            true  },
			{ "settings.rolesync",                    false },
			{ "settings.useglobaldirectory.language", true  },
			{ "settings.useglobaldirectory.rolesync", true  },
			{ "settings.regeneratelanguagefiles",     false }
		};

		private static readonly Dictionary<string, int> configInts = new Dictionary<string, int>
		{
			{ "bot.port", 8888 }
		};

		// The following four are a bit messed up but the language and config systems need slightly different versions of this list so it had to be this way
		private static readonly List<string> configAndLanguageNodes = new List<string>
		{
			"messages.onconnect",
			"messages.onplayerleave",
			"messages.onroundend",
			"messages.onroundrestart",
			"messages.onroundstart",
			"messages.onsetservername",
			"messages.onwaitingforplayers",

			"messages.ondecontaminate",
			"messages.ondetonate",
			"messages.ongeneratorfinish",
			"messages.onscp914activate",
			"messages.onstartcountdown.initiated",
			"messages.onstartcountdown.noplayer",
			"messages.onstartcountdown.resumed",
			"messages.onstopcountdown.default",
			"messages.onstopcountdown.noplayer",
			"messages.onsummonvehicle.chaos",
			"messages.onsummonvehicle.mtf",

			"messages.on079addexp",
			"messages.on079elevator.down",
			"messages.on079elevator.up",
			"messages.on079levelup",
			"messages.on079lockdoor",
			"messages.on079unlockdoor",
			"messages.on079lockdown",
			"messages.on079cancellockdown",
			"messages.on079teslagate",
			"messages.onassignteam",
			"messages.oncallcommand.console.player",
			"messages.oncallcommand.console.server",
			"messages.oncallcommand.game.player",
			"messages.oncallcommand.game.server",
			"messages.oncallcommand.remoteadmin.player",
			"messages.oncallcommand.remoteadmin.server",
			"messages.oncontain106",
			"messages.ondooraccess.allowed",
			"messages.ondooraccess.denied",
			"messages.onelevatoruse",
			"messages.onexecutedcommand.console.player",
			"messages.onexecutedcommand.console.server",
			"messages.onexecutedcommand.game.player",
			"messages.onexecutedcommand.game.server",
			"messages.onexecutedcommand.remoteadmin.player",
			"messages.onexecutedcommand.remoteadmin.server",
			"messages.ongeneratoractivated",
			"messages.ongeneratorclose",
			"messages.ongeneratordeactivated",
			"messages.ongeneratoropen",
			"messages.ongeneratorunlock",
			"messages.ongrenadeexplosion",
			"messages.ongrenadehitplayer",
			"messages.onhandcuff.default",
			"messages.onhandcuff.nootherplayer",
			"messages.onhandcuffremoved.default",
			"messages.onhandcuffremoved.nootherplayer",
			"messages.onlure",
			"messages.onitemuse",
			"messages.onnicknameset",
			"messages.onplayerdie.default",
			"messages.onplayerdie.friendlyfire",
			"messages.onplayerdie.nokiller",
			"messages.onplayerdropammo",
			"messages.onplayerdropitem",
			"messages.onplayerhurt.default",
			"messages.onplayerhurt.friendlyfire",
			"messages.onplayerhurt.noattacker",
			"messages.onplayerinfected",
			"messages.onplayerjoin",
			"messages.onplayerpickupammo",
			"messages.onplayerpickuparmor",
			"messages.onplayerpickupitem",
			"messages.onplayerpickupscp330",
			"messages.onplayerradioswitch",
			"messages.onplayertriggertesla.default",
			"messages.onplayertriggertesla.ignored",
			"messages.onpocketdimensiondie",
			"messages.onpocketdimensionenter",
			"messages.onpocketdimensionexit",
			"messages.onrecallzombie",
			"messages.onreload",
			"messages.onscp914changeknob",
			"messages.onsetrole",
			"messages.onshoot.default",
			"messages.onshoot.friendlyfire",
			"messages.onshoot.notarget",
			"messages.onspawn",
			"messages.onspawnragdoll",
			"messages.onthrowprojectile",

			"messages.onban.server",
			"messages.onkick.server",
			"messages.onban.player",
			"messages.onkick.player",

			"messages.onteamrespawn.mtf",
			"messages.onteamrespawn.ci",
			"messages.onsetntfunitname",

			"messages.connectedtobot",
		};

		private static readonly List<string> languageOnlyNodes = new List<string>
		{
			"messages.botstatus",
			"messages.invalidsteamid",
			"messages.invalidduration",
			"messages.playerbanned",
			"messages.consolecommandfeedback",
			"messages.invalidsteamidorip",
			"messages.playerunbanned",
			"messages.playerkicked",
			"messages.playernotfound",
			"messages.kickall"
		};

		public static readonly List<string> languageNodes = configAndLanguageNodes.Concat(languageOnlyNodes).ToList();

		// Convert message nodes to a dictionary to hold the settings from the config
		private static readonly Dictionary<string, string[]> configArrays = configAndLanguageNodes.Zip(new string[configAndLanguageNodes.Count][],
			(name, emptyArray) => (name: name, emptyArray: emptyArray)).ToDictionary(ns => ns.name, ns => ns.emptyArray);

		private static readonly Dictionary<string, Dictionary<string, ulong>> configDicts = new Dictionary<string, Dictionary<string, ulong>>
		{
			{ "channels", new Dictionary<string, ulong>() }
		};

		public static Dictionary<ulong, string[]> roleDictionary = new Dictionary<ulong, string[]>();

		internal static void Reload(SCPDiscord plugin)
		{
			ready = false;

			if (!Directory.Exists(GetConfigDir()))
			{
				Directory.CreateDirectory(GetConfigDir());
			}

			if (!File.Exists(GetConfigPath()))
			{
				plugin.Info("Config file '" + Config.GetConfigPath() + "' does not exist, creating...");
				File.WriteAllText(Config.GetConfigPath(), Encoding.UTF8.GetString(Resources.config));
			}

			// Reads file contents into FileStream
			FileStream stream = File.OpenRead(GetConfigDir() + "config.yml");

			// Converts the FileStream into a YAML Dictionary object
			IDeserializer deserializer = new DeserializerBuilder().Build();
			object yamlObject = deserializer.Deserialize(new StreamReader(stream));

			// Converts the YAML Dictionary into JSON String
			ISerializer serializer = new SerializerBuilder()
				.JsonCompatible()
				.Build();
			string jsonString = serializer.Serialize(yamlObject);

			JObject json = JObject.Parse(jsonString);

			plugin.Verbose("Reading config validation");

			// Reads the configvalidation node first as it is used for reading the others
			try
			{
				configBools["settings.configvalidation"] = json.SelectToken("settings.configvalidation").Value<bool>();
			}
			catch (ArgumentNullException)
			{
				plugin.Warn("Config bool 'settings.configvalidation' not found, using default value: true");
			}

			// Read config strings
			foreach (KeyValuePair<string, string> node in configStrings.ToList())
			{
				try
				{
					plugin.Debug("Reading config string '" + node.Key + "'");
					configStrings[node.Key] = json.SelectToken(node.Key).Value<string>();
				}
				catch (ArgumentNullException)
				{
					plugin.Warn("Config string '" + node.Key + "' not found, using default value: \"" + node.Value + "\"");
				}
				catch (Exception e)
				{
					plugin.Error("Reading config string '" + node.Key + "' failed: " + e.Message);
					throw new ConfigParseException(e);
				}
			}

			// Read config ints
			foreach (KeyValuePair<string, int> node in configInts.ToList())
			{
				try
				{
					plugin.Debug("Reading config int '" + node.Key + "'");
					configInts[node.Key] = json.SelectToken(node.Key).Value<int>();
				}
				catch (ArgumentNullException)
				{
					plugin.Warn("Config int '" + node.Key + "' not found, using default value: \"" + node.Value + "\"");
				}
				catch (Exception e)
				{
					plugin.Error("Reading config int '" + node.Key + "' failed: " + e.Message);
					throw new ConfigParseException(e);
				}
			}

			// Read config bools
			foreach (KeyValuePair<string, bool> node in configBools.ToList().Where(kvm => kvm.Key != "settings.configvalidation"))
			{
				try
				{
					plugin.Debug("Reading config bool '" + node.Key + "'");
					configBools[node.Key] = json.SelectToken(node.Key).Value<bool>();
				}
				catch (ArgumentNullException)
				{
					plugin.Warn("Config bool '" + node.Key + "' not found, using default value: " + node.Value);
				}
				catch (Exception e)
				{
					plugin.Error("Reading config bool '" + node.Key + "' failed: " + e.Message);
					throw new ConfigParseException(e);
				}
			}


			// Read config arrays
			foreach (KeyValuePair<string, string[]> node in configArrays.ToList())
			{
				try
				{
					plugin.Debug("Reading config array '" + node.Key + "'");
					configArrays[node.Key] = json.SelectToken(node.Key).Value<JArray>().Values<string>().ToArray();
				}
				catch (ArgumentNullException)
				{
					plugin.Warn("Config array '" + node.Key + "' not found, using default value: []");
				}
				catch (Exception e)
				{
					plugin.Error("Reading config arrays '" + node.Key + "' failed: " + e.Message);
					throw new ConfigParseException(e);
				}
			}

			// Read config dictionaries
			foreach (KeyValuePair<string, Dictionary<string, ulong>> node in configDicts.ToList())
			{
				try
				{
					plugin.Debug("Reading config dict '" + node.Key + "'");
					configDicts[node.Key] = json.SelectToken(node.Key).Value<JArray>().ToDictionary(k => ((JObject)k).Properties().First().Name, v => v.Values().First().Value<ulong>());
				}
				catch (ArgumentNullException)
				{
					plugin.Warn("Config dictionary '" + node.Key + "' not found, using default value: []");
				}
				catch (Exception e)
				{
					plugin.Error("Reading config dict '" + node.Key + "' failed: " + e.Message);
					throw new ConfigParseException(e);
				}
			}

			// Read rolesync system
			if (GetBool("settings.rolesync"))
			{
				try
				{
					plugin.Debug("Reading rolesync");
					roleDictionary = json.SelectToken("rolesync").Value<JArray>().ToDictionary(k => ulong.Parse(((JObject)k).Properties().First().Name), v => v.Values().First().Value<JArray>().Values<string>().ToArray());
				}
				catch (Exception)
				{
					plugin.Warn("The rolesync config list is invalid, rolesync disabled.");
					SetBool("settings.rolesync", false);
				}
			}

			plugin.Debug("Finished reading config file");

			if (GetBool("settings.configvalidation"))
			{
				ValidateConfig(plugin);
			}

			ready = true;
		}

		public static bool GetBool(string node)
		{
			return configBools[node];
		}

		public static string GetString(string node)
		{
			return configStrings[node];
		}

		public static int GetInt(string node)
		{
			return configInts[node];
		}

		public static string[] GetArray(string node)
		{
			return configArrays[node];
		}

		public static Dictionary<string, ulong> GetDict(string node)
		{
			return configDicts[node];
		}

		public static void SetBool(string key, bool value)
		{
			configBools[key] = value;
		}

		public static void SetString(string key, string value)
		{
			configStrings[key] = value;
		}

		public static void SetInt(string key, int value)
		{
			configInts[key] = value;
		}

		public static void SetArray(string key, string[] value)
		{
			configArrays[key] = value;
		}

		public static void SetDict(string key, Dictionary<string, ulong> value)
		{
			configDicts[key] = value;
		}

		public static string GetConfigDir()
		{
			return Paths.LocalPlugins.Plugins + "/SCPDiscord/";
		}

		public static string GetConfigPath()
		{
			return GetConfigDir() + "config.yml";
		}

		public static string GetLanguageDir()
		{
			if (GetBool("settings.useglobaldirectory.language"))
			{
				return Paths.GlobalPlugins.Plugins + "/SCPDiscord/Languages/";
			}
			else
			{
				return Paths.LocalPlugins.Plugins + "/SCPDiscord/Languages/";
			}
		}

		public static string GetRolesyncDir()
		{
			if (GetBool("settings.useglobaldirectory.rolesync"))
			{
				return Paths.GlobalPlugins.Plugins + "/SCPDiscord/";
			}
			else
			{
				return Paths.LocalPlugins.Plugins + "/SCPDiscord/";
			}
		}

		public static string GetRolesyncPath()
		{
			return GetRolesyncDir() + "rolesync.json";
		}

		public static void ValidateConfig(SCPDiscord plugin)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("\n||||||||||||| SCPDiscord config validator ||||||||||||||\n");
			sb.Append("------------ Config strings ------------\n");
			foreach (KeyValuePair<string, string> node in configStrings)
			{
				sb.Append(node.Key + ": " + node.Value + "\n");
			}

			sb.Append("------------ Config ints ------------\n");
			foreach (KeyValuePair<string, int> node in configInts)
			{
				sb.Append(node.Key + ": " + node.Value + "\n");
			}

			sb.Append("------------ Config bools ------------\n");
			foreach (KeyValuePair<string, bool> node in configBools)
			{
				sb.Append(node.Key + ": " + node.Value + "\n");
			}

			sb.Append("------------ Config arrays ------------\n");
			foreach (KeyValuePair<string, string[]> node in configArrays)
			{
				sb.Append(node.Key + ": [ " + string.Join(", ", node.Value) + " ]\n");
				if (node.Key.StartsWith("messages."))
				{
					foreach (string s in node.Value)
					{
						if (!GetDict("channels").ContainsKey(s))
						{
							sb.Append("WARNING: Channel alias '" + s + "' does not exist!\n");
						}
					}
				}
			}

			sb.Append("------------ Config dictionaries ------------\n");
			foreach (KeyValuePair<string, Dictionary<string, ulong>> node in configDicts)
			{
				sb.Append(node.Key + ":\n");
				foreach (KeyValuePair<string, ulong> subNode in node.Value)
				{
					sb.Append("    " + subNode.Key + ": " + subNode.Value + "\n");
				}
			}

			sb.Append("------------ Rolesync system ------------\n");
			foreach (KeyValuePair<ulong, string[]> node in roleDictionary)
			{
				sb.Append(node.Key + ":\n");
				foreach (string command in node.Value)
				{
					sb.Append("    " + command + "\n");
				}
			}

			sb.Append("|||||||||||| End of config validation ||||||||||||");
			plugin.Info(sb.ToString());
		}
	}
}