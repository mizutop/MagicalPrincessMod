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
			ModMenu.Section("时间控制");
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
						GameReflect.MyData.SI("situation", 1);
					}
					if (ModMenu.RoseBtn("夜晚", 70))
					{
						GameReflect.MyData.SI("situation", 2);
					}
					if (ModMenu.RoseBtn("红月", 70))
					{
						GameReflect.MyData.SI("situation", 3);
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
					ModMenu.InputRow("跳转到第", GameReflect.MyData, "period", ref _buf);
					ModMenu.Label("输入期数(0-42)后点 Set");
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
	}
}
