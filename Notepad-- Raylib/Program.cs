using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Notepad___Raylib {
   internal class Program {
      static readonly Color TEXT_COLOR = new Color(200, 200, 200, 255);
      static readonly Color BACKGROUND_COLOR = new Color(31, 31, 31, 255);
      static int fontSize = 20;
      static int leftPadding = 12;

      static void Main(string[] args) {
         Cursor cursor = new Cursor();
         List<Line> lines;

         Raylib.InitWindow(800, 600, "Notepad--");
         Raylib.SetExitKey(KeyboardKey.KEY_NULL);

         Font font = Raylib.LoadFont("Fonts/Inconsolata-Medium.ttf");
         Line.height = Line.MeasureTextHeight(font, "A", fontSize);

         lines = ReadLinesFromFile("test.txt");

         while (!Raylib.WindowShouldClose()) {
            Raylib.BeginDrawing();

            Raylib.ClearBackground(BACKGROUND_COLOR);

            var pressedKeys = GetPressedCharsAsString();
            if (pressedKeys != null) {
               PrintPressedKeys(pressedKeys);
               InsertTextAtCursor(lines, cursor, pressedKeys);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE)) {
               RemoveTextAtCursor(lines, cursor, 1);
            }

            RenderLines(lines, font);

            cursor.HandleArrowKeysNavigation(lines);
            cursor.Render(lines, fontSize, leftPadding, font);

            Raylib.EndDrawing();
         }

         Raylib.UnloadFont(font);
      }

      /// <summary>
      /// Doesn't detect special keys: enter, backspace, arrow keys, tab, delete.
      /// </summary>
      /// <returns>null if nothing is pressed</returns>
      static List<char> GetPressedChars() {
         List<char> pressedKeys = new List<char>();
         char key;
         while ((key = (char)Raylib.GetCharPressed()) != 0) {
            pressedKeys.Add(key);
         }
         return pressedKeys.Count == 0 ? null : pressedKeys;
      }

      static string GetPressedCharsAsString() {
         string pressedKeys = new string(GetPressedChars()?.ToArray());
         return string.IsNullOrEmpty(pressedKeys) ? null : pressedKeys;
      }

      static void PrintPressedKeys(List<char> pressedKeys) {
         string keys = new string(pressedKeys.ToArray());
         if (!string.IsNullOrEmpty(keys))
            Console.WriteLine(keys);
      }

      static void PrintPressedKeys(string pressedKeys) {
         if (!string.IsNullOrEmpty(pressedKeys))
            Console.WriteLine(pressedKeys);
      }

      static List<Line> ReadLinesFromFile(string path) {
         List<Line> lines = new List<Line>();
         string[] linesFromFile = File.ReadAllLines(path);

         for (int i = 0; i < linesFromFile.Length; i++) {
            lines.Add(new Line(linesFromFile[i], (uint)i));
         }
         return lines;
      }

      static void RenderLines(List<Line> lines, Font font) {
         for (int i = 0; i < lines.Count; i++) {
            //Raylib.DrawText(lines[i].Value, leftPadding, i * Line.height, fontSize, TEXT_COLOR);
            Raylib.DrawTextEx(font, lines[i].Value, new Vector2(leftPadding, i * Line.height), fontSize, 0, TEXT_COLOR);
         }
      }

      static void InsertTextAtCursor(List<Line> lines, Cursor cursor, string text) {
         Line line = lines[cursor.position.y];
         line.InsertTextAt(cursor.position.x, text);

         cursor.position.x += text.Length;
      }

      static void RemoveTextAtCursor(List<Line> lines, Cursor cursor, int count, Direction direction = Direction.Left) {
         Line line = lines[cursor.position.y];
         line.RemoveTextAt(cursor.position.x, count, direction);
         cursor.position.x += direction == Direction.Left ? -count : count;
      }
   }
}
/*
make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged.
It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like
Aldus PageMaker including versions of Lorem Ipsum.

Why do we use it?
It is a long established fact that a reader will be distracted by the readable content of a page when looking at its layout.
The point of using Lorem Ipsum is that it has a more-or-less normal distribution of letters, as opposed to using 'Content here, content here', making it look
like readable English. Many desktop publishing packages and web page editors now use Lorem Ipsum as their default model text, and a search for 'lorem ipsum'
will uncover many web sites still in their infancy.
Various versions have evolved over the years, sometimes by accident, sometimes on purpose (injected humour and the like).


Where does it come from?
Contrary to popular belief, Lorem Ipsum is not simply random text.
It has roots in a piece of classical Latin literature from 45 BC, making it over 2000 years old. Richard
McClintock, a Latin professor at Hampden-Sydney College in Virginia, looked up one of
the more obscure Latin words, consectetur, from a Lorem Ipsum passage, and going through
the cites of the word in classical literature, discovered the undoubtable source.
Lorem Ipsum comes from sections 1.10.32 and 1.10.33 of "de Finibus Bonorum et Malorum"
(The Extremes of Good and Evil) by Cicero, written in 45 BC.
This book is a treatise on the theory of ethics, very popular during the Renaissance.
The first line of Lorem Ipsum, "Lorem ipsum dolor sit amet..",
comes from a line in section 1.10.32.
*/