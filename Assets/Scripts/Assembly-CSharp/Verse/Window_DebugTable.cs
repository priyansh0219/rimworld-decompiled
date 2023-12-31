using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace Verse
{
	public class Window_DebugTable : Window
	{
		private enum SortMode
		{
			Off = 0,
			Ascending = 1,
			Descending = 2
		}

		private string[,] tableRaw;

		private Vector2 scrollPosition = Vector2.zero;

		private string[,] tableSorted;

		private List<float> colWidths = new List<float>();

		private List<float> rowHeights = new List<float>();

		private int sortColumn = -1;

		private SortMode sortMode;

		private bool[] colVisible;

		private const float ColExtraWidth = 2f;

		private const float RowExtraHeight = 2f;

		private const float HiddenColumnWidth = 10f;

		private const float MouseoverOffset = 2f;

		public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);

		public Window_DebugTable(string[,] tables)
		{
			tableRaw = tables;
			colVisible = new bool[tableRaw.GetLength(0)];
			for (int i = 0; i < colVisible.Length; i++)
			{
				colVisible[i] = true;
			}
			doCloseButton = true;
			doCloseX = true;
			Text.Font = GameFont.Tiny;
			BuildTableSorted();
		}

		private void BuildTableSorted()
		{
			if (sortMode == SortMode.Off)
			{
				tableSorted = tableRaw;
			}
			else
			{
				List<List<string>> list = new List<List<string>>();
				for (int i = 1; i < tableRaw.GetLength(1); i++)
				{
					list.Add(new List<string>());
					for (int j = 0; j < tableRaw.GetLength(0); j++)
					{
						list[i - 1].Add(tableRaw[j, i]);
					}
				}
				NumericStringComparer comparer = new NumericStringComparer();
				switch (sortMode)
				{
				case SortMode.Ascending:
					list = list.OrderBy((List<string> x) => x[sortColumn], comparer).ToList();
					break;
				case SortMode.Descending:
					list = list.OrderByDescending((List<string> x) => x[sortColumn], comparer).ToList();
					break;
				case SortMode.Off:
					throw new Exception();
				}
				tableSorted = new string[tableRaw.GetLength(0), tableRaw.GetLength(1)];
				for (int k = 0; k < tableRaw.GetLength(1); k++)
				{
					for (int l = 0; l < tableRaw.GetLength(0); l++)
					{
						if (k == 0)
						{
							tableSorted[l, k] = tableRaw[l, k];
						}
						else
						{
							tableSorted[l, k] = list[k - 1][l];
						}
					}
				}
			}
			colWidths.Clear();
			for (int m = 0; m < tableRaw.GetLength(0); m++)
			{
				float item;
				if (colVisible[m])
				{
					float num = 0f;
					for (int n = 0; n < tableRaw.GetLength(1); n++)
					{
						float x2 = Text.CalcSize(tableRaw[m, n]).x;
						if (x2 > num)
						{
							num = x2;
						}
					}
					item = num + 2f;
				}
				else
				{
					item = 10f;
				}
				colWidths.Add(item);
			}
			rowHeights.Clear();
			for (int num2 = 0; num2 < tableSorted.GetLength(1); num2++)
			{
				float num3 = 0f;
				for (int num4 = 0; num4 < tableSorted.GetLength(0); num4++)
				{
					float y = Text.CalcSize(tableSorted[num4, num2]).y;
					if (y > num3)
					{
						num3 = y;
					}
				}
				rowHeights.Add(num3 + 2f);
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Tiny;
			Rect outRect = inRect;
			outRect.yMax -= 40f;
			Widgets.BeginScrollView(viewRect: new Rect(0f, 0f, colWidths.Sum(), rowHeights.Sum()), outRect: outRect, scrollPosition: ref scrollPosition);
			float num = 0f;
			for (int i = 0; i < tableSorted.GetLength(0); i++)
			{
				float num2 = 0f;
				for (int j = 0; j < tableSorted.GetLength(1); j++)
				{
					if (num2 + rowHeights[j] < outRect.yMin + scrollPosition.y || num2 > outRect.yMax + scrollPosition.y)
					{
						num2 += rowHeights[j];
						continue;
					}
					Rect rect = new Rect(num, num2, colWidths[i], rowHeights[j]);
					Rect rect2 = rect;
					rect2.xMin -= 999f;
					rect2.xMax += 999f;
					if (Mouse.IsOver(rect2) || i % 2 == 0)
					{
						Widgets.DrawHighlight(rect);
					}
					if (j == 0 && Mouse.IsOver(rect))
					{
						rect.x += 2f;
						rect.y += 2f;
					}
					if (i == 0 || colVisible[i])
					{
						Widgets.Label(rect, tableSorted[i, j]);
					}
					if (j == 0)
					{
						MouseoverSounds.DoRegion(rect);
						if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseDown)
						{
							if (Event.current.button == 0)
							{
								if (i != sortColumn)
								{
									sortMode = SortMode.Off;
								}
								switch (sortMode)
								{
								case SortMode.Off:
									sortMode = SortMode.Descending;
									sortColumn = i;
									SoundDefOf.Tick_High.PlayOneShotOnCamera();
									break;
								case SortMode.Descending:
									sortMode = SortMode.Ascending;
									sortColumn = i;
									SoundDefOf.Tick_Low.PlayOneShotOnCamera();
									break;
								case SortMode.Ascending:
									sortMode = SortMode.Off;
									sortColumn = -1;
									SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
									break;
								}
								BuildTableSorted();
							}
							else if (Event.current.button == 1)
							{
								colVisible[i] = !colVisible[i];
								SoundDefOf.Crunch.PlayOneShotOnCamera();
								BuildTableSorted();
							}
							Event.current.Use();
						}
					}
					num2 += rowHeights[j];
				}
				num += colWidths[i];
			}
			Widgets.EndScrollView();
			Text.Font = GameFont.Small;
			if (Widgets.ButtonText(new Rect(inRect.xMax - 120f, inRect.yMax - 30f, 120f, 30f), "Copy CSV"))
			{
				CopyCSVToClipboard();
				Messages.Message("Copied table data to clipboard in CSV format.", MessageTypeDefOf.PositiveEvent);
			}
		}

		private void MouseOverHeaderRowCell(Rect cell, int col)
		{
			MouseoverSounds.DoRegion(cell);
			if (!Mouse.IsOver(cell) || Event.current.type != 0)
			{
				return;
			}
			if (Event.current.button == 0)
			{
				if (col != sortColumn)
				{
					sortMode = SortMode.Off;
				}
				switch (sortMode)
				{
				case SortMode.Off:
					sortMode = SortMode.Descending;
					sortColumn = col;
					SoundDefOf.Tick_High.PlayOneShotOnCamera();
					break;
				case SortMode.Descending:
					sortMode = SortMode.Ascending;
					sortColumn = col;
					SoundDefOf.Tick_Low.PlayOneShotOnCamera();
					break;
				case SortMode.Ascending:
					sortMode = SortMode.Off;
					sortColumn = -1;
					SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
					break;
				}
				BuildTableSorted();
			}
			else if (Event.current.button == 1)
			{
				colVisible[col] = !colVisible[col];
				SoundDefOf.Crunch.PlayOneShotOnCamera();
				BuildTableSorted();
			}
			Event.current.Use();
		}

		private static void MouseCenterButtonDrag(ref Vector2 scroll)
		{
			if (Event.current.type == EventType.MouseDrag && Event.current.button == 2)
			{
				Vector2 currentEventDelta = UnityGUIBugsFixer.CurrentEventDelta;
				Event.current.Use();
				if (currentEventDelta != Vector2.zero)
				{
					scroll += -1f * currentEventDelta;
				}
			}
		}

		private static bool IsMouseOverRow(Rect cell)
		{
			cell.xMin -= 9999999f;
			cell.xMax += 9999999f;
			return Mouse.IsOver(cell);
		}

		private static bool IsMouseOverCol(Rect cell)
		{
			cell.yMin -= 9999999f;
			cell.yMax += 9999999f;
			return Mouse.IsOver(cell);
		}

		private static void DrawOpaqueLabel(Rect r, string label, bool highlighted = true, bool selectedRow = false, bool selectedCol = false, bool offsetLabel = false)
		{
			if (offsetLabel)
			{
				r.height += 2f;
			}
			Color color = GUI.color;
			GUI.color = Widgets.WindowBGFillColor;
			GUI.DrawTexture(r, BaseContent.WhiteTex);
			GUI.color = color;
			if (selectedRow)
			{
				Widgets.DrawHighlightSelected(r);
			}
			if (selectedCol)
			{
				Widgets.DrawHighlightSelected(r);
			}
			if (highlighted)
			{
				Widgets.DrawHighlight(r);
			}
			else
			{
				Widgets.DrawLightHighlight(r);
			}
			if (offsetLabel)
			{
				r.y += 2f;
			}
			Widgets.Label(r, label);
		}

		private void CopyCSVToClipboard()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < tableSorted.GetLength(1); i++)
			{
				for (int j = 0; j < tableSorted.GetLength(0); j++)
				{
					if (j != 0)
					{
						stringBuilder.Append(",");
					}
					string text = tableSorted[j, i] ?? "";
					stringBuilder.Append("\"" + text.Replace("\n", " ") + "\"");
				}
				stringBuilder.Append("\n");
			}
			GUIUtility.systemCopyBuffer = stringBuilder.ToString();
		}
	}
}
