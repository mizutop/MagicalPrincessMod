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
					ModMenu.Label("通过修改 MyData.location 实现传送");
					ModMenu.Label("LocationType 枚举值:");
					ModMenu.Label("HOME=2  CENTRAL=3  ACADEMY=4");
					ModMenu.Label("BAKERY=6  CAFETERIA=7  WEAPON=8");
					ModMenu.Label("DRESS=9  SUBURB=5");
				});
			}, delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("快速传送");
					ModMenu.Gap(2f);
					if (ModMenu.RoseBtn("回家 HOME", 160))
					{
						GameReflect.MyData.SI("location", 2);
					}
					if (ModMenu.RoseBtn("中央广场 CENTRAL", 160))
					{
						GameReflect.MyData.SI("location", 3);
					}
					if (ModMenu.RoseBtn("学园 ACADEMY", 160))
					{
						GameReflect.MyData.SI("location", 4);
					}
					if (ModMenu.RoseBtn("郊外 SUBURB", 160))
					{
						GameReflect.MyData.SI("location", 5);
					}
					if (ModMenu.RoseBtn("面包店 BAKERY", 160))
					{
						GameReflect.MyData.SI("location", 6);
					}
					if (ModMenu.RoseBtn("食堂 CAFETERIA", 160))
					{
						GameReflect.MyData.SI("location", 7);
					}
					if (ModMenu.RoseBtn("武器店 WEAPON", 160))
					{
						GameReflect.MyData.SI("location", 8);
					}
					if (ModMenu.RoseBtn("服装店 DRESS", 160))
					{
						GameReflect.MyData.SI("location", 9);
					}
					ModMenu.Gap(4f);
					ModMenu.Label("传送后可能需要切换场景触发刷新");
				});
			});
		}
	}
}
