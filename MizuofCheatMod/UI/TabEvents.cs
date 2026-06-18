using MelonLoader;
using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabEvents
	{
		private static string _eventIdBuf = string.Empty;
		private static Vector2 _endingScroll;
		private static int _selectedEndingJob = -1;

		// 已知成就/事件ID范围参考
		private static readonly string[] KnownEventRanges = {
			"成就ID: 0-103", "一般事件: 1000+", "角色事件: 2000+"
		};

		internal static void Render()
		{
			ModMenu.Section("结局与事件");
			ModMenu.TwoCol(delegate
			{
				// === 结局触发 ===
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("结局直接触发");
					ModMenu.Gap(2f);

					// 显示当前结局信息
					Dyn ed = GameReflect.MyData.O("endingJob");
					if (ed)
					{
						ModMenu.Label("当前结局: " + ed.S("name") + " (ID:" + ed.I("jobId") + ")");
					}
					else
					{
						ModMenu.Label("当前无结局数据");
					}
					ModMenu.Gap(4f);

					// 列出所有可选结局
					ModMenu.BoldLabel("可选结局列表 (点击选择)");
					ModMenu.Gap(2f);
					_endingScroll = GUILayout.BeginScrollView(_endingScroll, GUILayout.Height(180));
					Dyn endingJobs = GameReflect.MyData.O("endingJobDataList");
					if (endingJobs)
					{
						for (int i = 0; i < endingJobs.Count; i++)
						{
							string jName = endingJobs[i].S("name");
							int jId = endingJobs[i].I("jobId");
							bool selected = (i == _selectedEndingJob);
							Color prev = GUI.backgroundColor;
							if (selected)
							{
								GUI.backgroundColor = Color.green;
							}
							if (GUILayout.Button("[" + jId + "] " + jName,
								GUIStyleBuilder.PillBtn, GUILayout.Height(20)))
							{
								_selectedEndingJob = i;
							}
							GUI.backgroundColor = prev;
						}
					}
					else
					{
						ModMenu.Label("(无结局数据)");
					}
					GUILayout.EndScrollView();

					ModMenu.Gap(4f);

					// 触发按钮
					if (_selectedEndingJob >= 0 && endingJobs && _selectedEndingJob < endingJobs.Count)
					{
						if (ModMenu.GoldBtn("设定此结局并触发", 180))
						{
							// 1. 将选定结局写入 MyData.endingJob
							object jobObj = endingJobs[_selectedEndingJob].Obj;
							if (jobObj != null)
							{
								Utils.GameReflect.SetField(GameReflect.MyData.Obj, "endingJob", jobObj);
								MelonLogger.Msg("[结局] 已设定结局: " + endingJobs[_selectedEndingJob].S("name"));
							}

							// 2. 设置 ending flag
							Dyn gs = GameReflect.GStatus;
							if (gs)
							{
								gs.SI("isTrueEndingUnlocked", 1);
							}

							// 3. 尝试多种方式触发结局场景
							Dyn gc = GameReflect.GetSingleton("GameController");
							bool triggered = false;

							// 方式A: GameController.LoadEndingDemoScene
							if (gc)
							{
								object result = GameReflect.Call(gc.Obj, "LoadEndingDemoScene");
								if (result != null || true)
								{
									triggered = true;
									MelonLogger.Msg("[结局] GameController.LoadEndingDemoScene() 已调用");
								}
							}

							// 方式B: 通过 StoryDemoScene 直接设置并播放结局
							if (!triggered)
							{
								Dyn sds = GameReflect.GetSingleton("StoryDemoScene");
								if (sds)
								{
									sds.CM("SetEndingDemoScene");
									triggered = true;
									MelonLogger.Msg("[结局] StoryDemoScene.SetEndingDemoScene() 已调用");
								}
							}

							// 方式C: EndingController 初始化
							if (!triggered)
							{
								Dyn ec = GameReflect.GetSingleton("EndingController");
								if (ec)
								{
									ec.CM("Init");
									triggered = true;
									MelonLogger.Msg("[结局] EndingController.Init() 已调用");
								}
							}

							// 方式D: Unity SceneManager 直接加载结局场景
							if (!triggered)
							{
								try
								{
									string[] sceneCandidates = { "Ending", "BadEnd", "EndingScene", "StaffRoll" };
									foreach (string sc in sceneCandidates)
									{
										try
										{
											UnityEngine.SceneManagement.SceneManager.LoadScene(sc);
											triggered = true;
											MelonLogger.Msg("[结局] SceneManager.LoadScene(" + sc + ") 已调用");
											break;
										}
										catch { }
									}
								}
								catch { }
							}

							// 方式E: CloseMonth 推进月结判定
							if (!triggered)
							{
								bool wasFrozen = FeatureRegistry.IsEnabled("time_freeze");
								if (wasFrozen) FeatureRegistry.SetEnabled("time_freeze", false);
								if (gc)
								{
									gc.CM("CloseMonth");
									MelonLogger.Msg("[结局] GameController.CloseMonth() 已调用");
								}
								if (wasFrozen) FeatureRegistry.SetEnabled("time_freeze", true);
								triggered = true;
							}

							ModMenu.Label("已触发");
						}
					}
					else
					{
						ModMenu.Label("请先在列表中选择一个结局");
					}
				});

				// === 技能点管理 ===
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("技能点管理");
					ModMenu.Gap(2f);
					Dyn s = GameReflect.Status;
					if (s)
					{
						ModMenu.Label("体力技能 " + (s.I("isUnlockSkillPhysical") > 0 ? "已解锁" : "未解锁"));
						ModMenu.Label("知性技能 " + (s.I("isUnlockSkillIntelligence") > 0 ? "已解锁" : "未解锁"));
						ModMenu.Label("魅力技能 " + (s.I("isUnlockSkillCharm") > 0 ? "已解锁" : "未解锁"));
						ModMenu.Label("感性技能 " + (s.I("isUnlockSkillSense") > 0 ? "已解锁" : "未解锁"));
						ModMenu.Gap(2f);
						if (ModMenu.GoldBtn("全部点亮", 100))
						{
							s.SI("isUnlockSkillPhysical", 1);
							s.SI("isUnlockSkillIntelligence", 1);
							s.SI("isUnlockSkillCharm", 1);
							s.SI("isUnlockSkillSense", 1);
							// 同时点亮技能列表中所有技能
							Dyn skills = GameReflect.MyData.O("skillDataList");
							if (skills)
							{
								for (int i = 0; i < skills.Count; i++)
								{
									skills[i].O("data").SI("isOpened", 1);
									skills[i].O("data").SI("isLearned", 1);
								}
								ModMenu.Label("共点亮 " + skills.Count + " 个技能");
							}
						}
					}
				});
			}, delegate
			{
				// === 剧情事件触发 ===
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("剧情事件直接触发");
					ModMenu.Gap(2f);
					ModMenu.Label("输入事件/成就ID后点击触发");
					GUILayout.BeginHorizontal();
					_eventIdBuf = GUILayout.TextField(_eventIdBuf ?? string.Empty,
						GUIStyleBuilder.TextField, GUILayout.Width(100), GUILayout.Height(18));
					if (ModMenu.RoseBtn("触发", 50))
					{
						if (int.TryParse(_eventIdBuf.Trim(), out int id))
						{
							Dyn ac = GameReflect.GetSingleton("AchievementController");
							if (ac)
							{
								ac.CM("Unlock", id);
								ModMenu.Label("已尝试触发 ID=" + id);
							}
							else
							{
								ModMenu.Label("AchievementController 不可用");
							}
						}
					}
					GUILayout.EndHorizontal();
					ModMenu.Gap(4f);

					// 已知事件范围参考
					ModMenu.BoldLabel("已知参考ID范围");
					foreach (string range in KnownEventRanges)
					{
						ModMenu.Label("  " + range);
					}
				});

				// === 成就操作快捷 ===
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("成就快捷操作");
					ModMenu.Gap(2f);
					if (ModMenu.GoldBtn("解锁全部成就(0-103)", 180))
					{
						Dyn ac = GameReflect.GetSingleton("AchievementController");
						if (ac)
						{
							for (int i = 0; i <= 103; i++)
							{
								ac.CM("Unlock", i);
							}
							ModMenu.Label("已尝试解锁0-103");
						}
					}
					ModMenu.Gap(2f);
					if (ModMenu.CoralBtn("重置成就(清空已解锁)", 180))
					{
						Dyn m = GameReflect.MyData;
						if (m)
						{
							Dyn gs = m.O("gstatus");
							if (gs)
							{
								// 清空已解锁列表
								var unlockedList = gs.O("acvUnlockedIds");
								if (unlockedList != null && unlockedList.Obj is System.Collections.IList list)
								{
									list.Clear();
									ModMenu.Label("已清空");
								}
							}
						}
					}
				});
			});
		}
	}
}