using Raylib_CsLo;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Notepad___Raylib {
   internal class Selection {
      /// <summary>
      /// In text space
      /// </summary>
      Int2 startPosition;
      /// <summary>
      /// In text space
      /// </summary>
      Int2 endPosition;
      Color color = new Color(61, 129, 207, 102); // gpt says rgba(0, 120, 215, 102)

      public Int2 EndPosition {
         get => endPosition;
         set => endPosition = value;
      }

      public Int2 StartPosition {
         get => startPosition;
         set => startPosition = value;
      }

      public Selection() { }

      public Selection(Int2 startPosition, Int2 endPosition) {
         this.startPosition = startPosition;
         this.endPosition = endPosition;
      }

      public void Render(List<Line> lines, int fontSize, int leftPadding, Font font) {
         GetRightAndLeft(out Int2 left, out Int2 right);

         Line[] linesInRange = GetLinesInRange(lines).ToArray();
         Debug.Assert(linesInRange.Length != 0);

         switch (linesInRange.Length) {
            case 1:
               RenderLine(left, right);
               break;
            case 2:
               RenderLine(left, new Int2(linesInRange[0].Value.Length, left.y));
               RenderLine(new Int2(0, right.y), right);
               break;
            default:
               RenderLine(left, new Int2(linesInRange[0].Value.Length, left.y));

               for (int i = left.y + 1; i < right.y; i++) {
                  RenderLine(new Int2(0, i), new Int2(linesInRange[i - left.y].Value.Length, i));
               }

               RenderLine(new Int2(0, right.y), right);
               break;
         }

         void RenderLine(Int2 start, Int2 end) {
            Debug.Assert(start.y == end.y);

            Int2 left = start.x <= end.x ? start : end;
            Int2 right = start.x <= end.x ? end : start;

            Int2 leftWorldSpacePosition = GetWorldSpacePosition(left, lines, fontSize, leftPadding, font);
            Int2 rightWorldSpacePosition = GetWorldSpacePosition(right, lines, fontSize, leftPadding, font);

            Raylib.DrawRectangle(leftWorldSpacePosition.x, leftWorldSpacePosition.y, rightWorldSpacePosition.x - leftWorldSpacePosition.x, Line.Height, color);
         }
      }

      public void Render(List<Line> lines, int fontSize, int leftPadding, Font font, Color c) {
         GetRightAndLeft(out Int2 left, out Int2 right);

         Line[] linesInRange = GetLinesInRange(lines).ToArray();
         Debug.Assert(linesInRange.Length != 0);

         switch (linesInRange.Length) {
            case 1:
               RenderLine(left, right);
               break;
            case 2:
               RenderLine(left, new Int2(linesInRange[0].Value.Length, left.y));
               RenderLine(new Int2(0, right.y), right);
               break;
            default:
               RenderLine(left, new Int2(linesInRange[0].Value.Length, left.y));

               for (int i = left.y + 1; i < right.y; i++) {
                  RenderLine(new Int2(0, i), new Int2(linesInRange[i - left.y].Value.Length, i));
               }

               RenderLine(new Int2(0, right.y), right);
               break;
         }

         void RenderLine(Int2 start, Int2 end) {
            Debug.Assert(start.y == end.y);

            Int2 left = start.x <= end.x ? start : end;
            Int2 right = start.x <= end.x ? end : start;

            Int2 leftWorldSpacePosition = GetWorldSpacePosition(left, lines, fontSize, leftPadding, font);
            Int2 rightWorldSpacePosition = GetWorldSpacePosition(right, lines, fontSize, leftPadding, font);

            Raylib.DrawRectangle(leftWorldSpacePosition.x, leftWorldSpacePosition.y, rightWorldSpacePosition.x - leftWorldSpacePosition.x, Line.Height, c);
         }
      }

      public IEnumerable<Line> GetLinesInRange(List<Line> lines) {
         Int2 left;
         Int2 right;

         GetRightAndLeft(out left, out right);

         for (int i = left.y; i <= right.y; i++) {
            yield return lines[i];
         }
      }

      void GetRightAndLeft(out Int2 left, out Int2 right) {
         if (startPosition.y == endPosition.y) {
            left = startPosition.x <= endPosition.x ? startPosition : endPosition;
            right = startPosition.x <= endPosition.x ? endPosition : startPosition;
         } else {
            left = startPosition.y < endPosition.y ? startPosition : endPosition;
            right = startPosition.y < endPosition.y ? endPosition : startPosition;
         }
      }

      /// <summary>
      /// In pixels. Top left corner.
      /// </summary>
      /// <param name="position">.x = character amount, .y = line number (zero-indexed)</param>
      /// <param name="lines"></param>
      /// <param name="fontSize"></param>
      /// <param name="leftPadding"></param>
      /// <param name="font"></param>
      /// <returns></returns>
      Int2 GetWorldSpacePosition(Int2 position, in List<Line> lines, int fontSize, int leftPadding, Font font) {
         Int2 pos = new Int2();

         Line line = lines[position.y];

         string textBeforeCursor = line.Value.Substring(0, position.x);

         pos.x = (int)Raylib.MeasureTextEx(font, textBeforeCursor, fontSize, 0).X + leftPadding;
         pos.y = position.y * Line.Height;

         return pos;
      }

      /// <summary>
      /// Deletes the selection and positions the cursor at the start of the selection.
      /// </summary>
      /// <param name="lines"></param>
      /// <param name="cursor"></param>
      public void Delete(List<Line> lines, Cursor cursor) {
         GetRightAndLeft(out Int2 left, out Int2 right);
         Line[] linesInRange = GetLinesInRange(lines).ToArray();

         Debug.Assert(linesInRange.Length != 0);

         switch (linesInRange.Length) {
            case 1:
               linesInRange[0].RemoveTextAt(left.x, right.x - left.x, Direction.Right);
               break;
            case 2:
               linesInRange[0].RemoveTextAt(left.x, linesInRange[0].Value.Length - left.x, Direction.Right);
               linesInRange[1].RemoveTextAt(0, right.x, Direction.Right);

               linesInRange[0].InsertTextAt(linesInRange[0].Value.Length, linesInRange[1].Value);

               lines.RemoveAt(right.y);

               break;
            default:
               linesInRange[0].RemoveTextAt(left.x, linesInRange[0].Value.Length - left.x, Direction.Right);

               for (int i = left.y + 1; i < right.y; i++) {
                  lines.RemoveAt(left.y + 1);
               }

               linesInRange[linesInRange.Length - 1].RemoveTextAt(0, right.x, Direction.Right);

               linesInRange[0].InsertTextAt(linesInRange[0].Value.Length, linesInRange[linesInRange.Length - 1].Value);

               lines.RemoveAt(left.y + 1);

               break;
         }

         cursor.position = left;
      }

      public void Copy(in List<Line> lines) {
         string text;
         GetRightAndLeft(out Int2 left, out Int2 right);
         Line[] linesInRange = GetLinesInRange(lines).ToArray();

         switch (linesInRange.Length) {
            case 1:
               text = linesInRange[0].Value.Substring(left.x, right.x - left.x);
               break;
            case 2:
               text = linesInRange[0].Value.Substring(left.x, linesInRange[0].Value.Length - left.x) + "\n" + linesInRange[1].Value.Substring(0, right.x);
               break;
            default:
               text = linesInRange[0].Value.Substring(left.x, linesInRange[0].Value.Length - left.x);

               for (int i = 1; i < linesInRange.Length - 1; i++) {
                  text += "\n" + linesInRange[i].Value;
               }

               text += "\n" + linesInRange[linesInRange.Length - 1].Value.Substring(0, right.x);
               break;
         }

         Raylib.SetClipboardText(text);
      }

      public void Cut(List<Line> lines, Cursor cursor) {
         Copy(lines);
         Delete(lines, cursor);
      }

      /// <summary>
      /// Given string is appended to the end of the selection before copying it to the clipboard.
      /// </summary>
      /// <param name="lines"></param>
      /// <param name="textToAppend"></param>
      public void CopyAndAppend(in List<Line> lines, string textToAppend) {
         string text;
         GetRightAndLeft(out Int2 left, out Int2 right);
         Line[] linesInRange = GetLinesInRange(lines).ToArray();

         switch (linesInRange.Length) {
            case 1:
               text = linesInRange[0].Value.Substring(left.x, right.x - left.x);
               break;
            case 2:
               text = linesInRange[0].Value.Substring(left.x, linesInRange[0].Value.Length - left.x) + "\n" + linesInRange[1].Value.Substring(0, right.x);
               break;
            default:
               text = linesInRange[0].Value.Substring(left.x, linesInRange[0].Value.Length - left.x);

               for (int i = 1; i < linesInRange.Length - 1; i++) {
                  text += "\n" + linesInRange[i].Value;
               }

               text += "\n" + linesInRange[linesInRange.Length - 1].Value.Substring(0, right.x);
               break;
         }

         text += textToAppend;
         Raylib.SetClipboardText(text);
      }

      public Rectangle[] GetRectanglesInWorldSpace(List<Line> lines, int fontSize, int leftPadding, Font font) {
         GetRightAndLeft(out Int2 left, out Int2 right);

         Line[] linesInRange = GetLinesInRange(lines).ToArray();

         switch (linesInRange.Length) {
            case 1:
               return new Rectangle[] { GetRectangleLine(left, right) };
            case 2:
               return new Rectangle[] { GetRectangleLine(left, new Int2(linesInRange[0].Value.Length, left.y)), GetRectangleLine(new Int2(0, right.y), right) };
            default:
               Rectangle[] rectangles = new Rectangle[linesInRange.Length];

               rectangles[0] = GetRectangleLine(left, new Int2(linesInRange[0].Value.Length, left.y));

               for (int i = left.y + 1; i < right.y; i++) {
                  rectangles[i - left.y] = GetRectangleLine(new Int2(0, i), new Int2(linesInRange[i - left.y].Value.Length, i));
               }

               rectangles[rectangles.Length - 1] = GetRectangleLine(new Int2(0, right.y), right);

               return rectangles;
         }

         Rectangle GetRectangleLine(Int2 start, Int2 end) {
            Debug.Assert(start.y == end.y);

            Int2 left = start.x <= end.x ? start : end;
            Int2 right = start.x <= end.x ? end : start;

            Int2 leftWorldSpacePosition = GetWorldSpacePosition(left, lines, fontSize, leftPadding, font);
            Int2 rightWorldSpacePosition = GetWorldSpacePosition(right, lines, fontSize, leftPadding, font);

            return new Rectangle(leftWorldSpacePosition.x, leftWorldSpacePosition.y, rightWorldSpacePosition.x - leftWorldSpacePosition.x, Line.Height);
         }
      }
   }
}
