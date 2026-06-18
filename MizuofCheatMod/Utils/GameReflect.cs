using System;
using System.Collections.Concurrent;
using System.Reflection;
using MelonLoader;

namespace MizuofCheatMod.Utils
{
	internal sealed class Dyn
	{
		public object Obj { get; }

		public Dyn(object obj)
		{
			Obj = obj;
		}

		public static Dyn Of(object obj)
		{
			return new Dyn(obj);
		}

		public int I(string field)
		{
			return GameReflect.GetInt(Obj, field);
		}

		public void SI(string field, int val)
		{
			GameReflect.SetInt(Obj, field, val);
		}

		public Dyn O(string field)
		{
			return new Dyn(GameReflect.GetFieldObj(Obj, field));
		}

		public string S(string field)
		{
			return GameReflect.GetString(Obj, field);
		}

		public void SE(string field, string val)
		{
			GameReflect.SetString(Obj, field, val);
		}

		public float F(string field)
		{
			return GameReflect.GetFloat(Obj, field);
		}

		public void SF(string field, float val)
		{
			GameReflect.SetFloat(Obj, field, val);
		}

		public void SEnum(string field, object enumValue)
		{
			GameReflect.SetEnum(Obj, field, enumValue);
		}

		public int EInt(string field)
		{
			return GameReflect.GetEnumInt(Obj, field);
		}

		public void CM(string method, params object[] args)
		{
			GameReflect.Call(Obj, method, args);
		}

		public Dyn CMr(string method, params object[] args)
		{
			return new Dyn(GameReflect.Call(Obj, method, args));
		}

		public int Count
		{
			get
			{
				if (Obj == null)
				{
					return 0;
				}
				try
				{
					if (Obj is System.Collections.ICollection col)
					{
						return col.Count;
					}
					PropertyInfo p = GameReflect.GetCachedProperty(Obj.GetType(), "Count");
					if (p != null)
					{
						return (int)p.GetValue(Obj, null);
					}
					return 0;
				}
				catch
				{
					return 0;
				}
			}
		}

		public Dyn this[int idx]
		{
			get
			{
				if (Obj == null)
				{
					return Of(null);
				}
				try
				{
					if (Obj is System.Collections.IList list)
					{
						return Of(list[idx]);
					}
					MethodInfo m = GameReflect.GetCachedMethod(Obj.GetType(), "get_Item", typeof(int));
					if (m != null)
					{
						return Of(m.Invoke(Obj, new object[] { idx }));
					}
					PropertyInfo p = GameReflect.GetCachedProperty(Obj.GetType(), "Item");
					if (p != null)
					{
						return Of(p.GetValue(Obj, new object[] { idx }));
					}
					return Of(null);
				}
				catch
				{
					return Of(null);
				}
			}
		}

		public override string ToString()
		{
			return Obj?.ToString() ?? "null";
		}

		public static implicit operator bool(Dyn d)
		{
			return d?.Obj != null;
		}
	}

	internal static class GameReflect
	{
		private static Assembly _assemblyCache;
		private static Type _myDataTypeCache;
		private static readonly ConcurrentDictionary<string, FieldInfo> _fieldCache = new ConcurrentDictionary<string, FieldInfo>();
		private static readonly ConcurrentDictionary<string, PropertyInfo> _propertyCache = new ConcurrentDictionary<string, PropertyInfo>();
		private static readonly ConcurrentDictionary<string, MethodInfo> _methodCache = new ConcurrentDictionary<string, MethodInfo>();
		private static readonly ConcurrentDictionary<string, Type> _typeCache = new ConcurrentDictionary<string, Type>();

		private static Assembly GameAssembly
		{
			get
			{
				if (_assemblyCache == null)
				{
					Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
					foreach (Assembly a in assemblies)
					{
						if (a.GetName().Name == "Assembly-CSharp")
						{
							_assemblyCache = a;
							break;
						}
					}
				}
				return _assemblyCache;
			}
		}

		internal static Dyn MyData
		{
			get
			{
				return Dyn.Of(GetMyData());
			}
		}

		internal static Dyn Status
		{
			get
			{
				return Dyn.Of(GetStatus());
			}
		}

		internal static Dyn GStatus
		{
			get
			{
				return Dyn.Of(GetGStatus());
			}
		}

		internal static object GetMyData()
		{
			try
			{
				if (_myDataTypeCache == null)
				{
					_myDataTypeCache = ResolveType("MyData");
				}
				if (_myDataTypeCache == null)
				{
					return null;
				}

				// 尝试1: 通过静态属性 Instance 获取单例
				PropertyInfo p = GetCachedProperty(_myDataTypeCache, "Instance");
				object instance = p?.GetValue(null, null);
				if (instance != null)
				{
					return instance;
				}

				// 尝试2: 通过静态字段 Instance 获取单例 (某些游戏使用字段而非属性)
				FieldInfo f = _myDataTypeCache.GetField("Instance",
					BindingFlags.Public | BindingFlags.Static);
				instance = f?.GetValue(null);
				if (instance != null)
				{
					return instance;
				}

				// 尝试3: 通过 FindObjectOfType / FindFirstObjectByType 在场景中查找 (兜底方案)
				// 注意: Unity 2023+ 弃用了 FindObjectOfType, 改用 FindFirstObjectByType
				// 且 FindObjectOfType 是静态方法，GetMethod 默认只搜实例方法，需加 BindingFlags.Static
				try
				{
					Type objType = typeof(UnityEngine.Object);
					System.Reflection.MethodInfo findMethod = null;

					// 3a: Unity 2023+ 新 API
					findMethod = objType.GetMethod("FindFirstObjectByType",
						BindingFlags.Public | BindingFlags.Static, null,
						new Type[] { typeof(Type) }, null);
					if (findMethod == null)
					{
						// 3b: 旧 API (带 includeInactive 重载)
						findMethod = objType.GetMethod("FindObjectOfType",
							BindingFlags.Public | BindingFlags.Static, null,
							new Type[] { typeof(Type), typeof(bool) }, null);
					}
					if (findMethod == null)
					{
						// 3c: 最旧的 API (仅 Type 参数)
						findMethod = objType.GetMethod("FindObjectOfType",
							BindingFlags.Public | BindingFlags.Static, null,
							new Type[] { typeof(Type) }, null);
					}

					if (findMethod != null)
					{
						if (findMethod.GetParameters().Length == 2)
						{
							instance = findMethod.Invoke(null, new object[] { _myDataTypeCache, true });
						}
						else
						{
							instance = findMethod.Invoke(null, new object[] { _myDataTypeCache });
						}
						if (instance != null)
						{
							return instance;
						}
					}
				}
				catch
				{
					// FindObjectOfType 失败，忽略
				}

				// 所有方案都失败，清除缓存以便下次重试
				_myDataTypeCache = null;
				return null;
			}
			catch
			{
				_myDataTypeCache = null;
				return null;
			}
		}

		internal static object GetStatus()
		{
			object m = GetMyData();
			if (m == null)
			{
				return null;
			}
			return GetFieldObj(m, "status");
		}

		internal static object GetGStatus()
		{
			object m = GetMyData();
			if (m == null)
			{
				return null;
			}
			return GetFieldObj(m, "gstatus");
		}

		internal static Dyn GetSingleton(string className)
		{
			try
			{
				Type t = ResolveType(className);
				if (t == null)
				{
					return null;
				}
				PropertyInfo p = GetCachedProperty(t, "Instance");
				return Dyn.Of(p?.GetValue(null, null));
			}
			catch
			{
				return null;
			}
		}

		internal static Type ResolveType(string className)
		{
			// 尝试从缓存获取
			if (_typeCache.TryGetValue(className, out Type cached) && cached != null)
			{
				return cached;
			}
			// 不缓存null，每次重新查找
			Assembly asm = GameAssembly;
			if (asm == null)
			{
				return null;
			}
			Type t = asm.GetType(className);
			if (t == null)
			{
				Type[] allTypes = asm.GetTypes();
				foreach (Type candidate in allTypes)
				{
					if (candidate.Name == className || candidate.FullName == className)
					{
						t = candidate;
						break;
					}
				}
			}
			if (t != null)
			{
				_typeCache[className] = t;
			}
			return t;
		}

		internal static FieldInfo GetCachedField(Type type, string fieldName)
		{
			string key = type.FullName + "." + fieldName;
			return _fieldCache.GetOrAdd(key, _ =>
			{
				FieldInfo f = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
				return f;
			});
		}

		internal static PropertyInfo GetCachedProperty(Type type, string propName)
		{
			string key = type.FullName + "." + propName;
			return _propertyCache.GetOrAdd(key, _ =>
			{
				PropertyInfo p = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
				return p;
			});
		}

		internal static MethodInfo GetCachedMethod(Type type, string methodName, params Type[] paramTypes)
		{
			string key = type.FullName + "." + methodName + "(" + string.Join(",", (object[])paramTypes ?? Array.Empty<object>()) + ")";
			return _methodCache.GetOrAdd(key, _ =>
			{
				if (paramTypes != null && paramTypes.Length > 0)
				{
					return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, paramTypes, null);
				}
				MethodInfo m = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
				return m;
			});
		}

		internal static int GetInt(object obj, string field)
		{
			if (obj == null)
			{
				return 0;
			}
			try
			{
				FieldInfo f = GetCachedField(obj.GetType(), field);
				if (f != null)
				{
					object val = f.GetValue(obj);
					if (f.FieldType == typeof(bool))
					{
						return (bool)val ? 1 : 0;
					}
					return (int)val;
				}
				PropertyInfo p = GetCachedProperty(obj.GetType(), field);
				if (p != null)
				{
					object val = p.GetValue(obj, null);
					if (p.PropertyType == typeof(bool))
					{
						return (bool)val ? 1 : 0;
					}
					return (int)val;
				}
				return 0;
			}
			catch
			{
				return 0;
			}
		}

		internal static void SetInt(object obj, string field, int val)
		{
			if (obj == null)
			{
				return;
			}
			try
			{
				Type t = obj.GetType();
				FieldInfo f = GetCachedField(t, field);
				if (f != null)
				{
					if (f.FieldType.IsEnum)
					{
						f.SetValue(obj, Enum.ToObject(f.FieldType, val));
						return;
					}
					if (f.FieldType == typeof(bool))
					{
						f.SetValue(obj, val != 0);
						return;
					}
					f.SetValue(obj, val);
					return;
				}
				PropertyInfo p = GetCachedProperty(t, field);
				if (p != null)
				{
					if (p.PropertyType.IsEnum)
					{
						p.SetValue(obj, Enum.ToObject(p.PropertyType, val), null);
						return;
					}
					if (p.PropertyType == typeof(bool))
					{
						p.SetValue(obj, val != 0, null);
						return;
					}
					p.SetValue(obj, val, null);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Msg("[Reflect] SetInt(" + field + "): " + ex.Message);
			}
		}

		internal static void SetEnum(object obj, string field, object enumValue)
		{
			if (obj == null)
			{
				return;
			}
			try
			{
				FieldInfo f = GetCachedField(obj.GetType(), field);
				if (f != null)
				{
					f.SetValue(obj, enumValue);
					return;
				}
				PropertyInfo p = GetCachedProperty(obj.GetType(), field);
				p?.SetValue(obj, enumValue, null);
			}
			catch (Exception ex)
			{
				MelonLogger.Msg("[Reflect] SetEnum(" + field + "): " + ex.Message);
			}
		}

		internal static int GetEnumInt(object obj, string field)
		{
			if (obj == null)
			{
				return 0;
			}
			try
			{
				FieldInfo f = GetCachedField(obj.GetType(), field);
				if (f != null)
				{
					return (int)f.GetValue(obj);
				}
				PropertyInfo p = GetCachedProperty(obj.GetType(), field);
				if (p != null)
				{
					return (int)p.GetValue(obj, null);
				}
				return 0;
			}
			catch
			{
				return 0;
			}
		}

		internal static string GetString(object obj, string field)
		{
			if (obj == null)
			{
				return "";
			}
			try
			{
				FieldInfo f = GetCachedField(obj.GetType(), field);
				if (f != null)
				{
					return f.GetValue(obj)?.ToString() ?? "";
				}
				PropertyInfo p = GetCachedProperty(obj.GetType(), field);
				return p?.GetValue(obj, null)?.ToString() ?? "";
			}
			catch
			{
				return "";
			}
		}

		internal static void SetString(object obj, string field, string val)
		{
			if (obj == null)
			{
				return;
			}
			try
			{
				FieldInfo f = GetCachedField(obj.GetType(), field);
				if (f != null)
				{
					f.SetValue(obj, val);
					return;
				}
				PropertyInfo p = GetCachedProperty(obj.GetType(), field);
				p?.SetValue(obj, val, null);
			}
			catch
			{
			}
		}

		internal static float GetFloat(object obj, string field)
		{
			if (obj == null) return 0f;
			try
			{
				FieldInfo f = GetCachedField(obj.GetType(), field);
				if (f != null) return (float)f.GetValue(obj);
				PropertyInfo p = GetCachedProperty(obj.GetType(), field);
				if (p != null) return (float)p.GetValue(obj, null);
				return 0f;
			}
			catch { return 0f; }
		}

		internal static void SetFloat(object obj, string field, float val)
		{
			if (obj == null) return;
			try
			{
				FieldInfo f = GetCachedField(obj.GetType(), field);
				if (f != null) { f.SetValue(obj, val); return; }
				PropertyInfo p = GetCachedProperty(obj.GetType(), field);
				p?.SetValue(obj, val, null);
			}
			catch { }
		}

		internal static object GetFieldObj(object obj, string field)
		{
			if (obj == null)
			{
				return null;
			}
			try
			{
				FieldInfo f = GetCachedField(obj.GetType(), field);
				if (f != null)
				{
					return f.GetValue(obj);
				}
				PropertyInfo p = GetCachedProperty(obj.GetType(), field);
				return p?.GetValue(obj, null);
			}
			catch
			{
				return null;
			}
		}

		internal static void SetField(object obj, string field, object val)
		{
			if (obj == null)
			{
				return;
			}
			try
			{
				FieldInfo f = GetCachedField(obj.GetType(), field);
				if (f != null)
				{
					f.SetValue(obj, val);
					return;
				}
				PropertyInfo p = GetCachedProperty(obj.GetType(), field);
				p?.SetValue(obj, val, null);
			}
			catch
			{
			}
		}

		internal static object Call(object obj, string method, params object[] args)
		{
			if (obj == null)
			{
				return null;
			}
			try
			{
				Type t = obj.GetType();
				Type[] types = args == null ? Type.EmptyTypes : new Type[args.Length];
				for (int i = 0; i < (args?.Length ?? 0); i++)
				{
					types[i] = args[i]?.GetType() ?? typeof(object);
				}
				MethodInfo m = GetCachedMethod(t, method, types);
				if (m != null)
				{
					return m.Invoke(obj, args);
				}
				foreach (MethodInfo candidate in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
				{
					if (candidate.Name != method)
					{
						continue;
					}
					ParameterInfo[] parms = candidate.GetParameters();
					if (parms.Length != (args?.Length ?? 0))
					{
						continue;
					}
					return candidate.Invoke(obj, args);
				}
				return null;
			}
			catch (Exception ex)
			{
				MelonLogger.Msg("[Reflect] Call " + method + ": " + ex.Message);
				return null;
			}
		}

		internal static void InvalidateCache()
		{
			_assemblyCache = null;
			_myDataTypeCache = null;
			_fieldCache.Clear();
			_propertyCache.Clear();
			_methodCache.Clear();
			_typeCache.Clear();
		}
	}
}
