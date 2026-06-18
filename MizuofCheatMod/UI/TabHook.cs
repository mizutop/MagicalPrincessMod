using MelonLoader;
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
			ModMenu.Section("运行时类型扫描与验证");

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
			int vf = FeatureRegistry.VerifiedCount;
			ModMenu.Label("功能状态 [" + en + "/" + tot + "] 开启  [" + vf + "] 已验证");
			ModMenu.Label("按 F4 快速运行验证, 按 F2 导出详细报告");
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
			MelonLogger.Msg("===========================================");
			MelonLogger.Msg("[Hook Tab] MizuofCheatMod 功能验证报告");
			MelonLogger.Msg("===========================================");
			MelonLogger.Msg("");
			MelonLogger.Msg("--- 运行时数据 ---");
			Dyn m = GameReflect.MyData;
			if (m)
			{
				MelonLogger.Msg("  [OK] MyData period=" + m.I("period"));
				Dyn st = m.O("status");
				if (st)
				{
					MelonLogger.Msg("  [OK] Status money=" + st.I("money") + " stress=" + st.I("stress"));
				}
				Dyn gs = m.O("gstatus");
				if (gs)
				{
					MelonLogger.Msg("  [OK] GStatus loop=" + gs.I("loopCount"));
				}
				Dyn items = m.O("itemDataList");
				if (items)
				{
					MelonLogger.Msg("  [OK] Items x" + items.Count);
				}
				Dyn friends = m.O("friendDataList");
				if (friends)
				{
					MelonLogger.Msg("  [OK] Friends x" + friends.Count);
				}
			}
			else
			{
				MelonLogger.Msg("  [--] MyData 不可用 (请进入游戏场景后重试)");
			}
			Dyn ac = GameReflect.GetSingleton("AchievementController");
			if (ac)
			{
				MelonLogger.Msg("  [OK] AchievementController");
			}
			Dyn bc = GameReflect.GetSingleton("BattleController");
			if (bc)
			{
				MelonLogger.Msg("  [OK] BattleController");
			}

			MelonLogger.Msg("");
			MelonLogger.Msg("--- 功能实现/生效/未完成 ---");
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach (var kv in FeatureRegistry.GetAllFeatures())
			{
				FeatureEntry fe = kv.Value;
				string statusIcon;
				switch (fe.Status)
				{
					case FeatureStatus.Implemented:
						statusIcon = "已实现";
						break;
					case FeatureStatus.Partial:
						statusIcon = "部分";
						break;
					case FeatureStatus.Unimplemented:
						statusIcon = "未完成";
						break;
					default:
						statusIcon = "未知";
						break;
				}
				string enabledIcon = fe.Enabled ? "生效" : "关闭";
				string verifIcon = fe.Verified ? "已测" : "待测";
				sb.Append("  [");
				sb.Append(enabledIcon);
				sb.Append("][");
				sb.Append(statusIcon);
				sb.Append("][");
				sb.Append(verifIcon);
				sb.Append("] ");
				sb.Append(fe.Key);
				sb.Append(" (");
				sb.Append(fe.DisplayName);
				sb.AppendLine(")");
			}
			MelonLogger.Msg(sb.ToString().TrimEnd());

			MelonLogger.Msg("--- 汇总 ---");
			MelonLogger.Msg("  功能总数: " + FeatureRegistry.TotalCount);
			MelonLogger.Msg("  已实现: " + FeatureRegistry.ImplementedCount);
			MelonLogger.Msg("  部分实现: " + FeatureRegistry.PartialCount);
			MelonLogger.Msg("  未完成: " + FeatureRegistry.UnimplementedCount);
			MelonLogger.Msg("  当前生效: " + FeatureRegistry.EnabledCount);
			MelonLogger.Msg("  已验证: " + FeatureRegistry.VerifiedCount);
			MelonLogger.Msg("===========================================");
		}
	}
}
