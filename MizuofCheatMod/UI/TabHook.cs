using MelonLoader;
using MizuofCheatMod.Harmony;
using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabHook
	{
		private static string _filter = string.Empty;
		private static Vector2 _scroll;

		internal static void Render()
		{
			ModMenu.Section("Hook 扫描与验证");

			GUILayout.BeginHorizontal();
			if (ModMenu.RoseBtn("刷新扫描", 80))
			{
				GameHookScanner.RefreshCache();
			}
			if (ModMenu.GoldBtn("运行验证", 80))
			{
				RunVerification();
			}
			_filter = GUILayout.TextField(_filter, GUIStyleBuilder.TextField,
				GUILayout.Width(150), GUILayout.Height(18));
			ModMenu.Label("过滤");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			ModMenu.Gap(4f);
			_scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			string result = GameHookScanner.GetScanResult();
			if (!string.IsNullOrEmpty(_filter))
			{
				result = FilterResult(result, _filter.Trim());
			}
			GUILayout.TextArea(result ?? "[空]", GUIStyleBuilder.TextArea, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			GUILayout.EndScrollView();

			int en = FeatureRegistry.EnabledCount;
			int tot = FeatureRegistry.TotalCount;
			ModMenu.Label("功能 [" + en + "/" + tot + "] 开启  F4=验证  F2=快照");
		}

		private static string FilterResult(string text, string filter)
		{
			if (string.IsNullOrEmpty(filter))
			{
				return text;
			}
			string[] lines = text.Split('\n');
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach (string line in lines)
			{
				if (line.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					sb.AppendLine(line);
				}
			}
			return sb.ToString();
		}

		internal static void RunVerification()
		{
			MelonLogger.Msg("");
			MelonLogger.Msg("== [Hook] MizuofCheatMod 功能验证 ==");

			Dyn m = GameReflect.MyData;
			if (m)
			{
				Dyn st = m.O("status");
				int money = st ? st.I("money") : 0;
				int stress = st ? st.I("stress") : 0;
				int period = m.I("period");
				Dyn gs = m.O("gstatus");
				int loop = gs ? gs.I("loopCount") : 0;
				Dyn items = m.O("itemDataList");
				Dyn friends = m.O("friendDataList");
				MelonLogger.Msg("  数据: 期" + period + " 金钱" + money + " 压力" + stress +
					" 周回" + loop + " 物品" + (items ? items.Count : 0) + " 好友" + (friends ? friends.Count : 0));
			}
			else
			{
				MelonLogger.Msg("  数据: MyData 不可用 (进入游戏场景后重试)");
			}

			Dyn ac = GameReflect.GetSingleton("AchievementController");
			Dyn bc = GameReflect.GetSingleton("BattleController");
			MelonLogger.Msg("  AchievementController: " + (ac ? "可用" : "不可用"));
			MelonLogger.Msg("  BattleController: " + (bc ? "可用" : "不可用"));

			int en = FeatureRegistry.EnabledCount;
			int tot = FeatureRegistry.TotalCount;
			MelonLogger.Msg("  Patch: " + (PatchController.IsApplied ? "已注册" : "未注册"));
			MelonLogger.Msg("  功能: " + en + "/" + tot + " 开启  " + FeatureRegistry.ImplementedCount + " 已实现");

			if (en > 0)
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder("  当前开启: ");
				foreach (var kv in FeatureRegistry.GetAllFeatures())
				{
					if (kv.Value.Enabled)
					{
						sb.Append(kv.Value.DisplayName);
						sb.Append(" ");
					}
				}
				MelonLogger.Msg(sb.ToString());
			}
			MelonLogger.Msg("========================");
		}
	}
}
