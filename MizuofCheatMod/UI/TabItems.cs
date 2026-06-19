using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabItems
	{
		private static string _search = string.Empty;
		private static Vector2 _scroll;
		private static bool _maxSell;
		private static bool _freeShop;
		private static bool _allShop;
		private static int _categoryFilter; // 0=全部 1=道具 2=衣服 3=武器防具 4=送礼用 5=重要

		private static readonly string[] CategoryLabels = {
			"全部", "道具", "衣服", "武器防具", "送礼用", "重要"
		};

		// ItemCategory 枚举值: CONSUME=0, UNCONSUME=1, RECIPE=2, WEAR=3, WEAPON=4, ARMOUR=5, IMPORTANT=6
		private static readonly int[][] CategoryMaps = {
			new[]{0,1,2,3,4,5,6}, // 全部
			new[]{0,1,2},         // 道具 (CONSUME/UNCONSUME/RECIPE)
			new[]{3},             // 衣服 (WEAR)
			new[]{4,5},           // 武器防具 (WEAPON/ARMOUR)
			null,                 // 送礼用 (特殊: gift字段非空)
			new[]{6}             // 重要 (IMPORTANT)
		};

		internal static void Render()
		{
			ModMenu.Section("物品系统");
			ModMenu.TwoCol(delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("物品搜索");
					ModMenu.Gap(2f);
					GUILayout.BeginHorizontal();
					_search = GUILayout.TextField(_search, GUIStyleBuilder.TextField,
						GUILayout.Width(200), GUILayout.Height(18));
					if (ModMenu.RoseBtn("搜索", 50))
					{
						_scroll = Vector2.zero;
					}
					GUILayout.EndHorizontal();
					ModMenu.Label("支持中英文搜索，留空显示全部");
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("分类筛选");
					ModMenu.Gap(2f);
					GUILayout.BeginHorizontal();
					for (int i = 0; i < CategoryLabels.Length; i++)
					{
						bool active = i == _categoryFilter;
						if (GUILayout.Button(CategoryLabels[i],
							active ? GUIStyleBuilder.TabActive : GUIStyleBuilder.TabNormal,
							GUILayout.Height(20)))
						{
							_categoryFilter = i;
							_scroll = Vector2.zero;
						}
					}
					GUILayout.EndHorizontal();
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("物品列表 (最多50条)");
					ModMenu.Gap(2f);
					_scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(280));
					Dyn items = GameReflect.MyData.O("itemDataList");
					if (items)
					{
						int shown = 0;
						for (int i = 0; i < items.Count && shown < 50; i++)
						{
							Dyn item = items[i];
							string name = item.S("name");
							int id = item.I("itemId");
							int cat = item.I("category");
							string giftStr = item.S("gift");

							// 分类过滤
							int[] allowed = CategoryMaps[_categoryFilter];
							if (allowed != null)
							{
								bool match = false;
								foreach (int a in allowed) { if (cat == a) { match = true; break; } }
								if (!match) continue;
							}
							else // 送礼用: gift 字段非空
							{
								if (string.IsNullOrEmpty(giftStr)) continue;
							}

							// 文字搜索过滤
							if (_search.Length > 0 && name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
							{
								continue;
							}

							int cnt = item.O("data").I("count");
							GUILayout.BeginHorizontal();
							ModMenu.Label("#" + id.ToString("D3") + " " + name);
							GUILayout.FlexibleSpace();
							ModMenu.Label("x" + cnt);
							if (ModMenu.RoseBtn("+1", 28))
							{
								GameMethodResolver.SetItemCount(item, cnt + 1);
							}
							if (ModMenu.RoseBtn("+10", 32))
							{
								GameMethodResolver.SetItemCount(item, cnt + 10);
							}
							if (ModMenu.GoldBtn("x99", 28))
							{
								GameMethodResolver.SetItemCount(item, 99);
							}
							GUILayout.EndHorizontal();
							shown++;
						}
						if (shown == 0)
						{
							ModMenu.Label("(无匹配物品)");
						}
					}
					else
					{
						ModMenu.Label("等待游戏加载...");
					}
					GUILayout.EndScrollView();
				});
			}, delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("商店作弊");
					ModMenu.Gap(2f);
					if (ModMenu.Toggle("最高售价", _maxSell, "以10倍价格出售物品"))
					{
						_maxSell = !_maxSell;
						FeatureRegistry.SetEnabled("item_max_sell", _maxSell);
					}
					ModMenu.Label("开启后所有物品以10倍价出售");
					ModMenu.Gap(4f);
					if (ModMenu.Toggle("商店免费", _freeShop, "商店物品免费获取"))
					{
						_freeShop = !_freeShop;
						FeatureRegistry.SetEnabled("item_free_shop", _freeShop);
					}
					ModMenu.Label("开启后商店购买不消耗金钱");
					ModMenu.Gap(4f);
					if (ModMenu.Toggle("商店全物品", _allShop, "武器店/服装店显示全部物品"))
					{
						_allShop = !_allShop;
						FeatureRegistry.SetEnabled("shop_all_items", _allShop);
					}
					ModMenu.Label("开启后武器店/服装店上架全部物品 + 定价0元");
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("批量操作");
					ModMenu.Gap(2f);
					if (ModMenu.GoldBtn("全部物品 x99", 150))
					{
						Dyn items = GameReflect.MyData.O("itemDataList");
						if (items)
						{
							for (int i = 0; i < items.Count; i++)
							{
								GameMethodResolver.SetItemCount(items[i], 99);
							}
							ModMenu.Label("操作完成");
						}
					}
					ModMenu.Gap(4f);
					if (ModMenu.CoralBtn("[危险] 清除全部物品", 150))
					{
						Dyn items = GameReflect.MyData.O("itemDataList");
						if (items)
						{
							for (int i = 0; i < items.Count; i++)
							{
								GameMethodResolver.SetItemCount(items[i], 0);
							}
							ModMenu.Label("已清除");
						}
					}
				});
			});
		}
	}
}
