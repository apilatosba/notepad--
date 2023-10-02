﻿using Raylib_CsLo;
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

      public Selection(Int2 startPosition, Int2 endPosition) {
         this.startPosition = startPosition;
         this.endPosition = endPosition;
      }

      public void Render(List<Line> lines, int fontSize, int leftPadding, Font font) {
         Int2 left;
         Int2 right;

         if(startPosition.y == endPosition.y) {
            left = startPosition.x <= endPosition.x ? startPosition : endPosition;
            right = startPosition.x <= endPosition.x ? endPosition : startPosition;
         } else {
            left = startPosition.y < endPosition.y ? startPosition : endPosition;
            right = startPosition.y < endPosition.y ? endPosition : startPosition;
         }

         Line[] linesInRange = GetLinesInRange().ToArray();
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

               for(int i = left.y + 1; i < right.y; i++) {
                  RenderLine(new Int2(0, i), new Int2(linesInRange[i - left.y].Value.Length, i));
               }

               RenderLine(new Int2(0, right.y), right);
               break;
         }

         IEnumerable<Line> GetLinesInRange() {
            for(int i = left.y; i <= right.y; i++) {
               yield return lines[i];
            }
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

      void Delete() {
         throw new System.NotImplementedException();
      }
   }
}
