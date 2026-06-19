using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace MizuofCheatMod.Utils
{
	/// <summary>
	/// 运行时优先调用游戏方法，兜底直接写字段的统一解析器
	/// 设计哲学：让游戏自身的逻辑处理数据变化，而非绕过它
	/// </summary>
	internal static class GameMethodResolver
	{
		private static bool _scanned;
		private static readonly Dictionary<string, Dictionary<string, MethodInfo>> _methodsByType =
			new Dictionary<string, Dictionary<string, MethodInfo>>();

		/// <summary>
		/// 扫描关键游戏类型，缓存所有 public 实例方法
		/// </summary>
		internal static void Scan()
		{
			if (_scanned) return;
			_scanned = true;

			string[] targetTypes = { "Status", "MyData", "ItemData", "FriendData", "CurriculumData" };
			foreach (string typeName in targetTypes)
			{
				Type t = GameReflect.ResolveType(typeName);
				if (t == null) continue;

				var methodMap = new Dictionary<string, MethodInfo>();
				MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance);
				foreach (MethodInfo m in methods)
				{
					if (!m.IsSpecialName)
					{
						string key = m.Name + "|" + m.GetParameters().Length;
						if (!methodMap.ContainsKey(key))
						{
							methodMap[key] = m;
						}
					}
				}
				_methodsByType[typeName] = methodMap;
			}
			MelonLogger.Msg("[Resolver] 已缓存 " + _methodsByType.Count + " 个类型的方法表");
		}

		/// <summary>
		/// 尝试调用游戏方法，返回 true 表示成功
		/// </summary>
		internal static bool TryCall(string typeName, string methodName, object instance, params object[] args)
		{
			if (!_scanned) Scan();
			if (instance == null) return false;

			if (!_methodsByType.TryGetValue(typeName, out var methodMap))
				return false;

			if (!methodMap.TryGetValue(methodName, out MethodInfo mi))
			{
				// 尝试 "Name|ParamCount" 格式的 key
				string key = methodName + "|" + (args?.Length ?? 0);
				if (!methodMap.TryGetValue(key, out mi))
				{
					// 遍历匹配方法名（不关心参数个数）
					foreach (var kv in methodMap)
					{
						if (kv.Key == methodName || kv.Key.StartsWith(methodName + "|"))
						{
							mi = kv.Value;
							break;
						}
					}
					if (mi == null) return false;
				}
			}

			try
			{
				mi.Invoke(instance, args);
				return true;
			}
			catch (Exception ex)
			{
				MelonLogger.Msg("[Resolver] " + typeName + "." + methodName + " 调用失败: " + ex.Message);
				return false;
			}
		}

		/// <summary>
		/// 检查某个方法是否存在（仅按名，忽略重载）
		/// </summary>
		internal static bool HasMethod(string typeName, string methodName)
		{
			if (!_scanned) Scan();
			if (!_methodsByType.TryGetValue(typeName, out var map))
				return false;
			foreach (string key in map.Keys)
			{
				if (key == methodName || key.StartsWith(methodName + "|"))
					return true;
			}
			return false;
		}

		/// <summary>
		/// 获取可用的方法名列表（调试用）
		/// </summary>
		internal static string[] GetAvailableMethods(string typeName)
		{
			if (!_scanned) Scan();
			if (_methodsByType.TryGetValue(typeName, out var map))
			{
				// 去重：只保留方法名
				var names = new HashSet<string>();
				foreach (string key in map.Keys)
				{
					string name = key.Contains("|") ? key.Substring(0, key.IndexOf('|')) : key;
					names.Add(name);
				}
				var result = new string[names.Count];
				names.CopyTo(result);
				return result;
			}
			return new string[0];
		}

		// ====================================================================
		// 专用封装：Status 相关
		// ====================================================================

		internal static void SetMoney(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			// 尝试：SetMoney > AddMoney > SpendMoney > EarnMoney > ChangeMoney
			if (TryCall("Status", "SetMoney", st, value)) return;
			if (TryCall("Status", "setMoney", st, value)) return;
			if (value > 0 && TryCall("Status", "AddMoney", st, value)) return;
			if (TryCall("Status", "EarnMoney", st, value)) return;
			// 兜底
			GameReflect.SetInt(st, "money", value);
		}

		internal static void SetStress(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetStress", st, value)) return;
			if (TryCall("Status", "setStress", st, value)) return;
			GameReflect.SetInt(st, "stress", value);
		}

		internal static void SetReputation(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetReputation", st, value)) return;
			GameReflect.SetInt(st, "reputation", value);
		}

		internal static void SetBlackCoin(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetBlackCoin", st, value)) return;
			if (TryCall("Status", "AddBlackCoin", st, value)) return;
			GameReflect.SetInt(st, "blackCoin", value);
		}

		internal static void SetActivePowerMax(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetActivePowerMax", st, value)) return;
			if (TryCall("Status", "setActivePowerMax", st, value)) return;
			GameReflect.SetInt(st, "activePowerMax", value);
		}

		internal static void FillActivePower()
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "RecoverAllActivePower", st)) return;
			if (TryCall("Status", "FillActivePower", st)) return;
			if (TryCall("Status", "SetActivePower", st, GameReflect.GetInt(st, "activePowerMax"))) return;
			GameReflect.SetInt(st, "activePower", GameReflect.GetInt(st, "activePowerMax"));
		}

		internal static void SetGoodAction(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetGoodAction", st, value)) return;
			if (TryCall("Status", "AddGoodAction", st, value)) return;
			GameReflect.SetInt(st, "goodAction", value);
		}

		internal static void SetBadAction(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetBadAction", st, value)) return;
			if (TryCall("Status", "AddBadAction", st, value)) return;
			GameReflect.SetInt(st, "badAction", value);
		}

		internal static void SetBalance(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetBalance", st, value)) return;
			if (TryCall("Status", "SetGbBalance", st, value)) return;
			GameReflect.SetInt(st, "gbBalance", value);
		}

		internal static void SetAttribute(string field, int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;

			// 尝试 Set{FieldName}(int) 模式
			string capField = char.ToUpper(field[0]) + field.Substring(1);
			if (TryCall("Status", "Set" + capField, st, value)) return;
			if (TryCall("Status", "set" + capField, st, value)) return;

			// 兜底
			GameReflect.SetInt(st, field, value);
		}

		internal static void SetLevel(string field, int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "Set" + field, st, value)) return;
			if (TryCall("Status", "set" + field, st, value)) return;
			GameReflect.SetInt(st, field, value);
		}

		internal static void SetRate(string field, int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "Set" + field, st, value)) return;
			if (TryCall("Status", "set" + field, st, value)) return;
			GameReflect.SetInt(st, field, value);
		}

		internal static void SetFatherFav(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetFatherFavarite", st, value)) return;
			if (TryCall("Status", "setFatherFavarite", st, value)) return;
			GameReflect.SetInt(st, "fatherFavarite", value);
		}

		internal static void SetFatherFavLevel(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetFatherFavLevel", st, value)) return;
			if (TryCall("Status", "setFatherFavLevel", st, value)) return;
			GameReflect.SetInt(st, "fatherFavLevel", value);
		}

		internal static void UnlockSkill(string skillField)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "Unlock" + skillField, st)) return;
			if (TryCall("Status", "Set" + skillField, st, 1)) return;
			GameReflect.SetInt(st, skillField, 1);
		}

		// ====================================================================
		// 专用封装：MyData 相关
		// ====================================================================

		internal static void SetSituation(int value)
		{
			Dyn m = GameReflect.MyData;
			if (!m) return;
			if (TryCall("MyData", "SetSituation", m.Obj, value)) return;
			if (TryCall("MyData", "ChangeSituation", m.Obj, value)) return;
			if (TryCall("MyData", "setSituation", m.Obj, value)) return;
			m.SI("situation", value);
		}

		internal static bool TryChangeLocation(int locationValue)
		{
			Dyn m = GameReflect.MyData;
			if (!m) return false;
			if (TryCall("MyData", "SetLocation", m.Obj, locationValue)) return true;
			if (TryCall("MyData", "MoveLocation", m.Obj, locationValue)) return true;
			if (TryCall("MyData", "ChangeLocation", m.Obj, locationValue)) return true;
			if (TryCall("MyData", "Teleport", m.Obj, locationValue)) return true;
			return false;
		}

		internal static void SetCurriculumSessions(int value)
		{
			object st = GameReflect.GetStatus();
			if (st == null) return;
			if (TryCall("Status", "SetCurriculumSessions", st, value)) return;
			if (TryCall("Status", "setCurriculumSessions", st, value)) return;
			GameReflect.SetInt(st, "curriculumSessions", value);
		}

		internal static void CompleteCurriculum(Dyn curriculum)
		{
			if (!curriculum) return;
			Dyn cd = curriculum.O("data");
			if (!cd) return;
			if (TryCall("CurriculumData", "Complete", cd.Obj)) return;
			if (TryCall("CurriculumData", "SetComplete", cd.Obj, true)) return;
			cd.SI("isComplete", 1);
			cd.SI("isActive", 1);
			cd.SI("restHP", 999);
		}

		// ====================================================================
		// 专用封装：ItemData 相关
		// ====================================================================

		internal static void SetItemCount(Dyn item, int count)
		{
			Dyn data = item.O("data");
			if (!data) return;
			if (TryCall("ItemData", "SetCount", data.Obj, count)) return;
			if (TryCall("ItemData", "setCount", data.Obj, count)) return;
			if (TryCall("ItemData", "SetAmount", data.Obj, count)) return;
			data.SI("count", count);
		}

		// ====================================================================
		// 专用封装：FriendData 相关
		// ====================================================================

		internal static void SetFriendField(Dyn friendData, string field, int value)
		{
			if (!friendData) return;
			if (string.IsNullOrEmpty(field)) return;
			string capField = char.ToUpper(field[0]) + field.Substring(1);
			if (TryCall("FriendData", "Set" + capField, friendData.Obj, value)) return;
			if (TryCall("FriendData", "set" + capField, friendData.Obj, value)) return;
			friendData.SI(field, value);
		}

		internal static void SetFriendMax(Dyn friendData)
		{
			if (!friendData) return;
			SetFriendField(friendData, "fMeet", 100);
			SetFriendField(friendData, "fFavarite", 100);
			SetFriendField(friendData, "fLoveEvents", 5);
		}

		/// <summary>
		/// 获取扫描到的诊断报告
		/// </summary>
		internal static string GetScanReport()
		{
			if (!_scanned) Scan();
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine("=== GameMethodResolver 扫描报告 ===");
			foreach (var kv in _methodsByType)
			{
				sb.AppendLine(kv.Key + ": " + kv.Value.Count + " 个方法");
				foreach (string name in kv.Value.Keys)
				{
					sb.AppendLine("  " + name);
				}
			}
			return sb.ToString();
		}
	}
}
