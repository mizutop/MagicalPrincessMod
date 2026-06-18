using System.Collections.Generic;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class GUIStyleBuilder
	{
		internal static class Palettes
		{
			internal static Color BgWarm = Hex(0xFEF6EE);
			internal static Color BgPanel = Hex(0xFFFBF5);
			internal static Color BgCard = Hex(0xFFFFFF);
			internal static Color BgField = Hex(0xF5EDE6);
			internal static Color BgHover = Hex(0xFAF0E8);
			internal static Color BgActive = Hex(0xF5E6DD);
			internal static Color FoldBg = Hex(0xFAF3EE);

			internal static Color Rose = Hex(0xE8A0B4);
			internal static Color RoseDeep = Hex(0xD4859C);
			internal static Color Lavender = Hex(0xB8A9C9);
			internal static Color Gold = Hex(0xD4A853);
			internal static Color Mint = Hex(0x8BC4A8);
			internal static Color Coral = Hex(0xE8836B);
			internal static Color Sky = Hex(0x89B8D4);

			internal static Color TextPrimary = Hex(0x3D2C2E);
			internal static Color TextSecondary = Hex(0x7A6B6D);
			internal static Color TextMuted = Hex(0xA8989A);
			internal static Color TextWhite = Hex(0xFFFBF5);

			internal static Color Divider = Hex(0xEDE0D4);
			internal static Color BorderLight = Hex(0xE8D8CC);

			internal static Color BtnRose = Hex(0xE8A0B4);
			internal static Color BtnMint = Hex(0x8BC4A8);
			internal static Color BtnGold = Hex(0xD4A853);
			internal static Color BtnCoral = Hex(0xE8836B);
			internal static Color BtnDisabled = Hex(0xD5CCCC);

			internal static Color StatusOn = Hex(0x8BC4A8);
			internal static Color StatusOff = Hex(0xC0B5B5);
		}

		internal static GUIStyle Label;
		internal static GUIStyle BoldLabel;
		internal static GUIStyle SectionTitle;
		internal static GUIStyle TabNormal;
		internal static GUIStyle TabActive;
		internal static GUIStyle StatusBar;
		internal static GUIStyle ToggleText;
		internal static GUIStyle TextField;
		internal static GUIStyle Card;
		internal static GUIStyle PillBtn;
		internal static GUIStyle ValueText;
		internal static GUIStyle Scrollbar;
		internal static GUIStyle ScrollbarThumb;
		internal static GUIStyle ScrollbarHorizontal;
		internal static GUIStyle ScrollbarHThumb;
		internal static GUIStyle GridButton;
		internal static GUIStyle TextArea;

		private static bool _initialized;
		private static readonly Dictionary<int, Texture2D> _solidTextureCache = new Dictionary<int, Texture2D>();

		internal static void Init()
		{
			if (_initialized)
			{
				return;
			}
			_initialized = true;

			Label = new GUIStyle(GUI.skin.label)
			{
				fontSize = 12,
				normal = { textColor = Palettes.TextPrimary },
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset(4, 2, 0, 0)
			};

			BoldLabel = new GUIStyle(Label)
			{
				fontStyle = FontStyle.Bold
			};

			ValueText = new GUIStyle(Label)
			{
				normal = { textColor = Palettes.Gold },
				fontStyle = FontStyle.Bold
			};

			SectionTitle = new GUIStyle(Label)
			{
				fontSize = 13,
				fontStyle = FontStyle.Bold,
				normal = { textColor = Palettes.Rose },
				padding = new RectOffset(6, 0, 4, 2)
			};

			TabNormal = new GUIStyle(GUI.skin.button)
			{
				fontSize = 11,
				normal = { textColor = Palettes.TextSecondary, background = GetSolid(Palettes.BgPanel) },
				hover = { textColor = Palettes.Rose, background = GetSolid(Palettes.BgHover) },
				active = { textColor = Palettes.RoseDeep, background = GetSolid(Palettes.BgActive) },
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset(4, 4, 1, 1),
				border = new RectOffset(0, 0, 0, 0)
			};

			TabActive = new GUIStyle(TabNormal)
			{
				normal = { textColor = Palettes.TextWhite, background = GetSolid(Palettes.Rose) },
				hover = { textColor = Palettes.TextWhite, background = GetSolid(Palettes.RoseDeep) }
			};

			StatusBar = new GUIStyle(Label)
			{
				fontSize = 10,
				normal = { textColor = Palettes.TextMuted },
				padding = new RectOffset(8, 0, 0, 0)
			};

			ToggleText = new GUIStyle(Label)
			{
				padding = new RectOffset(4, 0, 0, 0)
			};

			TextField = new GUIStyle(GUI.skin.textField)
			{
				fontSize = 11,
				normal = { textColor = Palettes.TextPrimary, background = GetSolid(Palettes.BgField) },
				focused = { textColor = Palettes.TextPrimary, background = GetSolid(Palettes.BgActive) },
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset(6, 4, 0, 0),
				border = new RectOffset(0, 0, 0, 0)
			};

			Card = new GUIStyle(GUI.skin.box)
			{
				normal = { background = GetSolid(Palettes.BgCard) },
				border = new RectOffset(0, 0, 0, 0),
				padding = new RectOffset(8, 8, 6, 6)
			};

			PillBtn = new GUIStyle(GUI.skin.button)
			{
				fontSize = 11,
				normal = { textColor = Palettes.TextWhite, background = GetSolid(Palettes.Rose) },
				hover = { textColor = Palettes.TextWhite, background = GetSolid(Palettes.RoseDeep) },
				active = { textColor = Color.white, background = GetSolid(Palettes.RoseDeep * 0.85f) },
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset(8, 8, 2, 2),
				border = new RectOffset(0, 0, 0, 0)
			};

			Scrollbar = new GUIStyle(GUI.skin.verticalScrollbar)
			{
				normal = { background = GetSolid(new Color(0.9f, 0.86f, 0.82f, 0.3f)) }
			};
			// === 滚动条滑块 ===
			Color scrollTrack = new Color(0.9f, 0.86f, 0.82f, 0.25f);
			Color scrollThumb = new Color(0.82f, 0.72f, 0.68f, 0.6f);
			Color scrollThumbHover = new Color(0.78f, 0.65f, 0.60f, 0.75f);

			Scrollbar = new GUIStyle(GUI.skin.verticalScrollbar)
			{
				normal = { background = GetSolid(scrollTrack) },
				fixedWidth = 8
			};

			ScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb)
			{
				normal = { background = GetSolid(scrollThumb) },
				hover = { background = GetSolid(scrollThumbHover) },
				fixedWidth = 8
			};

			ScrollbarHorizontal = new GUIStyle(GUI.skin.horizontalScrollbar)
			{
				normal = { background = GetSolid(scrollTrack) },
				fixedHeight = 8
			};

			ScrollbarHThumb = new GUIStyle(GUI.skin.horizontalScrollbarThumb)
			{
				normal = { background = GetSolid(scrollThumb) },
				hover = { background = GetSolid(scrollThumbHover) },
				fixedHeight = 8
			};

			// === 社交角色网格按钮 ===
			Color selectedBg = Hex(0xC88EA4);
			Color selectedHover = Hex(0xBA7D94);
			GridButton = new GUIStyle(GUI.skin.button)
			{
				fontSize = 11,
				normal = { textColor = Palettes.TextSecondary, background = GetSolid(Palettes.BgCard) },
				hover = { textColor = Palettes.Rose, background = GetSolid(Palettes.BgHover) },
				active = { textColor = Palettes.TextWhite, background = GetSolid(Palettes.Rose) },
				focused = { textColor = Palettes.RoseDeep, background = GetSolid(Palettes.BgActive) },
				onNormal = { textColor = Palettes.TextWhite, background = GetSolid(selectedBg) },
				onHover = { textColor = Palettes.TextWhite, background = GetSolid(selectedHover) },
				onActive = { textColor = Color.white, background = GetSolid(Hex(0xAC7088)) },
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset(4, 4, 3, 3),
				border = new RectOffset(1, 1, 1, 1)
			};

			// === Hook 文本区域 ===
			TextArea = new GUIStyle(GUI.skin.textArea)
			{
				fontSize = 11,
				normal = { textColor = Palettes.TextPrimary, background = GetSolid(Palettes.BgField) },
				focused = { textColor = Palettes.TextPrimary, background = GetSolid(Palettes.BgActive) },
				active = { textColor = Palettes.TextPrimary, background = GetSolid(Palettes.BgCard) },
				hover = { textColor = Palettes.TextPrimary, background = GetSolid(Palettes.BgField) },
				alignment = TextAnchor.UpperLeft,
				padding = new RectOffset(8, 8, 4, 4),
				border = new RectOffset(0, 0, 0, 0),
				wordWrap = false,
				richText = false
			};
		}

		private static Texture2D GetSolid(Color c)
		{
			int key = ColorToKey(c);
			Texture2D tex;
			if (!_solidTextureCache.TryGetValue(key, out tex) || tex == null)
			{
				tex = new Texture2D(1, 1);
				tex.hideFlags = HideFlags.HideAndDontSave;
				tex.SetPixel(0, 0, c);
				tex.Apply();
				_solidTextureCache[key] = tex;
			}
			return tex;
		}

		internal static Texture2D Solid(Color c)
		{
			return GetSolid(c);
		}

		private static int ColorToKey(Color c)
		{
			return ((int)(c.r * 255) << 24) | ((int)(c.g * 255) << 16) | ((int)(c.b * 255) << 8) | (int)(c.a * 255);
		}

		internal static void Cleanup()
		{
			foreach (Texture2D tex in _solidTextureCache.Values)
			{
				if (tex != null)
				{
					Object.Destroy(tex);
				}
			}
			_solidTextureCache.Clear();
			_initialized = false;
		}

		private static Color Hex(int hex)
		{
			return new Color(
				((hex >> 16) & 0xFF) / 255f,
				((hex >> 8) & 0xFF) / 255f,
				(hex & 0xFF) / 255f
			);
		}
	}
}
