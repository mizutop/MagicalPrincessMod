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

		// 战斗属性锁定缓存（由 TabCombat 的一键 999 或手动 Set 设置）
		private bool _combatStatsCached;
		private float _cachedHP, _cachedATK, _cachedDEF, _cachedSPD, _cachedSTA;
		private float _cachedBATK, _cachedWATK, _cachedMDEF, _cachedMSTA, _cachedLUCK;
		private float _cachedBMale, _cachedBFemale;

		// 延迟时间跳转（防止 GUI 中执行 CloseMonth 导致重入）
		private static int _pendingTimeJump = -1;

		public override void OnInitializeMelon()
		{
			Instance = this;
			ModConfig.Load();
			FeatureRegistry.InitDefaults();
			GameMethodResolver.Scan();
			MelonLogger.Msg("=== MizuofCheatMod v1.0.1 ===");
			MelonLogger.Msg("F1=面板  F2=快照  F3=Hook  F4=验证  F5=即赢");
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
			// 延迟时间跳转（防止 GUI 重入）
			if (_pendingTimeJump > 0)
			{
				int target = _pendingTimeJump;
				_pendingTimeJump = -1;
				ExecuteTimeJump(target);
			}

			Dyn status = GameReflect.Status;
			if (!status)
			{
				return;
			}

			if (FeatureRegistry.IsEnabled("player_stat_lock"))
			{
				// 注意：属性锁定使用直接写字段而不是 GameMethodResolver，
				// 因为锁定需要每帧覆盖/强制值，调用 AddMoney() 等会导致每帧累加
				status.SI("money", _lockMoney);
				status.SI("stress", _lockStress);
				// 每帧从游戏读取 activePowerMax，确保锁定为满值
				_lockAP = status.I("activePowerMax");
				if (_lockAP > 0)
				{
					status.SI("activePower", _lockAP);
				}
			}

			// 战斗属性锁定：战斗中每帧恢复 bstatus
			if (FeatureRegistry.IsEnabled("combat_stat_lock") && _combatStatsCached)
			{
				Dyn bc = GameReflect.GetSingleton("BattleController");
				if (bc)
				{
					// 检查是否在战斗中：BattleController 有 isBattle / isBattleActive 等属性
					bool inBattle = bc.I("isBattleStart") > 0 || bc.I("isBattle") > 0;
					if (!inBattle)
					{
						// 尝试通过其他字段判断
						inBattle = bc.I("battleActive") > 0 || bc.I("isBattleActive") > 0;
					}
					if (inBattle)
					{
						Dyn bs = GameReflect.MyData.O("bstatus");
						if (bs)
						{
							bs.SF("hp", _cachedHP); bs.SF("atk", _cachedATK);
							bs.SF("defence", _cachedDEF); bs.SF("speed", _cachedSPD);
							bs.SF("stamina", _cachedSTA); bs.SF("batk", _cachedBATK);
							bs.SF("watk", _cachedWATK); bs.SF("mdefence", _cachedMDEF);
							bs.SF("mstamina", _cachedMSTA); bs.SF("luck", _cachedLUCK);
							bs.SF("buffMale", _cachedBMale); bs.SF("buffFemale", _cachedBFemale);
						}
					}
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

		/// <summary>
		/// 供 TabTime 调用，将跳转调度到下一帧 OnUpdate 执行（防止 GUI 重入）
		/// </summary>
		internal static void ScheduleTimeJump(int targetPeriod)
		{
			_pendingTimeJump = targetPeriod;
		}

		/// <summary>
		/// 实际执行 CloseMonth 循环（在 OnUpdate 中安全执行）
		/// </summary>
		private void ExecuteTimeJump(int targetPeriod)
		{
			Dyn md = GameReflect.MyData;
			if (!md) return;

			int current = md.I("period");
			if (targetPeriod <= current)
			{
				MelonLogger.Msg("[时间] 跳转跳过: 目标 " + targetPeriod + " <= 当前 " + current);
				return;
			}

			int steps = targetPeriod - current;
			if (steps > 50)
			{
				MelonLogger.Msg("[时间] 跳转取消: 步数 " + steps + " 超过50上限");
				return;
			}

			bool wasFrozen = FeatureRegistry.IsEnabled("time_freeze");
			if (wasFrozen) FeatureRegistry.SetEnabled("time_freeze", false);

			MelonLogger.Msg("[时间] 开始跳转: " + current + " -> " + targetPeriod + " (共" + steps + "步)");
			for (int i = 0; i < steps; i++)
			{
				md.CM("CloseMonth");
			}

			int actual = md.I("period");
			if (actual >= targetPeriod)
			{
				MelonLogger.Msg("[时间] 跳转完成: 到达 " + actual);
			}
			else
			{
				MelonLogger.Msg("[时间] 跳转可能未完成: 目标 " + targetPeriod + ", 当前 " + actual);
			}

			if (wasFrozen) FeatureRegistry.SetEnabled("time_freeze", true);
		}

		private void RunVerification()
		{
			MelonLogger.Msg("");
			MelonLogger.Msg("== [F4] MizuofCheatMod 功能验证 ==");

			// 运行时数据
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

			// 功能状态
			int en = FeatureRegistry.EnabledCount;
			int tot = FeatureRegistry.TotalCount;
			MelonLogger.Msg("  Patch: " + (PatchController.IsApplied ? "已注册" : "未注册"));
			MelonLogger.Msg("  功能: " + en + "/" + tot + " 开启  " + FeatureRegistry.ImplementedCount + " 已实现");

			// 列出当前开启的功能
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

		/// <summary>
		/// 供 TabCombat 调用，缓存当前 bstatus 值用于战斗中每帧恢复
		/// </summary>
		internal static void CacheCombatStats()
		{
			var inst = Instance;
			if (inst == null) return;
			Dyn bs = GameReflect.MyData.O("bstatus");
			if (!bs) return;
			inst._cachedHP = bs.F("hp");
			inst._cachedATK = bs.F("atk");
			inst._cachedDEF = bs.F("defence");
			inst._cachedSPD = bs.F("speed");
			inst._cachedSTA = bs.F("stamina");
			inst._cachedBATK = bs.F("batk");
			inst._cachedWATK = bs.F("watk");
			inst._cachedMDEF = bs.F("mdefence");
			inst._cachedMSTA = bs.F("mstamina");
			inst._cachedLUCK = bs.F("luck");
			inst._cachedBMale = bs.F("buffMale");
			inst._cachedBFemale = bs.F("buffFemale");
			inst._combatStatsCached = true;
			MelonLogger.Msg("[战斗] bstatus 缓存已更新");
		}

		private void DumpSnapshot()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine("== MizuofCheatMod v1.0.1 快照 ==");
			sb.Append("Assembly-CSharp: ");
			sb.AppendLine(GameHookScanner.IsAssemblyLoaded ? "已加载" : "未加载");
			sb.Append("Patch: ");
			sb.AppendLine(PatchController.IsApplied ? "已注册" : "未注册");
			sb.AppendLine();
			sb.AppendLine(FeatureRegistry.GetReport());
			MelonLogger.Msg(sb.ToString());
		}
	}
}
