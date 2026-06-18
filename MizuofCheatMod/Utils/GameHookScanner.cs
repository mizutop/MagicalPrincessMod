using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace MizuofCheatMod.Utils
{
	internal static class GameHookScanner
	{
		private static string _cachedResult = "尚未扫描 -- 按 F3 刷新";
		private static readonly HashSet<string> _gameAssemblyNames = new HashSet<string>
		{
			"Assembly-CSharp",
			"Assembly-CSharp-firstpass"
		};
		private static bool _assemblyCSharpLoaded;
		private static int _lastTypeCount;

		internal static bool IsAssemblyLoaded
		{
			get
			{
				return _assemblyCSharpLoaded;
			}
		}

		internal static void RefreshCache()
		{
			try
			{
				Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
				_assemblyCSharpLoaded = false;
				Assembly mainAsm = null;
				foreach (Assembly a in allAssemblies)
				{
					if (a.GetName().Name == "Assembly-CSharp")
					{
						_assemblyCSharpLoaded = true;
						mainAsm = a;
						break;
					}
				}

				StringBuilder sb = new StringBuilder();
				sb.AppendLine("运行时程序集加载清单");
				foreach (Assembly a in allAssemblies)
				{
					AssemblyName name = a.GetName();
					bool isGame = _gameAssemblyNames.Contains(name.Name);
					sb.Append("  ");
					sb.Append(isGame ? "G" : ".");
					sb.Append(" ");
					sb.Append(name.Name);
					sb.Append(" v");
					sb.Append(name.Version);
					sb.AppendLine();
				}
				sb.AppendLine();
				sb.AppendLine("G = 游戏相关  . = 系统/第三方");
				sb.AppendLine();

				if (!_assemblyCSharpLoaded)
				{
					sb.AppendLine("Assembly-CSharp 尚未加载");
					sb.AppendLine("原因: 游戏主程序集在启动初期可能未被 Unity 加载");
					sb.AppendLine("解决: 进入游戏主菜单或实际游戏场景后重新扫描");
					sb.Append("当前已加载程序集数量: ");
					sb.Append(allAssemblies.Length);
					sb.AppendLine();
					_cachedResult = sb.ToString();
					MelonLogger.Msg("[HOOK] Assembly-CSharp 未加载");
					return;
				}

				Type[] allTypes;
				try
				{
					allTypes = mainAsm.GetTypes();
				}
				catch (ReflectionTypeLoadException rtle)
				{
					List<Type> loadedList = new List<Type>();
					if (rtle.Types != null)
					{
						foreach (Type t in rtle.Types)
						{
							if (t != null)
							{
								loadedList.Add(t);
							}
						}
					}
					allTypes = loadedList.ToArray();
					sb.Append("部分类型加载失败 (已加载 ");
					sb.Append(allTypes.Length);
					sb.Append(" 个):");
					sb.AppendLine();
					foreach (Exception le in rtle.LoaderExceptions)
					{
						if (le != null)
						{
							sb.Append("  -> ");
							sb.Append(le.Message);
							sb.AppendLine();
						}
					}
					sb.AppendLine();
				}
				_lastTypeCount = allTypes.Length;

				sb.Append("Assembly-CSharp 类型总数: ");
				sb.Append(allTypes.Length);
				sb.AppendLine();
				sb.AppendLine();

				ScanSingletons(allTypes, sb);
				ScanManagers(allTypes, sb);
				ScanDataClasses(allTypes, sb);
				ScanEnums(allTypes, sb);

				_cachedResult = sb.ToString();
				MelonLogger.Msg("[HOOK] 扫描完成");
			}
			catch (Exception ex)
			{
				_cachedResult = "[错误] 扫描异常: " + ex.Message + "\n" + ex.StackTrace;
				MelonLogger.Msg("[HOOK] 扫描异常: " + ex.Message);
			}
		}

		internal static string GetScanResult()
		{
			return _cachedResult;
		}

		private static void ScanSingletons(Type[] types, StringBuilder sb)
		{
			sb.AppendLine("-- 单例类 --");
			int count = 0;
			foreach (Type type in types)
			{
				if (type.IsAbstract || !type.IsClass || type.IsGenericTypeDefinition)
				{
					continue;
				}
				Type baseType = type.BaseType;
				if (baseType == null || !baseType.IsGenericType)
				{
					continue;
				}
				Type genericDef = baseType.GetGenericTypeDefinition();
				if (genericDef != typeof(SingletonMonoBehaviour<>) &&
					genericDef != typeof(PersistentSingletonMonoBehaviour<>))
				{
					continue;
				}
				count++;
				bool hasInstance = TryGetInstance(type);
				sb.Append("  ");
				sb.Append(hasInstance ? "O" : ".");
				sb.Append(" ");
				sb.Append(type.Name);
				sb.AppendLine();
			}
			sb.Append("共 ");
			sb.Append(count);
			sb.Append(" 个单例");
			sb.AppendLine();
			sb.AppendLine();
		}

		private static void ScanManagers(Type[] types, StringBuilder sb)
		{
			sb.AppendLine("-- Manager/Controller 类 --");
			int count = 0;
			foreach (Type type in types)
			{
				if (type.IsAbstract || !type.IsClass || type.IsGenericTypeDefinition)
				{
					continue;
				}
				string name = type.Name;
				if (!name.Contains("Manager") && !name.Contains("Controller") && !name.Contains("System"))
				{
					continue;
				}
				count++;
				bool hasInstance = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static) != null;
				sb.Append("  ");
				sb.Append(hasInstance ? "O" : ".");
				sb.Append(" ");
				sb.Append(type.Name);
				sb.AppendLine();
				int methodCount = 0;
				MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
				foreach (MethodInfo m in methods)
				{
					if (methodCount >= 6)
					{
						break;
					}
					if (m.IsSpecialName || m.Name == "Start" || m.Name == "Awake" || m.Name == "Update" ||
						m.Name == "FixedUpdate" || m.Name == "LateUpdate" || m.Name == "OnDestroy")
					{
						continue;
					}
					ParameterInfo[] parms = m.GetParameters();
					string paramStr = string.Empty;
					foreach (ParameterInfo p in parms)
					{
						if (paramStr.Length > 0)
						{
							paramStr += ", ";
						}
						paramStr += p.ParameterType.Name;
					}
					sb.Append("    ");
					sb.Append(m.Name);
					sb.Append("(");
					sb.Append(paramStr);
					sb.Append(") -> ");
					sb.Append(m.ReturnType.Name);
					sb.AppendLine();
					methodCount++;
				}
			}
			sb.Append("共 ");
			sb.Append(count);
			sb.Append(" 个 Manager 类");
			sb.AppendLine();
			sb.AppendLine();
		}

		private static void ScanDataClasses(Type[] types, StringBuilder sb)
		{
			sb.AppendLine("-- 数据类 (Status/Data/Param) --");
			int count = 0;
			foreach (Type type in types)
			{
				if (type.IsAbstract || type.IsEnum || !type.IsClass || type.IsGenericTypeDefinition)
				{
					continue;
				}
				string name = type.Name;
				if (!name.Contains("Status") && !name.Contains("Data") && !name.Contains("Param"))
				{
					continue;
				}
				if (name.Contains("Manager") || name.Contains("Controller"))
				{
					continue;
				}
				count++;
				sb.Append("  ");
				sb.Append(type.Name);
				sb.AppendLine();
				FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
				foreach (FieldInfo f in fields)
				{
					sb.Append("    ");
					sb.Append(f.Name);
					sb.Append(": ");
					sb.Append(f.FieldType.Name);
					sb.AppendLine();
				}
			}
			sb.Append("共 ");
			sb.Append(count);
			sb.Append(" 个数据类");
			sb.AppendLine();
			sb.AppendLine();
		}

		private static void ScanEnums(Type[] types, StringBuilder sb)
		{
			sb.AppendLine("-- 枚举类型 --");
			int count = 0;
			foreach (Type type in types)
			{
				if (!type.IsEnum)
				{
					continue;
				}
				count++;
				Array vals = Enum.GetValues(type);
				sb.Append("  ");
				sb.Append(type.Name);
				sb.Append(" [");
				int shown = 0;
				foreach (object v in vals)
				{
					if (shown >= 8)
					{
						sb.Append(", ...");
						break;
					}
					if (shown > 0)
					{
						sb.Append(", ");
					}
					sb.Append(v);
					shown++;
				}
				sb.AppendLine("]");
			}
			sb.Append("共 ");
			sb.Append(count);
			sb.Append(" 个枚举");
			sb.AppendLine();
		}

		private static bool TryGetInstance(Type type)
		{
			try
			{
				PropertyInfo p = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
				return p != null && p.GetValue(null) != null;
			}
			catch
			{
				return false;
			}
		}
	}
}
