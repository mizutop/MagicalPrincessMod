using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabCombat
	{
		// 战斗属性编辑缓冲区
		private static string _bufHP = string.Empty, _bufATK = string.Empty, _bufDEF = string.Empty;
		private static string _bufSPD = string.Empty, _bufBATK = string.Empty, _bufWATK = string.Empty;
		private static string _bufMDEF = string.Empty, _bufSTA = string.Empty, _bufMSTA = string.Empty;
		private static string _bufLUCK = string.Empty, _bufBMale = string.Empty, _bufBFemale = string.Empty;

		internal static void Render()
		{
			ModMenu.Section("战斗系统");
			ModMenu.TwoCol(delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("战斗模式");
					ModMenu.Gap(2f);
					bool god = FeatureRegistry.IsEnabled("combat_god");
					if (ModMenu.Toggle("无敌模式", god, "角色不会受到伤害"))
					{
						FeatureRegistry.SetEnabled("combat_god", !god);
					}
					ModMenu.Label("开启后所有伤害归零（含正式战斗）");
					ModMenu.Gap(4f);
					bool hk = FeatureRegistry.IsEnabled("combat_1hk");
					if (ModMenu.Toggle("一击必杀", hk, "攻击即秒杀敌人"))
					{
						FeatureRegistry.SetEnabled("combat_1hk", !hk);
					}
					ModMenu.Label("开启后对敌人造成99999伤害（含正式战斗）");
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("当前战斗状态");
					ModMenu.Gap(2f);
					Dyn bc = GameReflect.GetSingleton("BattleController");
					if (bc)
					{
						ModMenu.Label("战斗控制器: 可用");
					}
					else
					{
						ModMenu.Label("战斗未开始或无战斗场景");
					}
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("战斗结算");
					ModMenu.Gap(2f);
					if (ModMenu.GoldBtn("立即胜利", 180))
					{
						Dyn bc = GameReflect.GetSingleton("BattleController");
						if (bc)
						{
							bc.CM("ToBattleFinishReady", true);
							ModMenu.Label("已执行");
						}
						else
						{
							ModMenu.Label("错误: 不在战斗场景中");
						}
					}
					ModMenu.Label("仅在战斗场景中生效");
				});
			}, delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("爱丽丝战斗力（可编辑）");
					ModMenu.Gap(2f);
					Dyn bs = GameReflect.MyData.O("bstatus");
					if (bs)
					{
						FloatRow("HP", bs, "hp", ref _bufHP);
						FloatRow("攻击力", bs, "atk", ref _bufATK);
						FloatRow("防御力", bs, "defence", ref _bufDEF);
						FloatRow("速度", bs, "speed", ref _bufSPD);
						FloatRow("体力", bs, "stamina", ref _bufSTA);
						FloatRow("黑魔法", bs, "batk", ref _bufBATK);
						FloatRow("白魔法", bs, "watk", ref _bufWATK);
						FloatRow("魔法抗性", bs, "mdefence", ref _bufMDEF);
						FloatRow("咏唱速度", bs, "mstamina", ref _bufMSTA);
						FloatRow("幸运", bs, "luck", ref _bufLUCK);
						FloatRow("男性增益", bs, "buffMale", ref _bufBMale);
						FloatRow("女性增益", bs, "buffFemale", ref _bufBFemale);
						ModMenu.Gap(2f);
						if (ModMenu.GoldBtn("战斗属性一键999", 180))
						{
							bs.SF("hp", 9999); bs.SF("atk", 999); bs.SF("defence", 999);
							bs.SF("speed", 999); bs.SF("stamina", 999); bs.SF("batk", 999);
							bs.SF("watk", 999); bs.SF("mdefence", 999); bs.SF("mstamina", 999);
							bs.SF("luck", 999); bs.SF("buffMale", 999); bs.SF("buffFemale", 999);
							ModMenu.Label("已执行");
						}
					}
					else
					{
						ModMenu.Label("等待战斗数据加载...");
					}
				});
			});
		}

		/// <summary>
		/// 浮点数版输入行：读取用 F() 写入用 SF()，支持小数
		/// </summary>
		private static void FloatRow(string label, Dyn target, string field, ref string buf)
		{
			GUILayout.BeginHorizontal(GUILayout.Height(20));
			GUILayout.Label(label, GUIStyleBuilder.ToggleText, GUILayout.Width(60));
			if (target?.Obj != null)
			{
				float val = target.F(field);
				GUILayout.Label(val.ToString("F1"), GUIStyleBuilder.ValueText, GUILayout.Width(50));
			}
			buf = GUILayout.TextField(buf ?? string.Empty, GUIStyleBuilder.TextField,
				GUILayout.Width(60), GUILayout.Height(18));
			if (ModMenu.RoseBtn("Set", 36) && target?.Obj != null && float.TryParse(buf, out float parsed))
			{
				target.SF(field, parsed);
			}
			GUILayout.EndHorizontal();
		}
	}
}