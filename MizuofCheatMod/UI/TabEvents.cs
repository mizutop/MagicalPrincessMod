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
					ModMenu.BoldLabel("结局控制");
					ModMenu.Gap(2f);

					// 结局替换开关（最多替换6个）
					bool allJobs = FeatureRegistry.IsEnabled("ending_all_jobs");
					if (ModMenu.Toggle("替换结局选择面板", allJobs,
						"开启后游戏的结局选择界面只显示你选的 ≤6 个结局"))
					{
						bool newVal = !allJobs;
						FeatureRegistry.SetEnabled("ending_all_jobs", newVal);
						if (!newVal)
						{
							Harmony.PatchController.SetCustomJobIds(null);
						}
					}
					if (allJobs)
					{
						int count = Harmony.PatchController.GetCustomJobIds().Count;
						ModMenu.Label("已选 " + count + " / 6 个结局");
					}
					else
					{
						ModMenu.Label("关闭时使用游戏原本的结局判定");
					}
					ModMenu.Gap(2f);

					// 显示当前周期和结局信息
					Dyn mdInfo = GameReflect.MyData;
					if (mdInfo)
					{
						int curPeriod = mdInfo.I("period");
						Dyn pdl = mdInfo.O("periodDataList");
						int maxP = pdl ? pdl.Count : 0;
						ModMenu.Label("当前周期: " + curPeriod + " / " + (maxP > 0 ? maxP.ToString() : "?"));
					}
					ModMenu.Gap(2f);

					Dyn ed = GameReflect.MyData.O("endingJob");
					if (ed && ed.Obj != null)
					{
						ModMenu.Label("已设结局: " + ed.S("name") + " (ID:" + ed.I("jobId") + ")");
					}
					else
					{
						ModMenu.Label("未设定结局");
					}
					ModMenu.Gap(4f);

					// 列出所有可选结局（多选，最多6个）
					ModMenu.BoldLabel("全部结局列表 (勾选替换，最多6个)");
					ModMenu.Gap(2f);
					var savedIds = new System.Collections.Generic.HashSet<int>(
						Harmony.PatchController.GetCustomJobIds());
					_endingScroll = GUILayout.BeginScrollView(_endingScroll, GUILayout.Height(200));
					Dyn endingJobs = GameReflect.MyData.O("endingJobDataList");
					if (endingJobs)
					{
						for (int i = 0; i < endingJobs.Count; i++)
						{
							string jName = endingJobs[i].S("name");
							int jId = endingJobs[i].I("jobId");
							bool isChecked = savedIds.Contains(jId);
							Color prev = GUI.backgroundColor;
							if (isChecked)
							{
								GUI.backgroundColor = Color.green;
							}
							if (GUILayout.Button((isChecked ? "✓ " : "  ") + "[" + jId + "] " + jName,
								GUIStyleBuilder.PillBtn, GUILayout.Height(20)))
							{
								if (allJobs)
								{
									var current = Harmony.PatchController.GetCustomJobIds();
									if (current.Contains(jId))
									{
										current.Remove(jId);
									}
									else if (current.Count < 6)
									{
										current.Add(jId);
									}
									Harmony.PatchController.SetCustomJobIds(current);
								}
								else
								{
									_selectedEndingJob = i;
								}
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

					// 触发 / 应用按钮
					bool eaJobs = FeatureRegistry.IsEnabled("ending_all_jobs");
					if (eaJobs)
					{
						int cnt = Harmony.PatchController.GetCustomJobIds().Count;
						if (cnt > 0)
						{
							ModMenu.Label("已选 " + cnt + " 个结局，进入游戏毕业流程即可看到替换效果");
						}
						else
						{
							ModMenu.Label("请勾选最多6个结局来替换游戏的选择面板");
						}
					}
					else if (_selectedEndingJob >= 0 && endingJobs && _selectedEndingJob < endingJobs.Count)
					{
						if (ModMenu.GoldBtn("设定此结局并触发", 180))
						{
							object jobObj = endingJobs[_selectedEndingJob].Obj;
							int jId = endingJobs[_selectedEndingJob].I("jobId");
							string jName = endingJobs[_selectedEndingJob].S("name");

							bool triggered = EndingTriggerDiscovery.TriggerEnding(jobObj, jId);
							Dyn md = GameReflect.MyData;
							if (md)
							{
								int after = md.I("period");
								Dyn ej = md.O("endingJob");
								string ejName = ej ? ej.S("name") : "(无)";
								ModMenu.Label("结局: " + ejName + " | period: " + after + " | " + (triggered ? "[触发]" : "[未触发]"));
							}
							else
							{
								ModMenu.Label(triggered ? "结局已触发" : "触发失败");
							}
						}
					}
					else
					{
						ModMenu.Label("在列表中点击一个结局→触发; 或开启替换模式选多个");
					}
				});

				// === 快速结局（剩余回合=1） ===
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("快速触发结局");
					ModMenu.Gap(2f);
					ModMenu.Label("无需选择角色，直接推进到游戏终点");
					ModMenu.Gap(2f);
					if (ModMenu.CoralBtn("强制推进触发结局", 220))
					{
						bool triggered = EndingTriggerDiscovery.QuickTriggerEnding();
						Dyn md = GameReflect.MyData;
						if (md)
						{
							int after = md.I("period");
							ModMenu.Label("已执行 (period: " + after + ") " + (triggered ? "结局已触发" : "可能未触发"));
						}
						else
						{
							ModMenu.Label("已执行" + (triggered ? " [触发]" : " [可能未触发]"));
						}
					}
					ModMenu.Label("将推进到最终周期，触发游戏自然结局判定");
					ModMenu.Label("配合「结局控制」选择具体结局一起使用");
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
							GameMethodResolver.UnlockSkill("isUnlockSkillPhysical");
							GameMethodResolver.UnlockSkill("isUnlockSkillIntelligence");
							GameMethodResolver.UnlockSkill("isUnlockSkillCharm");
							GameMethodResolver.UnlockSkill("isUnlockSkillSense");
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

				// === 结局诊断 ===
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("结局诊断工具");
					ModMenu.Gap(2f);
					if (ModMenu.GoldBtn("诊断结局API", 150))
					{
						EndingTriggerDiscovery.Discover();
						ModMenu.Label("诊断完成，请查看 MelonLoader 日志");
					}
					ModMenu.Label("扫描 MyData/GStatus/PeriodData 所有字段");
					ModMenu.Label("分析周期上限和剩余回合相关数据");
					ModMenu.Label("结果输出到日志窗口");
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