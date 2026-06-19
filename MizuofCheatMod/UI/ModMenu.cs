using System;
using MelonLoader;
using UnityEngine;
using MizuofCheatMod.Utils;

namespace MizuofCheatMod.UI
{
	internal static class ModMenu
	{
		private const int PW = 920;
		private const int PH = 560;
		private const int TB = 26;
		private const int SB = 22;
		private const int CH = PH - TB - SB;
		internal const int ColW = 430;
		internal const int ColGap = 10;

		internal static int ActiveTab { get; private set; }
		private static Vector2 _scroll;
		private static Rect _winRect;
		private static bool _sizeOk;
		private static bool _renderFail;
		private static float _retryTime;

		private static readonly string[] TabNames = {
			"玩家", "物品", "时间", "战斗",
			"社交", "地点", "课程", "成就", "HOOK", "结局/事件"
		};

		internal static void SetActiveTab(int idx)
		{
			if (idx >= 0 && idx < TabNames.Length)
			{
				ActiveTab = idx;
				_scroll = Vector2.zero;
			}
		}

		private static GUIStyle _overlayStyle;
		private static GUIStyle _panelBgStyle;
		private static GUIStyle _panelBdrStyle;
		private static bool _stylesCached;

		private static void EnsureStyles()
		{
			if (_stylesCached)
			{
				return;
			}
			_stylesCached = true;
			_overlayStyle = BgStyle(new Color(1f, 0.96f, 0.93f, 0.92f));
			_panelBgStyle = BgStyle(GUIStyleBuilder.Palettes.BgPanel);
			_panelBdrStyle = BdrStyle();
		}

		internal static void RenderMainWindow()
		{
			if (_renderFail && Time.realtimeSinceStartup - _retryTime < 5f)
			{
				return;
			}
			try
			{
				EnsureStyles();
				if (!_sizeOk || Math.Abs(Screen.width - _winRect.width) > 10)
				{
					_winRect = new Rect((Screen.width - PW) / 2f, (Screen.height - PH) / 2f, PW, PH);
					_sizeOk = true;
				}
				Color prevBg = GUI.backgroundColor;
				Color prevContent = GUI.contentColor;

				GUI.Box(new Rect(0, 0, Screen.width, Screen.height), string.Empty, _overlayStyle);

				GUI.BeginGroup(_winRect);
				GUI.Box(new Rect(0, 0, PW, PH), string.Empty, _panelBgStyle);
				GUI.Box(new Rect(0, 0, PW, PH), string.Empty, _panelBdrStyle);

				RenderTabBar();
				RenderContent();
				RenderStatusBar();

				GUI.EndGroup();
				GUI.backgroundColor = prevBg;
				GUI.contentColor = prevContent;
			}
			catch (Exception ex)
			{
				MelonLogger.Msg("[ModMenu] " + ex.Message);
				_renderFail = true;
				_retryTime = Time.realtimeSinceStartup;
				RecoverLayout();
			}
		}

		private static void RecoverLayout()
		{
			try
			{
				int max = 10;
				while (max-- > 0)
				{
					GUILayout.EndScrollView();
				}
			}
			catch
			{
			}
			try
			{
				int max = 10;
				while (max-- > 0)
				{
					GUILayout.EndVertical();
				}
			}
			catch
			{
			}
			try
			{
				int max = 10;
				while (max-- > 0)
				{
					GUILayout.EndHorizontal();
				}
			}
			catch
			{
			}
		}

		private static GUIStyle BgStyle(Color c)
		{
			GUIStyle s = new GUIStyle(GUI.skin.box)
			{
				normal = { background = GUIStyleBuilder.Solid(c) },
				border = new RectOffset(0, 0, 0, 0),
				padding = new RectOffset(0, 0, 0, 0),
				margin = new RectOffset(0, 0, 0, 0)
			};
			return s;
		}

		private static GUIStyle BdrStyle()
		{
			GUIStyle s = new GUIStyle(GUI.skin.box)
			{
				normal = { background = GUIStyleBuilder.Solid(GUIStyleBuilder.Palettes.BorderLight) },
				border = new RectOffset(1, 1, 1, 1),
				padding = new RectOffset(0, 0, 0, 0)
			};
			return s;
		}

		private static void RenderTabBar()
		{
			GUILayout.BeginHorizontal(GUILayout.Height(TB));
			GUILayout.Space(4);
			for (int i = 0; i < TabNames.Length; i++)
			{
				bool active = i == ActiveTab;
				if (GUILayout.Button(TabNames[i],
					active ? GUIStyleBuilder.TabActive : GUIStyleBuilder.TabNormal,
					GUILayout.Height(TB - 2)))
				{
					SetActiveTab(i);
				}
				GUILayout.Space(2);
			}
			GUILayout.FlexibleSpace();
			GUILayout.Space(4);
			GUILayout.EndHorizontal();
			GUILayout.Space(4);
		}

		private static void RenderContent()
		{
			GUILayout.BeginVertical(GUILayout.Height(CH));

			// 临时替换GUI.skin滚动条样式
			GUIStyle savedVScroll = GUI.skin.verticalScrollbar;
			GUIStyle savedVThumb = GUI.skin.verticalScrollbarThumb;
			GUIStyle savedHScroll = GUI.skin.horizontalScrollbar;
			GUIStyle savedHThumb = GUI.skin.horizontalScrollbarThumb;
			GUI.skin.verticalScrollbar = GUIStyleBuilder.Scrollbar;
			GUI.skin.verticalScrollbarThumb = GUIStyleBuilder.ScrollbarThumb;
			GUI.skin.horizontalScrollbar = GUIStyleBuilder.ScrollbarHorizontal;
			GUI.skin.horizontalScrollbarThumb = GUIStyleBuilder.ScrollbarHThumb;

			_scroll = GUILayout.BeginScrollView(_scroll, false, true,
				GUILayout.Width(PW), GUILayout.Height(CH));

			GUILayout.BeginHorizontal();
			GUILayout.Space(8);
			GUILayout.BeginVertical(GUILayout.Width(PW - 16));
			try
			{
				switch (ActiveTab)
				{
					case 0:
						TabPlayer.Render();
						break;
					case 1:
						TabItems.Render();
						break;
					case 2:
						TabTime.Render();
						break;
					case 3:
						TabCombat.Render();
						break;
					case 4:
						TabSocial.Render();
						break;
					case 5:
						TabLocation.Render();
						break;
					case 6:
						TabAcademy.Render();
						break;
					case 7:
						TabAchievement.Render();
						break;
					case 8:
						TabHook.Render();
						break;
					case 9:
						TabEvents.Render();
						break;
				}
			}
			catch (Exception ex)
			{
				GUILayout.Label("Error: " + ex.Message, GUIStyleBuilder.Label);
			}
			GUILayout.EndVertical();
			GUILayout.Space(8);
			GUILayout.EndHorizontal();

			GUILayout.EndScrollView();

			// 恢复原始滚动条样式
			GUI.skin.verticalScrollbar = savedVScroll;
			GUI.skin.verticalScrollbarThumb = savedVThumb;
			GUI.skin.horizontalScrollbar = savedHScroll;
			GUI.skin.horizontalScrollbarThumb = savedHThumb;

			GUILayout.EndVertical();
		}

		private static void RenderStatusBar()
		{
			GUILayout.BeginHorizontal(GUILayout.Height(SB));
			GUILayout.Space(8);
			GUILayout.Label("F1=面板  F2=快照  F3=Hook  F4=验证", GUIStyleBuilder.StatusBar);
			GUILayout.FlexibleSpace();
			int en = FeatureRegistry.EnabledCount;
			int tot = FeatureRegistry.TotalCount;
			int vf = FeatureRegistry.VerifiedCount;
			GUILayout.Label("[" + en + "/" + tot + "] 开启  [" + vf + "] 已验证  Tab " + ActiveTab,
				GUIStyleBuilder.StatusBar);
			GUILayout.Space(8);
			GUILayout.EndHorizontal();
		}

		// ===== 面板交互组件 (无分隔线版本) =====

		internal static void Section(string title)
		{
			Color prev = GUI.contentColor;
			GUI.contentColor = GUIStyleBuilder.Palettes.Rose;
			GUILayout.Label("  " + title, GUIStyleBuilder.SectionTitle);
			GUI.contentColor = prev;
			GUILayout.Space(4);
		}

		internal static void Card(Action content)
		{
			Color prev = GUI.backgroundColor;
			GUI.backgroundColor = GUIStyleBuilder.Palettes.BgCard;
			GUILayout.BeginVertical(GUIStyleBuilder.Card);
			content?.Invoke();
			GUILayout.EndVertical();
			GUILayout.Space(6);
			GUI.backgroundColor = prev;
		}

		internal static bool Toggle(string label, bool cur, string desc)
		{
			GUILayout.BeginHorizontal(GUILayout.Height(20));
			Color prevC = GUI.contentColor;
			Color prevB = GUI.backgroundColor;

			GUI.contentColor = cur ? GUIStyleBuilder.Palettes.StatusOn : GUIStyleBuilder.Palettes.StatusOff;
			GUILayout.Label(cur ? "[ON]" : "[OFF]", GUILayout.Width(40));
			GUI.contentColor = prevC;

			GUILayout.Label(label, GUIStyleBuilder.ToggleText, GUILayout.Width(160));

			GUI.backgroundColor = cur ? GUIStyleBuilder.Palettes.BtnMint : GUIStyleBuilder.Palettes.BtnDisabled;
			string txt = cur ? "关闭" : "开启";
			bool click = GUILayout.Button(txt, GUIStyleBuilder.PillBtn, GUILayout.Width(50), GUILayout.Height(18));
			GUI.backgroundColor = prevB;

			GUILayout.EndHorizontal();
			return click;
		}

		internal static bool PillBtn(string text, Color color, int width = -1)
		{
			Color prev = GUI.backgroundColor;
			GUI.backgroundColor = color;
			bool result = width > 0
				? GUILayout.Button(text, GUIStyleBuilder.PillBtn, GUILayout.Width(width), GUILayout.Height(20))
				: GUILayout.Button(text, GUIStyleBuilder.PillBtn, GUILayout.Height(20));
			GUI.backgroundColor = prev;
			return result;
		}

		internal static bool RoseBtn(string text, int width = -1)
		{
			return PillBtn(text, GUIStyleBuilder.Palettes.BtnRose, width);
		}

		internal static bool GoldBtn(string text, int width = -1)
		{
			return PillBtn(text, GUIStyleBuilder.Palettes.BtnGold, width);
		}

		internal static bool CoralBtn(string text, int width = -1)
		{
			return PillBtn(text, GUIStyleBuilder.Palettes.BtnCoral, width);
		}

		internal static bool MintBtn(string text, int width = -1)
		{
			return PillBtn(text, GUIStyleBuilder.Palettes.BtnMint, width);
		}

		internal static void InputRow(string label, Dyn target, string field, ref string buf)
		{
			GUILayout.BeginHorizontal(GUILayout.Height(20));
			GUILayout.Label(label, GUIStyleBuilder.ToggleText, GUILayout.Width(100));
			if (target?.Obj != null)
			{
				int val = target.I(field);
				GUILayout.Label(val.ToString(), GUIStyleBuilder.ValueText, GUILayout.Width(50));
			}
			buf = GUILayout.TextField(buf ?? string.Empty, GUIStyleBuilder.TextField,
				GUILayout.Width(70), GUILayout.Height(18));
			if (RoseBtn("Set", 44) && target?.Obj != null && int.TryParse(buf, out int parsed))
			{
				target.SI(field, parsed);
			}
			GUILayout.EndHorizontal();
		}

		/// <summary>
		/// 使用 GameMethodResolver 优先调用游戏方法的输入行
		/// </summary>
		internal static void SmartInputRow(string label, Dyn target, string field, ref string buf, Action<int> resolverSet)
		{
			GUILayout.BeginHorizontal(GUILayout.Height(20));
			GUILayout.Label(label, GUIStyleBuilder.ToggleText, GUILayout.Width(100));
			if (target?.Obj != null)
			{
				int val = target.I(field);
				GUILayout.Label(val.ToString(), GUIStyleBuilder.ValueText, GUILayout.Width(50));
			}
			buf = GUILayout.TextField(buf ?? string.Empty, GUIStyleBuilder.TextField,
				GUILayout.Width(70), GUILayout.Height(18));
			if (RoseBtn("Set", 44) && target?.Obj != null && int.TryParse(buf, out int parsed))
			{
				resolverSet?.Invoke(parsed);
			}
			GUILayout.EndHorizontal();
		}

		internal static void InputRowEnum(string label, Dyn target, string field, ref string buf)
		{
			GUILayout.BeginHorizontal(GUILayout.Height(20));
			GUILayout.Label(label, GUIStyleBuilder.ToggleText, GUILayout.Width(100));
			if (target?.Obj != null)
			{
				int val = target.EInt(field);
				GUILayout.Label(val.ToString(), GUIStyleBuilder.ValueText, GUILayout.Width(50));
			}
			buf = GUILayout.TextField(buf ?? string.Empty, GUIStyleBuilder.TextField,
				GUILayout.Width(70), GUILayout.Height(18));
			if (RoseBtn("Set", 44) && target?.Obj != null && int.TryParse(buf, out int parsed))
			{
				target.SI(field, parsed);
			}
			GUILayout.EndHorizontal();
		}

		internal static void Label(string text)
		{
			GUILayout.Label(text, GUIStyleBuilder.Label);
		}

		internal static void BoldLabel(string text)
		{
			GUILayout.Label(text, GUIStyleBuilder.BoldLabel);
		}

		internal static void ValueLabel(string text)
		{
			GUILayout.Label(text, GUIStyleBuilder.ValueText);
		}

		internal static void Col(Action content)
		{
			GUILayout.BeginVertical(GUILayout.Width(ColW));
			content?.Invoke();
			GUILayout.EndVertical();
		}

		internal static void TwoCol(Action left, Action right)
		{
			GUILayout.BeginHorizontal();
			Col(left);
			GUILayout.Space(ColGap);
			Col(right);
			GUILayout.EndHorizontal();
		}

		internal static void Gap(float px = 4f)
		{
			GUILayout.Space(px);
		}
	}
}
