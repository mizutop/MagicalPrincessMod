using MelonLoader;
using MizuofCheatMod.Harmony;
using MizuofCheatMod.UI;
using MizuofCheatMod.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(MizuofCheatMod.Main), "MizuofCheatMod", "1.0.1", "Mizuof")]
[assembly: MelonGame("Neotro Inc.", "MagicalPrincess")]

namespace MizuofCheatMod
{
	public class Main : MelonMod
	{
		internal static Main Instance { get; private set; }
		internal static bool PanelVisible { get; set; }

		private bool _guiInitialized;
		private float _lastScan;
		private bool _scanDone;

		// 属性锁定缓存（默认值，每帧从游戏读取 activePowerMax 覆写）
		private int _lockMoney = 50000;
		private int _lockStress = 0;
		private int _lockAP;

		public override void OnInitializeMelon()
		{
			Instance = this;
			ModConfig.Load();
			FeatureRegistry.InitDefaults();
			MelonLogger.Msg("MizuofCheatMod for Magical Princess v1.0.1");
			MelonLogger.Msg("F1=面板  F2=快照  F3=Hook  F4=验证");
		}

		public override void OnUpdate()
		{
			if (!_scanDone && Time.realtimeSinceStartup - _lastScan > 3f)
			{
				_lastScan = Time.realtimeSinceStartup;
				GameHookScanner.RefreshCache();
				if (GameHookScanner.IsAssemblyLoaded)
				{
					_scanDone = true;
					PatchController.ApplyAll();
					MelonLogger.Msg("[Auto] 扫描完毕，Patch已注册");
				}
			}

			if (Input.GetKeyDown(KeyCode.F1))
			{
				PanelVisible = !PanelVisible;
				_guiInitialized = false;
			}
			if (Input.GetKeyDown(KeyCode.F2))
			{
				DumpSnapshot();
			}
			if (Input.GetKeyDown(KeyCode.F3))
			{
				PanelVisible = true;
				_guiInitialized = false;
				GameHookScanner.RefreshCache();
				ModMenu.SetActiveTab(8);
			}
			if (Input.GetKeyDown(KeyCode.F4))
			{
				RunVerification();
			}
			if (Input.GetKeyDown(KeyCode.F5))
			{
				// 快捷键: 立即获胜
				Dyn bc = GameReflect.GetSingleton("BattleController");
				if (bc)
				{
					bc.CM("ToBattleFinishReady", true);
					MelonLogger.Msg("[F5] 立即获胜已触发");
				}
			}

			// 每帧执行功能循环
			ExecuteFrameFeatures();
		}

		public override void OnGUI()
		{
			if (!PanelVisible)
			{
				return;
			}
			if (!_guiInitialized)
			{
				GUIStyleBuilder.Init();
				_guiInitialized = true;
			}
			ModMenu.RenderMainWindow();
		}

		public override void OnApplicationQuit()
		{
			PatchController.RemoveAll();
			GUIStyleBuilder.Cleanup();
		}

		private void ExecuteFrameFeatures()
		{
			Dyn status = GameReflect.Status;
			if (!status)
			{
				return;
			}

			if (FeatureRegistry.IsEnabled("player_stat_lock"))
			{
				status.SI("money", _lockMoney);
				status.SI("stress", _lockStress);
				// 每帧从游戏读取 activePowerMax，确保锁定为满值
				_lockAP = status.I("activePowerMax");
				if (_lockAP > 0)
				{
					status.SI("activePower", _lockAP);
				}
			}

			if (FeatureRegistry.IsEnabled("social_max_fav"))
			{
				Dyn friends = GameReflect.MyData.O("friendDataList");
				if (friends)
				{
					for (int i = 0; i < friends.Count; i++)
					{
						Dyn data = friends[i].O("data");
						if (data)
						{
							data.SI("fMeet", 100);
							data.SI("fFavarite", 100);
							data.SI("fLoveEvents", 5);
						}
					}
				}
			}

			if (FeatureRegistry.IsEnabled("shop_all_items"))
			{
				// 直接修改 ItemGroupData 的物品组列表，使其包含所有物品ID
				PatchController.ApplyShopAllItems();
			}
		}

		private void RunVerification()
		{
			MelonLogger.Msg("");
			MelonLogger.Msg("===========================================");
			MelonLogger.Msg("[F4] MizuofCheatMod 功能验证报告");
			MelonLogger.Msg("===========================================");

			// === 运行时数据验证 ===
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
				MelonLogger.Msg("  [--] MyData 不可用 (请在游戏场景中运行)");
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

			// === Harmony Patch 状态 ===
			MelonLogger.Msg("");
			MelonLogger.Msg("--- Harmony Patch 状态 ---");
			MelonLogger.Msg("  PatchesApplied=" + (PatchController.IsApplied ? "是" : "否"));

			// === 功能实现状态 ===
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

		private void DumpSnapshot()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine("============================================");
			sb.AppendLine("MizuofCheatMod v1.0.1 快照");
			sb.AppendLine("============================================");
			sb.Append("Game Assembly-CSharp: ");
			sb.AppendLine(GameHookScanner.IsAssemblyLoaded ? "已加载" : "未加载");
			sb.AppendLine();
			sb.AppendLine(FeatureRegistry.GetReport());
			sb.AppendLine("--- Hook 扫描 ---");
			sb.AppendLine(GameHookScanner.GetScanResult());
			MelonLogger.Msg(sb.ToString());
		}
	}
}
