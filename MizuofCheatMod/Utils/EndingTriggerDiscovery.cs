using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MelonLoader;
using UnityEngine;

namespace MizuofCheatMod.Utils
{
	internal static class EndingTriggerDiscovery
	{
		private static string _diagnosticReport;

		// 缓存游戏结束相关字段名
		private static string _maxPeriodFieldName;
		private static int _maxPeriodValue;
		private static readonly List<string> _periodFieldCandidates = new List<string>();

		private const int DEFAULT_MAX_PERIOD = 42;

		/// <summary>
		/// 运行时扫描程序集，发现周期/剩余回合/结局相关字段和方法
		/// </summary>
		internal static void Discover()
		{
			_maxPeriodFieldName = null;
			_maxPeriodValue = 0;
			_periodFieldCandidates.Clear();

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== 结局/周期 API 运行时扫描报告 ===");
			sb.AppendLine();

			Assembly asm = GetGameAssembly();
			if (asm == null)
			{
				sb.AppendLine("错误: Assembly-CSharp 未加载");
				_diagnosticReport = sb.ToString();
				return;
			}

			Type[] allTypes;
			try { allTypes = asm.GetTypes(); }
			catch (ReflectionTypeLoadException rtle) { allTypes = rtle.Types ?? new Type[0]; }

			// ---- 1. 扫描 MyData 的周期/上限字段 ----
			sb.AppendLine("--- MyData 周期/上限/剩余相关字段 ---");
			Type myDataType = FindType(allTypes, "MyData");
			if (myDataType != null)
			{
				FieldInfo[] fields = myDataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
				foreach (FieldInfo f in fields)
				{
					if (IsPeriodOrLimitField(f.Name) && IsNumericType(f.FieldType))
					{
						sb.AppendLine("  字段: " + f.Name + " : " + f.FieldType.Name);
						_periodFieldCandidates.Add(f.Name);
					}
				}
				// 也列出所有字段（太长的截断）
				sb.AppendLine("  --- 所有字段 ---");
				foreach (FieldInfo f in fields)
				{
					sb.AppendLine("    " + f.Name + " : " + f.FieldType.Name + (f.IsStatic ? " (static)" : ""));
				}
			}

			// ---- 2. 扫描 GStatus 的周期/上限字段 ----
			sb.AppendLine();
			sb.AppendLine("--- GStatus 周期/上限/剩余相关字段 ---");
			Type gsType = FindType(allTypes, "GStatus", "GameStatus");
			if (gsType != null)
			{
				FieldInfo[] fields = gsType.GetFields(BindingFlags.Public | BindingFlags.Instance);
				foreach (FieldInfo f in fields)
				{
					if (IsPeriodOrLimitField(f.Name) && IsNumericType(f.FieldType))
					{
						sb.AppendLine("  字段: " + f.Name + " : " + f.FieldType.Name);
						_periodFieldCandidates.Add("gstatus." + f.Name);
					}
				}
				sb.AppendLine("  --- 所有字段 ---");
				foreach (FieldInfo f in fields)
				{
					sb.AppendLine("    " + f.Name + " : " + f.FieldType.Name);
				}
			}

			// ---- 3. 扫描 PeriodData 的字段 ----
			sb.AppendLine();
			sb.AppendLine("--- PeriodData 字段 ---");
			Type pdType = FindType(allTypes, "PeriodData", "Period");
			if (pdType != null)
			{
				FieldInfo[] fields = pdType.GetFields(BindingFlags.Public | BindingFlags.Instance);
				foreach (FieldInfo f in fields)
				{
					sb.AppendLine("    " + f.Name + " : " + f.FieldType.Name);
				}
			}

			// ---- 4. 扫描 Status 剩余/上限字段 ----
			sb.AppendLine();
			sb.AppendLine("--- Status 周期/回合相关字段 ---");
			Type stType = FindType(allTypes, "Status", "PlayerStatus");
			if (stType != null)
			{
				FieldInfo[] fields = stType.GetFields(BindingFlags.Public | BindingFlags.Instance);
				foreach (FieldInfo f in fields)
				{
					if (IsPeriodOrLimitField(f.Name) && IsNumericType(f.FieldType))
					{
						sb.AppendLine("  字段: " + f.Name + " : " + f.FieldType.Name);
						_periodFieldCandidates.Add("status." + f.Name);
					}
				}
			}

			// ---- 5. 读取当前运行时值 ----
			sb.AppendLine();
			sb.AppendLine("--- 当前运行时值 ---");
			Dyn md = GameReflect.MyData;
			if (md)
			{
				sb.AppendLine("  MyData.period = " + md.I("period"));
				sb.AppendLine("  MyData.situation = " + md.I("situation"));
				Dyn pd = md.O("periodDataCurrent");
				if (pd)
				{
					sb.AppendLine("  PeriodData.year = " + pd.I("year"));
					sb.AppendLine("  PeriodData.month = " + pd.I("month"));
				}
				// 读取 periodDataList.Count
				Dyn pdl = md.O("periodDataList");
				int pdlCount = pdl ? pdl.Count : -1;
				sb.AppendLine("  periodDataList.Count = " + pdlCount);
				Dyn gs = md.O("gstatus");
				if (gs)
				{
					sb.AppendLine("  GStatus.loopCount = " + gs.I("loopCount"));
					sb.AppendLine("  GStatus.acvPoint = " + gs.I("acvPoint"));

					// 读取 GStatus 所有数值字段
					FieldInfo[] gsFields = gs.Obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
					foreach (FieldInfo f in gsFields)
					{
						if (IsNumericType(f.FieldType))
						{
							object val = f.GetValue(gs.Obj);
							sb.AppendLine("  GStatus." + f.Name + " = " + val);
						}
					}
				}
			}

			// ---- 6. 尝试自动识别最大周期字段 ----
			sb.AppendLine();
			sb.AppendLine("--- 自动识别结果 ---");
			if (md)
			{
				int mp = FindMaxPeriodValue(md);
				sb.AppendLine("  最终 maxPeriod = " + mp + " (来源: " + (_maxPeriodFieldName ?? "fallback") + ")");
			}

			_diagnosticReport = sb.ToString();
			MelonLogger.Msg("[EndingDiscovery] 扫描完成");
			MelonLogger.Msg(sb.ToString());
		}

		/// <summary>
		/// 触发结局：设置结局数据，将周期推进到最后，让游戏自然触发结局判定
		/// </summary>
		internal static bool TriggerEnding(object endingJobObj, int jobId)
		{
			Dyn md = GameReflect.MyData;
			if (!md)
			{
				MelonLogger.Msg("[结局] MyData 不可用");
				return false;
			}

			// 1. 设置结局数据
			if (endingJobObj != null)
			{
				GameReflect.SetField(md.Obj, "endingJob", endingJobObj);
				MelonLogger.Msg("[结局] 已设定结局 jobId=" + jobId);

				// 同时设置 endingPartner 为空，避免角色限定冲突
				Dyn ed = Dyn.Of(endingJobObj);
				Dyn partnerList = ed.O("partner");
				if (partnerList && partnerList.Count > 0)
				{
					// 如果有 partner 限制，清除 partner 避免匹配失败
					GameReflect.SetField(md.Obj, "endingPartner", null);
				}
			}

			// 2. 设置必要的 ending flag
			Dyn gs = md.O("gstatus");
			if (gs)
			{
				gs.SI("isShowTrueEnding", 1);
				// 某些游戏用 isTrueEndingUnlocked
				try { gs.SI("isTrueEndingUnlocked", 1); } catch { }
			}

			// 3. 临时关闭 time_freeze（异常安全）
			bool wasFrozen = FeatureRegistry.IsEnabled("time_freeze");
			if (wasFrozen) FeatureRegistry.SetEnabled("time_freeze", false);

			bool triggered = false;
			try
			{
				// 4. 计算真正的最大周期
				int maxPeriod = FindMaxPeriodValue(md);
				int currentPeriod = md.I("period");
				MelonLogger.Msg("[结局] 当前周期=" + currentPeriod + ", 最大周期=" + maxPeriod);

				if (maxPeriod > 0)
				{
					// 策略A: 设置 period 到最大周期，调用 CloseMonth
					if (currentPeriod < maxPeriod)
					{
						MelonLogger.Msg("[结局] [策略A] period " + currentPeriod + " → " + maxPeriod);
					}
					else
					{
						MelonLogger.Msg("[结局] [策略A] period 已到最大 (" + currentPeriod + ")，直接推进");
					}

					md.SI("period", maxPeriod);

					// 确保 situation 是 DAY（有些游戏在夜晚无法 CloseMonth）
					if (md.I("situation") != 0) md.SI("situation", 0);

					MelonLogger.Msg("[结局] 调用 CloseMonth...");
					md.CM("CloseMonth");

					int newPeriod = md.I("period");
					MelonLogger.Msg("[结局] CloseMonth 后 period=" + newPeriod);

					// 检查结局是否已触发 (period 变化或 endingJob 已生效)
					if (newPeriod > maxPeriod || CheckEndingTriggered(md, jobId))
					{
						triggered = true;
						MelonLogger.Msg("[结局] 结局已触发完成");
					}
					else
					{
						// 策略B: CloseMonth 未触发，再推进一次
						MelonLogger.Msg("[结局] [策略B] 再推进一次...");
						md.SI("period", maxPeriod + 1);
						md.CM("CloseMonth");

						int newPeriod2 = md.I("period");
						MelonLogger.Msg("[结局] 二次CloseMonth 后 period=" + newPeriod2);

						if (CheckEndingTriggered(md, jobId) || newPeriod2 > maxPeriod + 1)
						{
							triggered = true;
						}
						else
						{
							// 策略C: 直接注入结局
							MelonLogger.Msg("[结局] [策略C] 直接注入结局...");
							triggered = ForceEndingScene(md, jobId);
						}
					}
				}
				else
				{
					MelonLogger.Msg("[结局] 无法识别最大周期，直接调用 CloseMonth");
					md.CM("CloseMonth");
					triggered = true;
				}
			}
			finally
			{
				if (wasFrozen) FeatureRegistry.SetEnabled("time_freeze", true);
			}

			if (!triggered)
			{
				MelonLogger.Msg("[结局] 所有策略均未触发结局，尝试 F5 快捷键方式处理");
			}

			return triggered;
		}

		/// <summary>
		/// 检查结局是否正确触发
		/// </summary>
		private static bool CheckEndingTriggered(Dyn md, int expectedJobId)
		{
			try
			{
				Dyn ej = md.O("endingJob");
				if (ej && ej.Obj != null)
				{
					int actualId = ej.I("jobId");
					MelonLogger.Msg("[结局] 检查: endingJob.jobId=" + actualId + " (期望=" + expectedJobId + ")");
					return actualId == expectedJobId;
				}
				// 如果 endingJob 为 null，但 period 变化异常也可能是触发了
				return false;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// 强制触发结局场景：直接注入 endingJob 并调用游戏自身的场景切换
		/// </summary>
		private static bool ForceEndingScene(Dyn md, int jobId)
		{
			try
			{
				// 1. 尝试直接调用 EndingController
				Dyn ec = GameReflect.GetSingleton("EndingController");
				if (ec && ec.Obj != null)
				{
					MelonLogger.Msg("[结局] [Force] 调用 EndingController 方法...");
					// 常见的 EndingController 方法
					ec.CM("Init");
					ec.CM("ToSkip");
					MelonLogger.Msg("[结局] [Force] EndingController 已触发");
					return true;
				}

				// 2. 尝试设置所有 gstatus ending flags
				Dyn gs = md.O("gstatus");
				if (gs)
				{
					FieldInfo[] fields = gs.Obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
					foreach (FieldInfo f in fields)
					{
						if (f.Name.ToLowerInvariant().Contains("true") ||
							f.Name.ToLowerInvariant().Contains("another") ||
							f.Name.ToLowerInvariant().Contains("ending"))
						{
							if (f.FieldType == typeof(bool))
							{
								f.SetValue(gs.Obj, true);
								MelonLogger.Msg("[结局] [Force] 设置 " + f.Name + " = true");
							}
							else if (IsNumericType(f.FieldType))
							{
								f.SetValue(gs.Obj, 1);
								MelonLogger.Msg("[结局] [Force] 设置 " + f.Name + " = 1");
							}
						}
					}
				}

				// 3. 在 MyData 中查找场景切换方法
				MethodInfo[] methods = md.Obj.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
				foreach (MethodInfo m in methods)
				{
					string lower = m.Name.ToLowerInvariant();
					if ((lower.Contains("ending") || lower.Contains("finish") || lower.Contains("result")) &&
						m.GetParameters().Length == 0)
					{
						MelonLogger.Msg("[结局] [Force] 调用 " + m.Name);
						m.Invoke(md.Obj, null);
						return true;
					}
				}

				return false;
			}
			catch (Exception ex)
			{
				MelonLogger.Msg("[结局] [Force] 失败: " + ex.Message);
				return false;
			}
		}

		/// <summary>
		/// 尝试从运行时数据中找出游戏的最大周期数
		/// 优先使用 periodDataList.Count，其次才用启发式字段扫描
		/// </summary>
		private static int FindMaxPeriodValue(Dyn md)
		{
			int currentPeriod = md.I("period");

			// 方法0: periodDataList.Count — 最准确的来源
			try
			{
				object pdlObj = GameReflect.GetFieldObj(md.Obj, "periodDataList");
				if (pdlObj is IList pdl)
				{
					int count = pdl.Count;
					if (count > 0)
					{
						_maxPeriodFieldName = "periodDataList.Count";
						_maxPeriodValue = count;
						MelonLogger.Msg("[EndingDiscovery] periodDataList.Count=" + count + " 用作 maxPeriod");
						return count;
					}
				}
			}
			catch { }

			// 方法1: 缓存的结果
			if (_maxPeriodValue > 0)
				return _maxPeriodValue;

			// 方法2: 扫描所有候选字段，找大于当前周期的int值
			TryFindMaxPeriod(md, null);

			if (_maxPeriodValue > currentPeriod)
				return _maxPeriodValue;

			// 方法3: 遍历所有 int 字段找最大值
			Type myDataType = md.Obj.GetType();
			FieldInfo[] fields = myDataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
			int maxVal = 0;
			foreach (FieldInfo f in fields)
			{
				if (f.FieldType == typeof(int))
				{
					int val = (int)f.GetValue(md.Obj);
					if (val > 50 && val < 500 && val > maxVal)
					{
						maxVal = val;
						_maxPeriodFieldName = "MyData." + f.Name;
					}
				}
			}
			if (maxVal > 0)
			{
				_maxPeriodValue = maxVal;
				return maxVal;
			}

			// 方法4: 硬编码兜底
			MelonLogger.Msg("[EndingDiscovery] 未找到动态 maxPeriod，使用默认值 " + DEFAULT_MAX_PERIOD);
			_maxPeriodValue = DEFAULT_MAX_PERIOD;
			return DEFAULT_MAX_PERIOD;
		}

		private static void TryFindMaxPeriod(Dyn md, StringBuilder sb)
		{
			int currentPeriod = md.I("period");
			string[] strategyNames = {
				"periodMax", "maxPeriod", "totalPeriod", "endPeriod",
				"limitPeriod", "gameEndPeriod", "maxTurn", "totalTurn",
				"maxLoops", "loopMax", "periodLimit", "gameOverPeriod"
			};

			foreach (string candidate in strategyNames)
			{
				try
				{
					int val = md.I(candidate);
					if (val > 0 && val > currentPeriod)
					{
						_maxPeriodFieldName = "MyData." + candidate;
						_maxPeriodValue = val;
						if (sb != null)
							sb.AppendLine("  *** 候选: " + _maxPeriodFieldName + " = " + val);
						return;
					}
				}
				catch { }
			}

			// 检查 gstatus 中的字段
			Dyn gs = md.O("gstatus");
			if (gs)
			{
				foreach (string candidate in strategyNames)
				{
					try
					{
						int val = gs.I(candidate);
						if (val > 0 && val > currentPeriod)
						{
							_maxPeriodFieldName = "GStatus." + candidate;
							_maxPeriodValue = val;
							if (sb != null)
								sb.AppendLine("  *** 候选: " + _maxPeriodFieldName + " = " + val);
							return;
						}
					}
					catch { }
				}
			}

			// 检查 status 中的字段
			Dyn st = md.O("status");
			if (st)
			{
				foreach (string candidate in strategyNames)
				{
					try
					{
						int val = st.I(candidate);
						if (val > 0 && val > currentPeriod)
						{
							_maxPeriodFieldName = "Status." + candidate;
							_maxPeriodValue = val;
							if (sb != null)
								sb.AppendLine("  *** 候选: " + _maxPeriodFieldName + " = " + val);
							return;
						}
					}
					catch { }
				}
			}

			if (sb != null)
				sb.AppendLine("  (未找到匹配的周期上限字段，使用 periodDataList.Count 或默认)");
			_maxPeriodValue = 0;
		}

		private static bool IsPeriodOrLimitField(string name)
		{
			string lower = name.ToLowerInvariant();
			return lower.Contains("period") || lower.Contains("max") ||
				   lower.Contains("limit") || lower.Contains("remain") ||
				   lower.Contains("total") || lower.Contains("turn") ||
				   lower.Contains("loop") || lower.Contains("end") ||
				   lower.Contains("final") || lower.Contains("count") ||
				   lower.Contains("over") || lower.Contains("finish");
		}

		private static bool IsNumericType(Type t)
		{
			return t == typeof(int) || t == typeof(float) || t == typeof(long) ||
				   t == typeof(short) || t == typeof(byte);
		}

		internal static string GetReport()
		{
			return _diagnosticReport ?? "尚未运行扫描，请点击「诊断结局API」按钮";
		}

		private static Assembly GetGameAssembly()
		{
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (a.GetName().Name == "Assembly-CSharp")
					return a;
			}
			return null;
		}

		private static Type FindType(Type[] allTypes, params string[] names)
		{
			foreach (Type t in allTypes)
			{
				if (t == null) continue;
				foreach (string n in names)
				{
					if (t.Name == n || t.FullName == n)
						return t;
				}
			}
			return null;
		}

		/// <summary>
		/// 对外暴露的强制触发入口（供 TabEvents 快速按钮使用）
		/// </summary>
		internal static bool QuickTriggerEnding()
		{
			Dyn md = GameReflect.MyData;
			if (!md) return false;

			bool wasFrozen = FeatureRegistry.IsEnabled("time_freeze");
			if (wasFrozen) FeatureRegistry.SetEnabled("time_freeze", false);

			bool triggered = false;
			try
			{
				int maxPeriod = FindMaxPeriodValue(md);
				int before = md.I("period");

				// 确保 period 推进到 maxPeriod+1，触发游戏"无更多周期"判定
				md.SI("period", maxPeriod + 1);
				if (md.I("situation") != 0) md.SI("situation", 0);
				MelonLogger.Msg("[结局] 快速触发: period " + before + " → " + (maxPeriod + 1) + ", 调用 CloseMonth");
				md.CM("CloseMonth");

				int after = md.I("period");
				MelonLogger.Msg("[结局] 快速触发完成: period " + (maxPeriod + 1) + " -> " + after);

				triggered = (after > maxPeriod + 1);
			}
			finally
			{
				if (wasFrozen) FeatureRegistry.SetEnabled("time_freeze", true);
			}
			return triggered;
		}
	}
}
