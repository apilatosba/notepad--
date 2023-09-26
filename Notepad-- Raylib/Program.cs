using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.IO;

namespace Notepad___Raylib {
   internal class Program {
      static readonly Color TEXT_COLOR = new Color(200, 200, 200, 255);
      static readonly Color BACKGROUND_COLOR = new Color(31, 31, 31, 255);
      static int fontSize = 20;
      static int leftPadding = 12;

      static void Main(string[] args) {
         Cursor cursor = new Cursor();
         List<Line> lines = new List<Line>();
         
         Raylib.InitWindow(800, 600, "Notepad--");
         Raylib.SetExitKey(KeyboardKey.KEY_NULL);
         
         lines = ReadLinesFromFile("test.txt");
         while(!Raylib.WindowShouldClose()) {
            Raylib.BeginDrawing();
            if(Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT)) {
               cursor.position.x++;
               Console.WriteLine(cursor.position.x);
            }
            
            
            Raylib.ClearBackground(BACKGROUND_COLOR);
            var pressedKeys = GetPressedChars();

            RenderLines(lines);
            cursor.Render(lines, fontSize, leftPadding);
            
            Raylib.EndDrawing();
         }
      }

      /// <summary>
      /// Doesn't detect special keys: enter, backspace, arrow keys, tab, delete.
      /// </summary>
      /// <returns></returns>
      static List<char> GetPressedChars() {
         List<char> pressedKeys = new List<char>();
         char key;
         while((key = (char)Raylib.GetCharPressed()) != 0) {
            pressedKeys.Add(key);
         }
         return pressedKeys;
      }

      static void PrintPressedKeys(List<char> pressedKeys) {
         string keys = new string(pressedKeys.ToArray());
         if(!string.IsNullOrEmpty(keys))
            Console.WriteLine(keys);
      }

      static List<Line> ReadLinesFromFile(string path) {
         List<Line> lines = new List<Line>();
         string[] linesFromFile = File.ReadAllLines(path);
         
         for(int i = 0; i < linesFromFile.Length; i++) {
            lines.Add(new Line(linesFromFile[i], (uint)i));
         }
         return lines;
      }

      static void RenderLines(List<Line> lines) {
         for(int i = 0; i < lines.Count; i++) {
            Raylib.DrawText(lines[i].Value, leftPadding, 12 + i * Line.height, fontSize, TEXT_COLOR);
         }
      }
   }
}