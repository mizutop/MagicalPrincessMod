using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using MizuofCheatMod.Utils;

namespace MizuofCheatMod.Harmony
{
	internal static class PatchController
	{
		private static bool _patchesApplied;
		private static HarmonyLib.Harmony _instance;

		// 用户自定义结局列表（最多6个），由 TabEvents 设置
		private static readonly System.Collections.Generic.List<int> _customJobIds =
			new System.Collections.Generic.List<int>();

		/// <summary>
		/// 设置用户选择的替换结局 ID 列表（最多6个）
		/// </summary>
		internal static void SetCustomJobIds(System.Collections.Generic.List<int> ids)
		{
			_customJobIds.Clear();
			if (ids != null)
			{
				foreach (int id in ids)
				{
					if (!_customJobIds.Contains(id))
						_customJobIds.Add(id);
					if (_customJobIds.Count >= 6) break;
				}
			}
		}

		internal static System.Collections.Generic.List<int> GetCustomJobIds()
		{
			return new System.Collections.Generic.List<int>(_customJobIds);
		}

		internal static bool IsApplied
		{
			get
			{
				return _patchesApplied;
			}
		}

		internal static void ApplyAll()
		{
			if (_patchesApplied)
			{
				return;
			}
			try
			{
				_instance = new HarmonyLib.Harmony("MizuofCheatMod.Harmony");
				PatchBattleCharacterDamage();
				PatchBattleCharacterExtraDamage();
				PatchObjectBaseDamage();
				PatchItemPrices();
				PatchTimeFreeze();
				PatchShopAllItems();
				PatchEndingAllJobs();
				_patchesApplied = true;
				MelonLogger.Msg("[Harmony] 所有Patch已注册 (battle, items, time, shop, ending)");
			}
			catch (Exception ex)
			{
				MelonLogger.Msg("[Harmony] 注册失败: " + ex.Message);
			}
		}

		internal static void RemoveAll()
		{
			if (!_patchesApplied)
			{
				return;
			}
			try
			{
				_instance?.UnpatchSelf();
				_patchesApplied = false;
				MelonLogger.Msg("[Harmony] 所有Patch已卸载");
			}
			catch (Exception ex)
			{
				MelonLogger.Msg("[Harmony] 卸载失败: " + ex.Message);
			}
		}

		// ================================================================
		//  combat_god + combat_1hk: 拦截 BattleCharacter 回合制战斗伤害
		//  反编译证明: 正式战斗使用 BattleCharacter.SetPhysicalDamage /
		//  SetMagicalDamage，而非 ObjectBase.OnDamage（后者仅用于野外平台碰撞）
		// ================================================================
		private static void PatchBattleCharacterDamage()
		{
			Type bcType = Utils.GameReflect.ResolveType("BattleCharacter");
			if (bcType == null)
			{
				MelonLogger.Msg("[Harmony] BattleCharacter 未找到，combat 战斗Patch跳过");
				return;
			}

			int count = 0;

			// combat_god: 物理伤害 Prefix
			MethodInfo physicalDmg = bcType.GetMethod("SetPhysicalDamage",
				BindingFlags.Public | BindingFlags.Instance);
			if (physicalDmg != null)
			{
				MethodInfo godPhys = typeof(PatchController).GetMethod("CombatGodPrefix",
					BindingFlags.NonPublic | BindingFlags.Static);
				_instance.Patch(physicalDmg, new HarmonyLib.HarmonyMethod(godPhys), null, null);
				count++;
				MelonLogger.Msg("[Harmony] combat_god → BattleCharacter.SetPhysicalDamage [Prefix]");
			}

			// combat_god: 魔法伤害 Prefix
			MethodInfo magicalDmg = bcType.GetMethod("SetMagicalDamage",
				BindingFlags.Public | BindingFlags.Instance);
			if (magicalDmg != null)
			{
				MethodInfo godMag = typeof(PatchController).GetMethod("CombatGodPrefix",
					BindingFlags.NonPublic | BindingFlags.Static);
				_instance.Patch(magicalDmg, new HarmonyLib.HarmonyMethod(godMag), null, null);
				count++;
				MelonLogger.Msg("[Harmony] combat_god → BattleCharacter.SetMagicalDamage [Prefix]");
			}

			// combat_1hk: 物理伤害放大
			if (physicalDmg != null)
			{
				MethodInfo hkPhys = typeof(PatchController).GetMethod("Combat1HKPhysicalPrefix",
					BindingFlags.NonPublic | BindingFlags.Static);
				_instance.Patch(physicalDmg, new HarmonyLib.HarmonyMethod(hkPhys), null, null);
				count++;
				MelonLogger.Msg("[Harmony] combat_1hk → BattleCharacter.SetPhysicalDamage [Prefix]");
			}

			// combat_1hk: 魔法伤害放大
			if (magicalDmg != null)
			{
				MethodInfo hkMag = typeof(PatchController).GetMethod("Combat1HKMagicalPrefix",
					BindingFlags.NonPublic | BindingFlags.Static);
				_instance.Patch(magicalDmg, new HarmonyLib.HarmonyMethod(hkMag), null, null);
				count++;
				MelonLogger.Msg("[Harmony] combat_1hk → BattleCharacter.SetMagicalDamage [Prefix]");
			}

			if (count == 0)
			{
				MelonLogger.Msg("[Harmony] BattleCharacter 战斗方法未找到，combat 全部跳过");
			}
		}

		/// <summary>
		/// combat_god: 当伤害目标为友方角色时跳过伤害 (isEnemySide==false)
		/// </summary>
		private static bool CombatGodPrefix(BattleCharacter __instance)
		{
			if (!Utils.FeatureRegistry.IsEnabled("combat_god"))
			{
				return true;
			}
			// __instance = 承受伤害的目标
			// isEnemySide == false → 友方 → 跳过伤害
			return __instance.isEnemySide;
		}

		/// <summary>
		/// combat_1hk: 对敌方角色放大物理攻击参数
		/// </summary>
		private static bool Combat1HKPhysicalPrefix(ref float _atk, BattleCharacter __instance)
		{
			if (!Utils.FeatureRegistry.IsEnabled("combat_1hk"))
			{
				return true;
			}
			if (__instance.isEnemySide)
			{
				_atk = 99999f;
			}
			return true;
		}

		/// <summary>
		/// combat_1hk: 对敌方角色放大魔法攻击参数
		/// </summary>
		private static bool Combat1HKMagicalPrefix(ref float _batk, BattleCharacter __instance)
		{
			if (!Utils.FeatureRegistry.IsEnabled("combat_1hk"))
			{
				return true;
			}
			if (__instance.isEnemySide)
			{
				_batk = 99999f;
			}
			return true;
		}

		// ================================================================
		//  combat_god + combat_1hk: 额外战斗伤害路径补丁
		//  覆盖 SetDamage、AddDamage、OnDamage 等非标准伤害路径
		// ================================================================
		private static void PatchBattleCharacterExtraDamage()
		{
			Type bcType = Utils.GameReflect.ResolveType("BattleCharacter");
			if (bcType == null) return;

			int count = 0;

			// SetDamage(float dmg) — 通用伤害接口
			MethodInfo setDmg = bcType.GetMethod("SetDamage",
				BindingFlags.Public | BindingFlags.Instance, null,
				new Type[] { typeof(float) }, null);
			if (setDmg != null)
			{
				MethodInfo prefix = typeof(PatchController).GetMethod("CombatGodPrefixExtra",
					BindingFlags.NonPublic | BindingFlags.Static, null,
					new[] { typeof(BattleCharacter) }, null);
				_instance.Patch(setDmg, new HarmonyLib.HarmonyMethod(prefix), null, null);
				count++;
				MelonLogger.Msg("[Harmony] combat_god → BattleCharacter.SetDamage [Prefix]");

				MethodInfo hkPrefix = typeof(PatchController).GetMethod("Combat1HKPrefixExtra",
					BindingFlags.NonPublic | BindingFlags.Static, null,
					new[] { typeof(float).MakeByRefType(), typeof(BattleCharacter) }, null);
				_instance.Patch(setDmg, new HarmonyLib.HarmonyMethod(hkPrefix), null, null);
				count++;
				MelonLogger.Msg("[Harmony] combat_1hk → BattleCharacter.SetDamage [Prefix]");
			}

			// AddDamage(float dmg) — DOT/持续伤害
			MethodInfo addDmg = bcType.GetMethod("AddDamage",
				BindingFlags.Public | BindingFlags.Instance, null,
				new Type[] { typeof(float) }, null);
			if (addDmg != null)
			{
				MethodInfo prefix = typeof(PatchController).GetMethod("CombatGodPrefixExtra",
					BindingFlags.NonPublic | BindingFlags.Static, null,
					new[] { typeof(BattleCharacter) }, null);
				_instance.Patch(addDmg, new HarmonyLib.HarmonyMethod(prefix), null, null);
				count++;
				MelonLogger.Msg("[Harmony] combat_god → BattleCharacter.AddDamage [Prefix]");

				MethodInfo hkPrefix = typeof(PatchController).GetMethod("Combat1HKPrefixExtra",
					BindingFlags.NonPublic | BindingFlags.Static, null,
					new[] { typeof(float).MakeByRefType(), typeof(BattleCharacter) }, null);
				_instance.Patch(addDmg, new HarmonyLib.HarmonyMethod(hkPrefix), null, null);
				count++;
				MelonLogger.Msg("[Harmony] combat_1hk → BattleCharacter.AddDamage [Prefix]");
			}

			if (count == 0)
			{
				MelonLogger.Msg("[Harmony] BattleCharacter 额外伤害方法未找到，跳过");
			}
		}

		// ================================================================
		//  combat_god + combat_1hk: ObjectBase.OnDamage 野外/环境伤害
		// ================================================================
		private static void PatchObjectBaseDamage()
		{
			Type obType = Utils.GameReflect.ResolveType("ObjectBase");
			if (obType == null) return;

			MethodInfo onDmg = obType.GetMethod("OnDamage",
				BindingFlags.Public | BindingFlags.Instance, null,
				new Type[] { typeof(float) }, null);
			if (onDmg == null)
			{
				onDmg = obType.GetMethod("OnDamage",
					BindingFlags.Public | BindingFlags.Instance, null,
					new Type[] { typeof(float), typeof(int) }, null);
			}
			if (onDmg == null)
			{
				MelonLogger.Msg("[Harmony] ObjectBase.OnDamage 未找到，跳过环境伤害Patch");
				return;
			}

			MethodInfo godPrefix = typeof(PatchController).GetMethod("CombatGodPrefixExtra",
				BindingFlags.NonPublic | BindingFlags.Static, null,
				new[] { typeof(object) }, null);
			_instance.Patch(onDmg, new HarmonyLib.HarmonyMethod(godPrefix), null, null);
			MelonLogger.Msg("[Harmony] combat_god → ObjectBase.OnDamage [Prefix]");

			MethodInfo hkPrefix = typeof(PatchController).GetMethod("Combat1HKPrefixExtra",
				BindingFlags.NonPublic | BindingFlags.Static, null,
				new[] { typeof(float).MakeByRefType(), typeof(object) }, null);
			_instance.Patch(onDmg, new HarmonyLib.HarmonyMethod(hkPrefix), null, null);
			MelonLogger.Msg("[Harmony] combat_1hk → ObjectBase.OnDamage [Prefix]");
		}

		// --- Prefix 方法（额外伤害路径的通用版） ---

		private static bool CombatGodPrefixExtra(BattleCharacter __instance)
		{
			if (!Utils.FeatureRegistry.IsEnabled("combat_god"))
				return true;
			return __instance.isEnemySide;
		}

		private static bool Combat1HKPrefixExtra(ref float dmg, BattleCharacter __instance)
		{
			if (!Utils.FeatureRegistry.IsEnabled("combat_1hk"))
				return true;
			if (__instance.isEnemySide)
				dmg = 99999f;
			return true;
		}

		/// <summary>
		/// ObjectBase.OnDamage 的 combat_god — 检查 __instance 是否可转为 BattleCharacter
		/// </summary>
		private static bool CombatGodPrefixExtra(object __instance)
		{
			if (!Utils.FeatureRegistry.IsEnabled("combat_god"))
				return true;
			if (__instance is BattleCharacter bc)
				return bc.isEnemySide;
			return true;
		}

		/// <summary>
		/// ObjectBase.OnDamage 的 combat_1hk
		/// </summary>
		private static bool Combat1HKPrefixExtra(ref float dmg, object __instance)
		{
			if (!Utils.FeatureRegistry.IsEnabled("combat_1hk"))
				return true;
			if (__instance is BattleCharacter bc && bc.isEnemySide)
				dmg = 99999f;
			return true;
		}

		// ================================================================
		//  item_free_shop + item_max_sell: 拦截 ItemData 价格属性
		//  反编译证明: 不存在 ShopSystem 类，价格通过 ItemData.priceBuy/
		//  ItemData.priceSellItem 计算属性完成
		// ================================================================
		private static void PatchItemPrices()
		{
			Type itemDataType = Utils.GameReflect.ResolveType("ItemData");
			if (itemDataType == null)
			{
				MelonLogger.Msg("[Harmony] ItemData 未找到，item 价格Patch跳过");
				return;
			}

			// item_free_shop: 拦截 get_priceBuy → 返回 0
			MethodInfo priceBuyGetter = itemDataType.GetMethod("get_priceBuy",
				BindingFlags.Public | BindingFlags.Instance);
			if (priceBuyGetter != null)
			{
				MethodInfo postfix = typeof(PatchController).GetMethod("FreeShopPostfix",
					BindingFlags.NonPublic | BindingFlags.Static);
				_instance.Patch(priceBuyGetter, null, new HarmonyLib.HarmonyMethod(postfix), null);
				MelonLogger.Msg("[Harmony] item_free_shop → ItemData.get_priceBuy [Postfix]");
			}
			else
			{
				MelonLogger.Msg("[Harmony] ItemData.get_priceBuy 未找到，item_free_shop跳过");
			}

			// item_free_shop: 额外拦截 get_priceBuyBlackCoin → 返回 0 (东亚硬币商品)
			MethodInfo blackCoinGetter = itemDataType.GetMethod("get_priceBuyBlackCoin",
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
			if (blackCoinGetter != null)
			{
				MethodInfo postfix = typeof(PatchController).GetMethod("FreeShopPostfix",
					BindingFlags.NonPublic | BindingFlags.Static);
				_instance.Patch(blackCoinGetter, null, new HarmonyLib.HarmonyMethod(postfix), null);
				MelonLogger.Msg("[Harmony] item_free_shop → ItemData.get_priceBuyBlackCoin [Postfix]");
			}
			else
			{
				MelonLogger.Msg("[Harmony] ItemData.get_priceBuyBlackCoin 未找到，跳过");
			}

			// item_max_sell: 拦截 get_priceSellItem → 结果 x10
			MethodInfo priceSellGetter = itemDataType.GetMethod("get_priceSellItem",
				BindingFlags.Public | BindingFlags.Instance);
			if (priceSellGetter != null)
			{
				MethodInfo postfix = typeof(PatchController).GetMethod("MaxSellPostfix",
					BindingFlags.NonPublic | BindingFlags.Static);
				_instance.Patch(priceSellGetter, null, new HarmonyLib.HarmonyMethod(postfix), null);
				MelonLogger.Msg("[Harmony] item_max_sell → ItemData.get_priceSellItem [Postfix]");
			}
			else
			{
				MelonLogger.Msg("[Harmony] ItemData.get_priceSellItem 未找到，item_max_sell跳过");
			}
		}

		private static void FreeShopPostfix(ref int __result)
		{
			if (Utils.FeatureRegistry.IsEnabled("item_free_shop"))
			{
				__result = 0;
			}
		}

		private static void MaxSellPostfix(ref int __result)
		{
			if (Utils.FeatureRegistry.IsEnabled("item_max_sell"))
			{
				__result *= 10;
			}
		}

		// ================================================================
		//  time_freeze: 拦截 MyData.CloseMonth + GameController.CloseMonth
		// ================================================================
		private static void PatchTimeFreeze()
		{
			int count = 0;

			// 拦截 MyData.CloseMonth (月结核心逻辑)
			Type myDataType = Utils.GameReflect.ResolveType("MyData");
			if (myDataType != null)
			{
				MethodInfo original = myDataType.GetMethod("CloseMonth",
					BindingFlags.Public | BindingFlags.Instance);
				if (original != null)
				{
					MethodInfo prefix = typeof(PatchController).GetMethod("TimeFreezePrefix",
						BindingFlags.NonPublic | BindingFlags.Static);
					_instance.Patch(original, new HarmonyLib.HarmonyMethod(prefix), null, null);
					count++;
					MelonLogger.Msg("[Harmony] time_freeze → MyData.CloseMonth [Prefix]");
				}
			}

			// 额外拦截 GameController.CloseMonth (事件队列处理链)
			Type gcType = Utils.GameReflect.ResolveType("GameController");
			if (gcType != null)
			{
				MethodInfo original = gcType.GetMethod("CloseMonth",
					BindingFlags.Public | BindingFlags.Instance);
				if (original != null)
				{
					MethodInfo prefix = typeof(PatchController).GetMethod("TimeFreezePrefix",
						BindingFlags.NonPublic | BindingFlags.Static);
					_instance.Patch(original, new HarmonyLib.HarmonyMethod(prefix), null, null);
					count++;
					MelonLogger.Msg("[Harmony] time_freeze → GameController.CloseMonth [Prefix]");
				}
			}

			if (count == 0)
			{
				MelonLogger.Msg("[Harmony] CloseMonth 方法未找到，time_freeze跳过");
			}
		}

		private static bool TimeFreezePrefix()
		{
			return !Utils.FeatureRegistry.IsEnabled("time_freeze");
		}

		// ================================================================
		//  ending_all_jobs: Hook GetEnableJobList 返回全部结局
		//  游戏选择界面只显示通过 stat/event 过滤后的结局，
		//  此 Patch 绕过过滤，让所有 50+ 个结局在游戏内可选
		// ================================================================
		private static void PatchEndingAllJobs()
		{
			Type myDataType = Utils.GameReflect.ResolveType("MyData");
			if (myDataType == null)
			{
				MelonLogger.Msg("[Harmony] MyData 未找到，ending_all_jobs跳过");
				return;
			}

			MethodInfo original = myDataType.GetMethod("GetEnableJobList",
				BindingFlags.Public | BindingFlags.Instance);
			if (original == null)
			{
				MelonLogger.Msg("[Harmony] MyData.GetEnableJobList 未找到，ending_all_jobs跳过");
				return;
			}

			MethodInfo postfix = typeof(PatchController).GetMethod("AllEndingJobsPostfix",
				BindingFlags.NonPublic | BindingFlags.Static);
			_instance.Patch(original, null, new HarmonyLib.HarmonyMethod(postfix), null);
			MelonLogger.Msg("[Harmony] ending_all_jobs → MyData.GetEnableJobList [Postfix]");
		}

		private static void AllEndingJobsPostfix(ref System.Collections.Generic.List<EndingJobData> __result, MyData __instance)
		{
			if (!Utils.FeatureRegistry.IsEnabled("ending_all_jobs"))
			{
				return;
			}
			if (_customJobIds.Count == 0)
			{
				// 未选择任何自定义结局时，仅返回 Queen（第一个）避免空列表
				__result = new System.Collections.Generic.List<EndingJobData>
				{
					__instance.endingJobDataList[0]
				};
				return;
			}
			// 只返回用户选择的结局（最多6个）
			var filtered = new System.Collections.Generic.List<EndingJobData>();
			foreach (int id in _customJobIds)
			{
				// endingJobDataList 索引从 1 开始对应 jobId
				int index = id - 1;
				if (index >= 0 && index < __instance.endingJobDataList.Count)
				{
					filtered.Add(__instance.endingJobDataList[index]);
				}
			}
			if (filtered.Count == 0)
			{
				filtered.Add(__instance.endingJobDataList[0]);
			}
			__result = filtered;
		}

		// ================================================================
		//  shop_all_items: 武器店/服装店显示全部物品
		//  直接修改 MyData.itemGroupDataList 中的物品组，使其包含所有物品ID
		//  配合 item_free_shop 实现定价0元
		// ================================================================
		private static void PatchShopAllItems()
		{
			// 无需 Harmony 补丁，全部在帧更新中完成
			// (因为 PeriodData 的商店字段是 Int32 字段，不是属性，无法 Hook)
			MelonLogger.Msg("[Harmony] shop_all_items: 使用帧更新模式 (direct reflection)");
		}

		/// <summary>
		/// 在运行时应用商店全物品修改（由 Main.ExecuteFrameFeatures 调用）
		/// </summary>
		internal static void ApplyShopAllItems()
		{
			try
			{
				Dyn m = Utils.GameReflect.MyData;
				if (!m) return;

				int loc = m.I("location");
				if (loc != 8 && loc != 9) return;

				// 获取 PeriodData 中的商店物品组 ID
				Dyn pd = m.O("periodDataCurrent");
				if (!pd) return;

				int weaponGroupId = pd.I("weaponshopItems");
				int dressGroupId = pd.I("dressshopItems");

				// 获取当前所有物品的 ID 列表
				Dyn allItems = m.O("itemDataList");
				if (!allItems || allItems.Count == 0) return;

				System.Collections.Generic.List<int> allItemIds =
					new System.Collections.Generic.List<int>();
				for (int i = 0; i < allItems.Count; i++)
				{
					allItemIds.Add(allItems[i].I("itemId"));
				}

				// 获取 itemGroupDataList，修改对应的物品组
				Dyn groups = m.O("itemGroupDataList");
				if (!groups) return;

				int targetId = (loc == 8) ? weaponGroupId : dressGroupId;
				if (targetId <= 0) return;

				for (int i = 0; i < groups.Count; i++)
				{
					int gid = groups[i].I("itemGroupId");
					if (gid == targetId)
					{
						// 直接替换 itemIds 为全部物品 ID
						object itemIdsObj = Utils.GameReflect.GetFieldObj(groups[i].Obj, "itemIds");
						if (itemIdsObj is System.Collections.IList list)
						{
							list.Clear();
							foreach (int id in allItemIds)
							{
								list.Add(id);
							}
						}
						break;
					}
				}
			}
			catch { }
		}
	}
}
