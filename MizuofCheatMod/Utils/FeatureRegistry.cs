using System.Collections.Generic;
using System.Text;
using MizuofCheatMod.Harmony;

namespace MizuofCheatMod.Utils
{
	internal enum FeatureStatus
	{
		Unknown,
		Implemented,
		Partial,
		Unimplemented
	}

	internal enum FeatureRequires
	{
		None,
		HarmonyPatch,
		FrameUpdate,
		ReflectionCall
	}

	internal sealed class FeatureEntry
	{
		internal string Key { get; }
		internal string DisplayName { get; }
		internal FeatureStatus Status { get; }
		internal FeatureRequires Requires { get; }
		internal bool Enabled { get; set; }
		internal bool Verified { get; set; }

		internal FeatureEntry(string key, string displayName, FeatureStatus status, FeatureRequires requires)
		{
			Key = key;
			DisplayName = displayName;
			Status = status;
			Requires = requires;
		}
	}

	internal static class FeatureRegistry
	{
		private static readonly Dictionary<string, FeatureEntry> _features = new Dictionary<string, FeatureEntry>();

		internal static int TotalCount
		{
			get
			{
				return _features.Count;
			}
		}

		internal static int EnabledCount
		{
			get
			{
				int count = 0;
				foreach (FeatureEntry entry in _features.Values)
				{
					if (entry.Enabled)
					{
						count++;
					}
				}
				return count;
			}
		}

		internal static int VerifiedCount
		{
			get
			{
				int count = 0;
				foreach (FeatureEntry entry in _features.Values)
				{
					if (entry.Verified)
					{
						count++;
					}
				}
				return count;
			}
		}

		internal static int ImplementedCount
		{
			get
			{
				int count = 0;
				foreach (FeatureEntry entry in _features.Values)
				{
					if (entry.Status == FeatureStatus.Implemented)
					{
						count++;
					}
				}
				return count;
			}
		}

		internal static int PartialCount
		{
			get
			{
				int count = 0;
				foreach (FeatureEntry entry in _features.Values)
				{
					if (entry.Status == FeatureStatus.Partial)
					{
						count++;
					}
				}
				return count;
			}
		}

		internal static int UnimplementedCount
		{
			get
			{
				int count = 0;
				foreach (FeatureEntry entry in _features.Values)
				{
					if (entry.Status == FeatureStatus.Unimplemented)
					{
						count++;
					}
				}
				return count;
			}
		}

		internal static Dictionary<string, FeatureEntry> GetAllFeatures()
		{
			return _features;
		}

		internal static void Register(string key, string displayName, FeatureStatus status, FeatureRequires requires = FeatureRequires.None)
		{
			if (!_features.ContainsKey(key))
			{
				_features[key] = new FeatureEntry(key, displayName, status, requires);
			}
		}

		internal static void SetEnabled(string key, bool enabled)
		{
			if (_features.TryGetValue(key, out FeatureEntry entry))
			{
				entry.Enabled = enabled;

				// 管理依赖资源
				if (enabled && entry.Requires == FeatureRequires.HarmonyPatch)
				{
					PatchController.ApplyAll();
				}

				if (!AnyHarmonyFeatureEnabled())
				{
					PatchController.RemoveAll();
				}
			}
		}

		private static bool AnyHarmonyFeatureEnabled()
		{
			foreach (FeatureEntry entry in _features.Values)
			{
				if (entry.Enabled && entry.Requires == FeatureRequires.HarmonyPatch)
				{
					return true;
				}
			}
			return false;
		}

		internal static bool IsEnabled(string key)
		{
			return _features.TryGetValue(key, out FeatureEntry entry) && entry.Enabled;
		}

		internal static void SetVerified(string key)
		{
			if (_features.TryGetValue(key, out FeatureEntry entry))
			{
				entry.Verified = true;
			}
		}

		internal static string GetReport()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== FeatureRegistry 功能报告 ===");
			sb.Append("总计: ");
			sb.Append(TotalCount);
			sb.Append(", 已实现: ");
			sb.Append(ImplementedCount);
			sb.Append(", 部分: ");
			sb.Append(PartialCount);
			sb.Append(", 未完成: ");
			sb.Append(UnimplementedCount);
			sb.Append(", 已开启: ");
			sb.Append(EnabledCount);
			sb.Append(", 已验证: ");
			sb.Append(VerifiedCount);
			sb.AppendLine();
			foreach (KeyValuePair<string, FeatureEntry> kv in _features)
			{
				FeatureEntry fe = kv.Value;
				sb.Append("  ");
				sb.Append(fe.Enabled ? "[ON]" : "[--]");
				sb.Append(" ");
				sb.Append(fe.Status switch
				{
					FeatureStatus.Implemented => "[已实现]",
					FeatureStatus.Partial => "[部分]",
					FeatureStatus.Unimplemented => "[未完成]",
					_ => "[未知]"
				});
				sb.Append(" ");
				sb.Append(fe.Key);
				sb.Append(" [");
				sb.Append(fe.Verified ? "OK" : "..");
				sb.Append("] ");
				sb.Append(fe.Requires switch
				{
					FeatureRequires.HarmonyPatch => "(Harmony)",
					FeatureRequires.FrameUpdate => "(每帧)",
					FeatureRequires.ReflectionCall => "(反射)",
					_ => ""
				});
				sb.AppendLine();
			}
			return sb.ToString();
		}

		internal static void InitDefaults()
		{
			Register("player_stat_lock", "属性锁定", FeatureStatus.Implemented, FeatureRequires.FrameUpdate);

			Register("item_max_sell", "最高售价", FeatureStatus.Implemented, FeatureRequires.HarmonyPatch);
			Register("item_free_shop", "商店免费", FeatureStatus.Implemented, FeatureRequires.HarmonyPatch);

			Register("time_freeze", "时间冻结", FeatureStatus.Implemented, FeatureRequires.HarmonyPatch);

			Register("combat_god", "无敌模式", FeatureStatus.Implemented, FeatureRequires.HarmonyPatch);
			Register("combat_1hk", "一击必杀", FeatureStatus.Implemented, FeatureRequires.HarmonyPatch);

			Register("social_max_fav", "好感最大化", FeatureStatus.Implemented, FeatureRequires.FrameUpdate);
			Register("location_teleport", "地点传送", FeatureStatus.Implemented, FeatureRequires.None);
			Register("academy_unlock", "技能解锁", FeatureStatus.Implemented, FeatureRequires.None);
			Register("achievement_unlock", "成就解锁", FeatureStatus.Implemented, FeatureRequires.ReflectionCall);
			Register("shop_all_items", "商店全物品", FeatureStatus.Implemented, FeatureRequires.HarmonyPatch);
			Register("combat_stat_lock", "战斗属性锁定", FeatureStatus.Implemented, FeatureRequires.FrameUpdate);
			Register("ending_all_jobs", "强制全结局可选", FeatureStatus.Implemented, FeatureRequires.HarmonyPatch);
		}
	}
}
