using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabLocation
	{
		internal static void Render()
		{
			ModMenu.Section("地点传送");
			ModMenu.TwoCol(delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("当前位置");
					ModMenu.Gap(2f);
					Dyn m = GameReflect.MyData;
					if (m)
					{
						ModMenu.ValueLabel("地点 " + m.S("location"));
						ModMenu.Label("时刻 " + m.S("situation"));
						ModMenu.Label("父地点 " + m.S("locationParent"));
					}
					else
					{
						ModMenu.Label("等待游戏加载...");
					}
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("传送说明");
					ModMenu.Gap(2f);
					ModMenu.Label("LocationType 枚举值:");
					ModMenu.Label("HOME=2  CENTRAL=3  ACADEMY=4");
					ModMenu.Label("BAKERY=6  CAFETERIA=7  WEAPON=8");
					ModMenu.Label("DRESS=9  SUBURB=5");
					ModMenu.Gap(4f);
					ModMenu.Label("自动通过游戏方法传送，兜底直接写字段");
				});
			}, delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("快速传送");
					ModMenu.Gap(2f);
					if (ModMenu.RoseBtn("回家 HOME", 160)) { TeleportTo(2); }
					if (ModMenu.RoseBtn("中央广场 CENTRAL", 160)) { TeleportTo(3); }
					if (ModMenu.RoseBtn("学园 ACADEMY", 160)) { TeleportTo(4); }
					if (ModMenu.RoseBtn("郊外 SUBURB", 160)) { TeleportTo(5); }
					if (ModMenu.RoseBtn("面包店 BAKERY", 160)) { TeleportTo(6); }
					if (ModMenu.RoseBtn("食堂 CAFETERIA", 160)) { TeleportTo(7); }
					if (ModMenu.RoseBtn("武器店 WEAPON", 160)) { TeleportTo(8); }
					if (ModMenu.RoseBtn("服装店 DRESS", 160)) { TeleportTo(9); }
					ModMenu.Gap(4f);
					ModMenu.Label("传送后自动重置时刻为白天");
				});
			});
		}

		private static bool _teleportMethodChecked;
		private static System.Reflection.MethodInfo _changeLocationMethod;
		private static object _gameControllerInstance;

		/// <summary>
		/// 智能传送：优先调用游戏方法，兜底直接写字段
		/// </summary>
		private static void TeleportTo(int locationValue)
		{
			Dyn md = GameReflect.MyData;
			if (!md) return;

			// 扫描 GameController 中的传送方法（只做一次）
			if (!_teleportMethodChecked)
			{
				_teleportMethodChecked = true;
				Dyn gc = GameReflect.GetSingleton("GameController");
				if (gc)
				{
					_gameControllerInstance = gc.Obj;
					var type = gc.Obj.GetType();
					var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
					foreach (var m in methods)
					{
						string name = m.Name;
						if (name.Contains("ChangeLocation") || name.Contains("MoveScene") ||
							name.Contains("Teleport") || name.Contains("StartMove") ||
							name.Contains("MoveLocation") || name.Contains("GoTo"))
						{
							var parms = m.GetParameters();
							if (parms.Length == 1 && parms[0].ParameterType.IsEnum)
							{
								_changeLocationMethod = m;
								MelonLoader.MelonLogger.Msg("[传送] 找到方法: " + type.Name + "." + m.Name + "(enum)");
								break;
							}
							if (parms.Length == 1 && parms[0].ParameterType == typeof(int))
							{
								_changeLocationMethod = m;
								MelonLoader.MelonLogger.Msg("[传送] 找到方法: " + type.Name + "." + m.Name + "(int)");
								break;
							}
						}
					}
				}
			}

			bool calledMethod = false;

			// 优先：通过游戏方法传送
			if (_changeLocationMethod != null && _gameControllerInstance != null)
			{
				try
				{
					var parms = _changeLocationMethod.GetParameters();
					object arg;
					if (parms[0].ParameterType.IsEnum)
					{
						arg = System.Enum.ToObject(parms[0].ParameterType, locationValue);
					}
					else
					{
						arg = locationValue;
					}
					_changeLocationMethod.Invoke(_gameControllerInstance, new object[] { arg });
					calledMethod = true;
					MelonLoader.MelonLogger.Msg("[传送] 通过 " + _changeLocationMethod.Name + "(" + locationValue + ") 传送成功");
				}
				catch (System.Exception ex)
				{
					MelonLoader.MelonLogger.Msg("[传送] 方法调用失败: " + ex.Message + "，使用兜底");
				}
			}

			// 兜底：直接写字段
			if (!calledMethod)
			{
				md.SI("location", locationValue);
				MelonLoader.MelonLogger.Msg("[传送] 直接写入 location=" + locationValue);
			}

			// 重置时刻为白天（situation=1）
			md.SI("situation", 1);
		}
	}
}
