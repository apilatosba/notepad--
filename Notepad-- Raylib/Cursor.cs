using Raylib_CsLo;
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
      /// <summary>
      /// In milliseconds.
      /// </summary>
      long timeSinceLastCursorInput = 0;
      /// <summary>
      /// How many milliseconds to wait before accepting input from the cursor.
      /// </summary>
      int cursorInputDelay = 30;
      /// <summary>
      /// In milliseconds. This means that cursor will wait after moving one character even if you are holding down the arrow key.
      /// </summary>
      readonly long waitTimeBeforeRapidCursorRush = 700;
      Stopwatch stopwatch = new Stopwatch();
      /// <summary>
      /// Measures how long the user has been holding down the arrow key.
      /// </summary>
      Stopwatch keyHoldTimer = new Stopwatch();
      /// <summary>
      /// Holds the number of times the cursor has moved while the user is holding down the arrow key.
      /// </summary>
      int cursorRushCounter = 0;


      public Cursor() {
         stopwatch.Start();
      }

      public void Render(in List<Line> lines, int fontSize, int leftPadding, Font font) {
         Line line = lines[position.y];
         
         string textBeforeCursor = line.Value.Substring(0, position.x);

         int distance = (int)Raylib.MeasureTextEx(font, textBeforeCursor, fontSize, 0).X;
         Raylib.DrawRectangle(leftPadding + distance, position.y * Line.height, 1, Line.height, color);
      }

      public void HandleArrowKeysNavigation(in List<Line> lines) {
         timeSinceLastCursorInput = stopwatch.ElapsedMilliseconds;

         if (timeSinceLastCursorInput < cursorInputDelay) return;

         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT)) {
            keyHoldTimer.Start();

            if (IsCursorAtEndOfFile(lines)) return;

            if (cursorRushCounter > 0 && keyHoldTimer.ElapsedMilliseconds < waitTimeBeforeRapidCursorRush) return;

            cursorRushCounter++;

            if (IsCursorAtEndOfLine(lines)) {
               position.x = 0;
               position.y++;
            } else {
               position.x++;
            }

            stopwatch.Restart();
         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT)) {
            keyHoldTimer.Start();

            if (IsCursorAtBeginningOfFile()) return;

            if (cursorRushCounter > 0 && keyHoldTimer.ElapsedMilliseconds < waitTimeBeforeRapidCursorRush) return;

            cursorRushCounter++;

            if (IsCursorAtBeginningOfLine()) {
               position.x = lines[--position.y].Value.Length;
            } else {
               position.x--;
            }

            stopwatch.Restart();
         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_UP)) {
            keyHoldTimer.Start();

            if (IsCursorAtFirstLine()) return;

            if (cursorRushCounter > 0 && keyHoldTimer.ElapsedMilliseconds < waitTimeBeforeRapidCursorRush) return;

            cursorRushCounter++;

            position.y--;
            position.x = Math.Min(position.x, lines[position.y].Value.Length);

            stopwatch.Restart();
         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_DOWN)) {
            keyHoldTimer.Start();

            if (IsCursorAtLastLine(lines)) return;

            if (cursorRushCounter > 0 && keyHoldTimer.ElapsedMilliseconds < waitTimeBeforeRapidCursorRush) return;

            cursorRushCounter++;

            position.y++;
            position.x = Math.Min(position.x, lines[position.y].Value.Length);

            stopwatch.Restart();
         }

         if (Raylib.IsKeyUp(KeyboardKey.KEY_RIGHT) &&
               Raylib.IsKeyUp(KeyboardKey.KEY_UP) &&
               Raylib.IsKeyUp(KeyboardKey.KEY_LEFT) &&
               Raylib.IsKeyUp(KeyboardKey.KEY_DOWN)
            ) {
            keyHoldTimer.Stop();
            keyHoldTimer.Reset();
            cursorRushCounter = 0;
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
