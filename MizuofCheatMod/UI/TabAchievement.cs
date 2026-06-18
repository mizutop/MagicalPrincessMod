using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabAchievement
	{
		private static string _idBuf = string.Empty;
		private static Vector2 _achScroll;

		private static Dyn AchievementController
		{
			get
			{
				return GameReflect.GetSingleton("AchievementController");
			}
		}

		internal static void Render()
		{
			ModMenu.Section("成就系统");
			ModMenu.TwoCol(delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("当前成就进度");
					ModMenu.Gap(2f);
					Dyn gs = GameReflect.GStatus;
					if (gs)
					{
						Dyn ids = gs.O("acvUnlockedIds");
						int cnt = ids ? ids.Count : 0;
						ModMenu.ValueLabel("已解锁 " + cnt + " 个成就");
						ModMenu.Label("功绩点数 " + gs.I("acvPoint") + " pt");
					}
					else
					{
						ModMenu.Label("等待游戏加载...");
					}
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("成就列表 (已完成/未完成)");
					ModMenu.Gap(2f);
					_achScroll = GUILayout.BeginScrollView(_achScroll, GUILayout.Height(300));
					Dyn gs2 = GameReflect.GStatus;
					if (gs2)
					{
						Dyn ids = gs2.O("acvUnlockedIds");
						System.Collections.Generic.HashSet<int> unlocked = new System.Collections.Generic.HashSet<int>();
						if (ids)
						{
							for (int i = 0; i < ids.Count; i++)
							{
								// acvUnlockedIds 是 List<int>，元素是原始整数而非对象
								// 不能使用 ids[i].I("")，需要用 .Obj 直接拆箱
								object raw = ids[i].Obj;
								if (raw is int intVal)
								{
									unlocked.Add(intVal);
								}
							}
						}
						int completedCount = unlocked.Count;
						for (int i = 0; i <= 103; i++)
						{
							bool done = unlocked.Contains(i);
							Color prev = GUI.contentColor;
							if (done)
							{
								GUI.contentColor = Color.green;  // 绿色=已完成
							}
							else
							{
								GUI.contentColor = Color.gray;   // 灰色=未完成
							}
							GUILayout.Label("  " + (done ? "✔" : "✗") + " 成就ID:" + i, GUIStyleBuilder.Label);
							GUI.contentColor = prev;
						}
					}
					GUILayout.EndScrollView();
				});
			}, delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("解锁成就");
					ModMenu.Gap(2f);
					GUILayout.BeginHorizontal();
					_idBuf = GUILayout.TextField(_idBuf, GUIStyleBuilder.TextField,
						GUILayout.Width(60), GUILayout.Height(18));
					if (ModMenu.GoldBtn("解锁此ID", 80))
					{
						if (int.TryParse(_idBuf, out int id) && AchievementController)
						{
							AchievementController.CM("Unlock", id);
							ModMenu.Label("已尝试解锁 " + id);
						}
					}
					GUILayout.EndHorizontal();
					ModMenu.Label("输入成就ID后点击解锁");
					ModMenu.Gap(4f);
					if (ModMenu.RoseBtn("解锁成就 ID:0 (游戏开始)", 220))
					{
						if (AchievementController)
						{
							AchievementController.CM("Unlock", 0);
						}
					}
					if (ModMenu.RoseBtn("解锁成就 ID:65 (收集100种)", 220))
					{
						if (AchievementController)
						{
							AchievementController.CM("Unlock", 65);
						}
					}
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("批量操作");
					ModMenu.Gap(2f);
					if (ModMenu.CoralBtn("[危险] 解锁全部成就 (0-103)", 220))
					{
						Dyn ac = AchievementController;
						if (ac)
						{
							for (int i = 0; i <= 103; i++)
							{
								ac.CM("Unlock", i);
							}
						}
						ModMenu.Label("已尝试解锁全部");
					}
					ModMenu.Label("解锁全部成就可能影响游戏体验");
				});
			});
		}
	}
}
