﻿using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Notepad___Raylib {
   internal class Cursor {
      /// <summary>
      /// position.x is how many characters from the left of the line the cursor is.<br />
      /// position.y is line number.
      /// </summary>
      public Int2 position;
      Color color = new Color(150, 150, 150, 255);

      public void Render(in List<Line> lines, int fontSize, int leftPadding, Font font) {
         Line line = lines[position.y];
         
         string textBeforeCursor = line.Value.Substring(0, position.x);

         int distance = (int)Raylib.MeasureTextEx(font, textBeforeCursor, fontSize, 0).X;
         Raylib.DrawRectangle(leftPadding + distance, position.y * Line.height, 1, Line.height, color);
      }

      public void HandleArrowKeysNavigation(in List<Line> lines) {
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT)) {

            if (IsCursorAtEndOfFile(lines)) return;

            if (IsCursorAtEndOfLine(lines)) {
               position.x = 0;
               position.y++;
            } else {
               position.x++;
            }

         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT)) {

            if (IsCursorAtBeginningOfFile()) return;

            if (IsCursorAtBeginningOfLine()) {
               position.x = lines[--position.y].Value.Length;
            } else {
               position.x--;
            }

         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_UP)) {

            if (IsCursorAtFirstLine()) return;

            position.y--;
            position.x = Math.Min(position.x, lines[position.y].Value.Length);

         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_DOWN)) {

            if (IsCursorAtLastLine(lines)) return;

            position.y++;
            position.x = Math.Min(position.x, lines[position.y].Value.Length);

         }
      }

      public bool IsCursorAtEndOfLine(in List<Line> lines) {
         return position.x == lines[position.y].Value.Length;
      }

      public bool IsCursorAtBeginningOfLine() {
         return position.x == 0;
      }

      public bool IsCursorAtBeginningOfFile() {
         return position.y == 0 && IsCursorAtBeginningOfLine();
      }

      public bool IsCursorAtEndOfFile(in List<Line> lines) {
         return (lines.Count - 1 == position.y) && IsCursorAtEndOfLine(lines);
      }

      public bool IsCursorAtFirstLine() {
         return position.y == 0;
      }

      public bool IsCursorAtLastLine(in List<Line> lines) {
         return position.y == lines.Count - 1;
      }
   }
}
