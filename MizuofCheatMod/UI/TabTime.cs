using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabTime
	{
		private static string _buf = string.Empty;
		private static bool _frozen;

		internal static void Render()
		{
			ModMenu.Section("时间");
			ModMenu.TwoCol(delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("当前时间");
					ModMenu.Gap(2f);
					Dyn m = GameReflect.MyData;
					if (m)
					{
						ModMenu.ValueLabel("第 " + m.I("period") + " 期");
						ModMenu.Label("时刻 " + m.S("situation"));
						Dyn pd = m.O("periodDataCurrent");
						if (pd)
						{
							ModMenu.Label("年份 " + pd.I("year") + "    月份 " + pd.I("month"));
						}
					}
					else
					{
						ModMenu.Label("等待游戏加载...");
					}
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("时刻切换");
					ModMenu.Gap(2f);
					GUILayout.BeginHorizontal();
					if (ModMenu.RoseBtn("白天", 70))
					{
						GameMethodResolver.SetSituation(1);
					}
					if (ModMenu.RoseBtn("夜晚", 70))
					{
						GameMethodResolver.SetSituation(2);
					}
					if (ModMenu.RoseBtn("红月", 70))
					{
						GameMethodResolver.SetSituation(3);
					}
					GUILayout.EndHorizontal();
					ModMenu.Label("修改situation值切换时刻");
				});
			}, delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("时间跳转");
					ModMenu.Gap(2f);
					GUILayout.BeginHorizontal();
					GUILayout.Label("跳转到第", GUIStyleBuilder.ToggleText, GUILayout.Width(60));
					_buf = GUILayout.TextField(_buf ?? string.Empty, GUIStyleBuilder.TextField,
						GUILayout.Width(60), GUILayout.Height(18));
					if (ModMenu.RoseBtn("跳转", 50) && int.TryParse(_buf, out int target))
					{
						JumpToPeriod(target);
					}
					GUILayout.EndHorizontal();
					ModMenu.Label("输入目标期数后点跳转，自动推进时间");
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("功能开关");
					ModMenu.Gap(2f);
					if (ModMenu.Toggle("时间冻结", _frozen, "阻止时间自动推进"))
					{
						_frozen = !_frozen;
						FeatureRegistry.SetEnabled("time_freeze", _frozen);
					}
					ModMenu.Label("冻结后游戏内时间不会变化");
					ModMenu.Gap(4f);
					if (ModMenu.GoldBtn("执行月末处理", 150))
					{
						Dyn md = GameReflect.MyData;
						if (md)
						{
							md.CM("CloseMonth");
							ModMenu.Label("已执行");
						}
					}
					ModMenu.Label("强制触发月末结算进入下一月");
				});
			});
		}

		/// <summary>
		/// 调度时间跳转（通过 Main 的帧更新延迟执行，防止 GUI 重入）
		/// </summary>
		private static void JumpToPeriod(int targetPeriod)
		{
			Dyn md = GameReflect.MyData;
			if (!md)
			{
				ModMenu.Label("游戏数据不可用");
				return;
			}

			int current = md.I("period");
			if (targetPeriod <= current)
			{
				ModMenu.Label("目标期数必须大于当前期数 (" + current + ")");
				return;
			}

			int steps = targetPeriod - current;
			if (steps > 50)
			{
				ModMenu.Label("一次最多跳转50期，请分批操作");
				return;
			}

			Main.ScheduleTimeJump(targetPeriod);
			ModMenu.Label("已调度跳转至第 " + targetPeriod + " 期");
		}
	}
}
