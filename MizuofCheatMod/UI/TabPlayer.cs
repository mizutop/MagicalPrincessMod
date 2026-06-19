using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabPlayer
	{
		private static string _b1 = string.Empty;
		private static string _b2 = string.Empty;
		private static string _b3 = string.Empty;
		private static bool _statLock;

		// 16 项核心属性输入缓冲区
		private static string _bufPK = string.Empty, _bufPS = string.Empty;
		private static string _bufPKo = string.Empty, _bufPBi = string.Empty;
		private static string _bufIBu = string.Empty, _bufISa = string.Empty;
		private static string _bufIMa = string.Empty, _bufISh = string.Empty;
		private static string _bufCBi = string.Empty, _bufCSh = string.Empty;
		private static string _bufCRe = string.Empty, _bufCDo = string.Empty;
		private static string _bufSSo = string.Empty, _bufSSa = string.Empty;
		private static string _bufSOn = string.Empty, _bufSBi = string.Empty;

		// 等级/倍率输入缓冲区
		private static string _bufLvPhy = string.Empty, _bufLvInt = string.Empty;
		private static string _bufLvCha = string.Empty, _bufLvSen = string.Empty;
		private static string _bufLvBat = string.Empty, _bufLvMag = string.Empty;
		private static string _bufSal = string.Empty, _bufBuy = string.Empty, _bufBatM = string.Empty;

		// 父亲好感/名声/善恶/黑币输入缓冲区
		private static string _bufFav = string.Empty, _bufFavLv = string.Empty, _bufRep = string.Empty;
		private static string _bufGood = string.Empty, _bufBad = string.Empty, _bufBal = string.Empty;
		private static string _bufBCoin = string.Empty, _bufAPMax = string.Empty;

		internal static void Render()
		{
			ModMenu.Section("玩家属性");
			ModMenu.TwoCol(delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("基础属性");
					ModMenu.Gap(2f);
					Dyn st = GameReflect.Status;
					if (st)
					{
						ModMenu.SmartInputRow("金钱", st, "money", ref _b1, v => GameMethodResolver.SetMoney(v));
						ModMenu.SmartInputRow("压力", st, "stress", ref _b2, v => GameMethodResolver.SetStress(v));
						ModMenu.SmartInputRow("名声", st, "reputation", ref _bufRep, v => GameMethodResolver.SetReputation(v));
						ModMenu.SmartInputRow("善恶(善行)", st, "goodAction", ref _bufGood, v => GameMethodResolver.SetGoodAction(v));
						ModMenu.SmartInputRow("善恶(恶行)", st, "badAction", ref _bufBad, v => GameMethodResolver.SetBadAction(v));
						ModMenu.SmartInputRow("善恶平衡", st, "gbBalance", ref _bufBal, v => GameMethodResolver.SetBalance(v));
						ModMenu.Gap(2f);
						ModMenu.SmartInputRow("行动力上限", st, "activePowerMax", ref _bufAPMax, v => GameMethodResolver.SetActivePowerMax(v));
						int ap = st.I("activePower");
						int max = st.I("activePowerMax");
						ModMenu.Label("行动力  " + ap + " / " + max);
						GUILayout.BeginHorizontal();
						ModMenu.Label("行动力");
						_b3 = GUILayout.TextField(_b3, GUIStyleBuilder.TextField, GUILayout.Width(60), GUILayout.Height(18));
						if (ModMenu.RoseBtn("补满", 50))
						{
							GameMethodResolver.FillActivePower();
						}
						GUILayout.EndHorizontal();
						ModMenu.Gap(2f);
						ModMenu.SmartInputRow("东亚硬币", st, "blackCoin", ref _bufBCoin, v => GameMethodResolver.SetBlackCoin(v));
					}
					else
					{
						ModMenu.Label("等待游戏加载...");
					}
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("16项核心属性");
					ModMenu.Gap(2f);
					Dyn s = GameReflect.Status;
					if (s)
					{
						TwoColInput("筋力", "phyKinryoku", s, ref _bufPK, "生命力", "phySeimei", s, ref _bufPS);
						TwoColInput("根性", "phyKonjyo", s, ref _bufPKo, "敏捷", "phyBinsho", s, ref _bufPBi);
						TwoColInput("文学", "intBungaku", s, ref _bufIBu, "算数", "intSanjyutsu", s, ref _bufISa);
						TwoColInput("魔术", "intMajyutsu", s, ref _bufIMa, "信仰", "intShinkou", s, ref _bufISh);
						TwoColInput("美貌", "chaBibou", s, ref _bufCBi, "社交", "chaShakou", s, ref _bufCSh);
						TwoColInput("礼仪", "chaReigi", s, ref _bufCRe, "道德", "chaDoutoku", s, ref _bufCDo);
						TwoColInput("想像", "senSouzou", s, ref _bufSSo, "创作", "senSousaku", s, ref _bufSSa);
						TwoColInput("音感", "senOnkan", s, ref _bufSOn, "美感", "senBikan", s, ref _bufSBi);
						ModMenu.Gap(2f);
						if (ModMenu.GoldBtn("一键999 (全属性)", 160))
						{
							GameMethodResolver.SetAttribute("phyKinryoku", 999); GameMethodResolver.SetAttribute("phySeimei", 999);
							GameMethodResolver.SetAttribute("phyKonjyo", 999); GameMethodResolver.SetAttribute("phyBinsho", 999);
							GameMethodResolver.SetAttribute("intBungaku", 999); GameMethodResolver.SetAttribute("intSanjyutsu", 999);
							GameMethodResolver.SetAttribute("intMajyutsu", 999); GameMethodResolver.SetAttribute("intShinkou", 999);
							GameMethodResolver.SetAttribute("chaBibou", 999); GameMethodResolver.SetAttribute("chaShakou", 999);
							GameMethodResolver.SetAttribute("chaReigi", 999); GameMethodResolver.SetAttribute("chaDoutoku", 999);
							GameMethodResolver.SetAttribute("senSouzou", 999); GameMethodResolver.SetAttribute("senSousaku", 999);
							GameMethodResolver.SetAttribute("senOnkan", 999); GameMethodResolver.SetAttribute("senBikan", 999);
							ModMenu.Label("已执行");
						}
					}
				});
			}, delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("等级与倍率");
					ModMenu.Gap(2f);
					Dyn st = GameReflect.Status;
					if (st)
					{
						ModMenu.SmartInputRow("体力Lv", st, "levelPhysical", ref _bufLvPhy, v => GameMethodResolver.SetLevel("levelPhysical", v));
						ModMenu.SmartInputRow("知性Lv", st, "levelIntelligence", ref _bufLvInt, v => GameMethodResolver.SetLevel("levelIntelligence", v));
						ModMenu.SmartInputRow("魅力Lv", st, "levelCharm", ref _bufLvCha, v => GameMethodResolver.SetLevel("levelCharm", v));
						ModMenu.SmartInputRow("感性Lv", st, "levelSense", ref _bufLvSen, v => GameMethodResolver.SetLevel("levelSense", v));
						ModMenu.SmartInputRow("战斗Lv", st, "levelBattle", ref _bufLvBat, v => GameMethodResolver.SetLevel("levelBattle", v));
						ModMenu.SmartInputRow("魔法Lv", st, "levelMagic", ref _bufLvMag, v => GameMethodResolver.SetLevel("levelMagic", v));
						ModMenu.Gap(2f);
						ModMenu.SmartInputRow("工资率", st, "salaryRate", ref _bufSal, v => GameMethodResolver.SetRate("salaryRate", v));
						ModMenu.SmartInputRow("购买率", st, "buyPriceRate", ref _bufBuy, v => GameMethodResolver.SetRate("buyPriceRate", v));
						ModMenu.SmartInputRow("战斗金率", st, "battleMoneyRate", ref _bufBatM, v => GameMethodResolver.SetRate("battleMoneyRate", v));
					}
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("父亲好感");
					ModMenu.Gap(2f);
					Dyn st = GameReflect.Status;
					if (st)
					{
						ModMenu.SmartInputRow("好感度", st, "fatherFavarite", ref _bufFav, v => GameMethodResolver.SetFatherFav(v));
						ModMenu.SmartInputRow("好感等级", st, "fatherFavLevel", ref _bufFavLv, v => GameMethodResolver.SetFatherFavLevel(v));
					}
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("功能开关");
					ModMenu.Gap(2f);
					if (ModMenu.Toggle("属性锁定", _statLock, "每帧强制恢复属性值"))
					{
						_statLock = !_statLock;
						FeatureRegistry.SetEnabled("player_stat_lock", _statLock);
					}
					ModMenu.Label("开启后每帧将属性锁定为设定值");
				});
			});
		}

		/// <summary>
		/// 两列并排的可编辑输入行（使用 Resolver 调用游戏方法）
		/// </summary>
		private static void TwoColInput(string label1, string field1, Dyn stat, ref string buf1,
			string label2, string field2, Dyn stat2, ref string buf2)
		{
			GUILayout.BeginHorizontal();

			GUILayout.Label(label1, GUIStyleBuilder.ToggleText, GUILayout.Width(36));
			int v1 = stat.I(field1);
			GUILayout.Label(v1.ToString(), GUIStyleBuilder.ValueText, GUILayout.Width(36));
			buf1 = GUILayout.TextField(buf1 ?? string.Empty, GUIStyleBuilder.TextField,
				GUILayout.Width(44), GUILayout.Height(18));
			if (ModMenu.RoseBtn("S", 22) && int.TryParse(buf1, out int p1))
			{
				GameMethodResolver.SetAttribute(field1, p1);
			}

			GUILayout.Space(6);

			GUILayout.Label(label2, GUIStyleBuilder.ToggleText, GUILayout.Width(36));
			int v2 = stat2.I(field2);
			GUILayout.Label(v2.ToString(), GUIStyleBuilder.ValueText, GUILayout.Width(36));
			buf2 = GUILayout.TextField(buf2 ?? string.Empty, GUIStyleBuilder.TextField,
				GUILayout.Width(44), GUILayout.Height(18));
			if (ModMenu.RoseBtn("S", 22) && int.TryParse(buf2, out int p2))
			{
				GameMethodResolver.SetAttribute(field2, p2);
			}

			GUILayout.EndHorizontal();
		}
	}
}
