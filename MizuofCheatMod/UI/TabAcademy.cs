using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabAcademy
	{
		private static string _buf = string.Empty;
		private static Vector2 _curScroll;

		internal static void Render()
		{
			ModMenu.Section("课程与技能");
			ModMenu.TwoCol(delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("课程状态");
					ModMenu.Gap(2f);
					Dyn m = GameReflect.MyData;
					if (m)
					{
						ModMenu.Label("当前班级 " + m.S("acClass"));
						ModMenu.Label("课程次数 " + m.I("curriculumSessions"));
						ModMenu.Gap(2f);
						if (ModMenu.GoldBtn("全部课程完成", 140))
						{
							Dyn curricula = m.O("curriculumDataList");
							if (curricula)
							{
								for (int i = 0; i < curricula.Count; i++)
								{
									GameMethodResolver.CompleteCurriculum(curricula[i]);
								}
								ModMenu.Label("已完成" + curricula.Count + "门课程");
							}
						}
					}
					else
					{
						ModMenu.Label("等待游戏加载...");
					}
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("课程列表");
					ModMenu.Gap(2f);
					_curScroll = GUILayout.BeginScrollView(_curScroll, GUILayout.Height(200));
					Dyn m2 = GameReflect.MyData;
					if (m2)
					{
						Dyn curricula = m2.O("curriculumDataList");
						if (curricula)
						{
							for (int i = 0; i < curricula.Count; i++)
							{
								string cName = curricula[i].S("name");
								int cLv = curricula[i].I("level");
								string status = curricula[i].O("data").I("isComplete") > 0 ? "完成" : "进行中";
								ModMenu.Label(cName + " Lv." + cLv + " [" + status + "]");
							}
						}
						else
						{
							ModMenu.Label("(无课程数据)");
						}
					}
					GUILayout.EndScrollView();
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("技能解锁");
					ModMenu.Gap(2f);
					Dyn st = GameReflect.Status;
					if (st)
					{
						ModMenu.Label("体力技能 " + (st.I("isUnlockSkillPhysical") > 0 ? "已解锁" : "未解锁"));
						ModMenu.Label("知性技能 " + (st.I("isUnlockSkillIntelligence") > 0 ? "已解锁" : "未解锁"));
						ModMenu.Label("魅力技能 " + (st.I("isUnlockSkillCharm") > 0 ? "已解锁" : "未解锁"));
						ModMenu.Label("感性技能 " + (st.I("isUnlockSkillSense") > 0 ? "已解锁" : "未解锁"));
						ModMenu.Gap(4f);
						if (ModMenu.RoseBtn("全部技能解锁", 130))
						{
							GameMethodResolver.UnlockSkill("isUnlockSkillPhysical");
							GameMethodResolver.UnlockSkill("isUnlockSkillIntelligence");
							GameMethodResolver.UnlockSkill("isUnlockSkillCharm");
							GameMethodResolver.UnlockSkill("isUnlockSkillSense");
							// 同时点亮所有技能
							Dyn skills = GameReflect.MyData.O("skillDataList");
							if (skills)
							{
								for (int i = 0; i < skills.Count; i++)
								{
									skills[i].O("data").SI("isOpened", 1);
									skills[i].O("data").SI("isLearned", 1);
								}
							}
							ModMenu.Label("已解锁");
						}
					}
				});
			}, delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("属性提升");
					ModMenu.Gap(2f);
					if (ModMenu.GoldBtn("全属性 +10", 130))
					{
						Dyn s = GameReflect.Status;
						if (s)
						{
							GameMethodResolver.SetAttribute("phyKinryoku", s.I("phyKinryoku") + 10);
							GameMethodResolver.SetAttribute("phySeimei", s.I("phySeimei") + 10);
							GameMethodResolver.SetAttribute("phyKonjyo", s.I("phyKonjyo") + 10);
							GameMethodResolver.SetAttribute("phyBinsho", s.I("phyBinsho") + 10);
							GameMethodResolver.SetAttribute("intBungaku", s.I("intBungaku") + 10);
							GameMethodResolver.SetAttribute("intSanjyutsu", s.I("intSanjyutsu") + 10);
							GameMethodResolver.SetAttribute("intMajyutsu", s.I("intMajyutsu") + 10);
							GameMethodResolver.SetAttribute("intShinkou", s.I("intShinkou") + 10);
							GameMethodResolver.SetAttribute("chaBibou", s.I("chaBibou") + 10);
							GameMethodResolver.SetAttribute("chaShakou", s.I("chaShakou") + 10);
							GameMethodResolver.SetAttribute("chaReigi", s.I("chaReigi") + 10);
							GameMethodResolver.SetAttribute("chaDoutoku", s.I("chaDoutoku") + 10);
							GameMethodResolver.SetAttribute("senSouzou", s.I("senSouzou") + 10);
							GameMethodResolver.SetAttribute("senSousaku", s.I("senSousaku") + 10);
							GameMethodResolver.SetAttribute("senOnkan", s.I("senOnkan") + 10);
							GameMethodResolver.SetAttribute("senBikan", s.I("senBikan") + 10);
							ModMenu.Label("已执行");
						}
					}
					ModMenu.Label("16项属性各增加10点，不再受999限制");
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("课程次数");
					ModMenu.Gap(2f);
					ModMenu.SmartInputRow("设定次数", GameReflect.Status, "curriculumSessions", ref _buf, v => GameMethodResolver.SetCurriculumSessions(v));
				});
			});
		}
	}
}
