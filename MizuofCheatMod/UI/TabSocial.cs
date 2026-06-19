using MizuofCheatMod.Utils;
using UnityEngine;

namespace MizuofCheatMod.UI
{
	internal static class TabSocial
	{
		private static int _selected;
		private static readonly string[] FriendNames = {
			"克罗瓦 Crowa", "肖可拉 Chocola", "弗兰 Fran",
			"可罗奈 Cornet", "夏伊尔 Shaile", "哈希斯 Hasis", "诺亚 Noah"
		};
		private static string _bufMeet = string.Empty, _bufFav = string.Empty, _bufLove = string.Empty;
		private static string _bufConv = string.Empty, _bufDate = string.Empty, _bufPresent = string.Empty;

		internal static void Render()
		{
			ModMenu.Section("社交与好感");
			ModMenu.TwoCol(delegate
			{
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("选择角色");
					ModMenu.Gap(2f);
					_selected = GUILayout.SelectionGrid(_selected, FriendNames, 2,
					GUIStyleBuilder.GridButton, GUILayout.Height(110));
				});
				ModMenu.Card(delegate
				{
					ModMenu.BoldLabel("全部角色操作");
					ModMenu.Gap(2f);
					if (ModMenu.RoseBtn("全员好感度 MAX", 150))
					{
						Dyn friends = GameReflect.MyData.O("friendDataList");
						if (friends)
						{
							for (int i = 0; i < friends.Count; i++)
							{
								GameMethodResolver.SetFriendMax(friends[i].O("data"));
							}
							ModMenu.Label("已完成");
						}
					}
				});
			}, delegate
			{
				ModMenu.Card(delegate
				{
					string name = FriendNames[_selected];
					ModMenu.BoldLabel(name + " 好感数据");
					ModMenu.Gap(2f);
					Dyn friends = GameReflect.MyData.O("friendDataList");
					if (friends && _selected < friends.Count)
					{
						Dyn data = friends[_selected].O("data");
						if (data)
						{
							ModMenu.SmartInputRow("好感度", data, "fMeet", ref _bufMeet, v => GameMethodResolver.SetFriendField(data, "fMeet", v));
							ModMenu.SmartInputRow("亲密度", data, "fFavarite", ref _bufFav, v => GameMethodResolver.SetFriendField(data, "fFavarite", v));
							ModMenu.SmartInputRow("恋爱等级", data, "fLoveEvents", ref _bufLove, v => GameMethodResolver.SetFriendField(data, "fLoveEvents", v));
							ModMenu.Gap(2f);
							ModMenu.SmartInputRow("对话次数", data, "fConversation", ref _bufConv, v => GameMethodResolver.SetFriendField(data, "fConversation", v));
							ModMenu.SmartInputRow("约会次数", data, "fDate", ref _bufDate, v => GameMethodResolver.SetFriendField(data, "fDate", v));
							ModMenu.SmartInputRow("送礼次数", data, "fPresent", ref _bufPresent, v => GameMethodResolver.SetFriendField(data, "fPresent", v));
							ModMenu.Gap(4f);
							GUILayout.BeginHorizontal();
							if (ModMenu.RoseBtn("好感MAX", 80))
							{
								GameMethodResolver.SetFriendField(data, "fMeet", 100);
								GameMethodResolver.SetFriendField(data, "fFavarite", 100);
							}
							if (ModMenu.RoseBtn("恋爱MAX", 80))
							{
								GameMethodResolver.SetFriendField(data, "fLoveEvents", 5);
							}
							GUILayout.EndHorizontal();
						}
					}
					else
					{
						ModMenu.Label("加载中...");
					}
				});
			});
		}
	}
}
