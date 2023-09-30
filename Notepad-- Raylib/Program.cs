#define VISUAL_STUDIO
using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;

namespace Notepad___Raylib {
   internal class Program {
      static readonly Color TEXT_COLOR = new Color(200, 200, 200, 255);
      static readonly Color BACKGROUND_COLOR = new Color(31, 31, 31, 255);
      public static int fontSize = 20;
      static int leftPadding = 12;
      /// <summary>
      /// In milliseconds.
      /// </summary>
      static long timeSinceLastInput = 0;
      /// <summary>
      /// How many milliseconds to wait before accepting new input.
      /// </summary>
      static int inputDelay = 30;
      /// <summary>
      /// In milliseconds. This means that writing/deleting/(moving cursor) will wait after moving one character even if you are holding down the key.
      /// </summary>
      readonly static long waitTimeBeforeRapidInputRush = 700;
      static Stopwatch lastInputTimer = new Stopwatch();
      /// <summary>
      /// Measures how long the user has been holding down key.
      /// </summary>
      static Stopwatch keyHoldTimer = new Stopwatch();
      /// <summary>
      /// Holds the number of times the cursor has moved while the user is holding down the arrow key.
      /// </summary>
      static int inputRushCounter = 0;
      public static List<Line> lines;
      public static Font font;
      static int tabSize = 4;
      static string filePath;
      static int spacingBetweenLines = 2;
#if VISUAL_STUDIO
      static readonly string customFontsDirectory = "Fonts";
#else
      static readonly string customFontsDirectory = Path.Combine(GetExecutableDirectory(), "Fonts");
#endif

      static void Main(string[] args) {
#if VISUAL_STUDIO
#else
         Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_FATAL);
#endif

         // Command line arguments
         {
            if (args.Length == 0) {
               Console.WriteLine("ERROR: No file specified\n");
               PrintHelp();
               return;
            }

            for (int i = 0; i < args.Length; i++) {
               switch (args[i]) {
                  case "-h":
                  case "--help":
                     PrintHelp();
                     return;
                  default:
                     if (File.Exists(args[i])) {
                        if (lines == null) {
                           lines = ReadLinesFromFile(args[i]);
                           filePath = args[i];
                        } else {
                           Console.WriteLine("ERROR: Specify only one file\n");
                           PrintHelp();
                           return;
                        }
                     } else {
                        if (args[i].StartsWith("-")) {
                           Console.WriteLine($"Invalid flag: \"{args[i]}\"\n");
                        } else {
                           Console.WriteLine($"File {args[i]} does not exist.");
                        }

                        return;
                     }
                     break;
               }
            }
         }

         lastInputTimer.Start();

         Cursor cursor = new Cursor();
         //ScrollBar horizontalScrollBar = new ScrollBar();

         Raylib.InitWindow(800, 600, "Notepad--");
         Raylib.SetExitKey(KeyboardKey.KEY_NULL);
         Camera2D camera = new Camera2D() {
            zoom = 1.0f,
            target = new Vector2(0, 0),
            rotation = 0.0f,
            offset = new Vector2(0, 0),
         };

         font = Raylib.LoadFontEx($"{customFontsDirectory}/Inconsolata-Medium.ttf", fontSize, 0); // Raylib.LoadFont() has rendering problems if font size is not 32. https://github.com/raysan5/raylib/issues/323

         while (!Raylib.WindowShouldClose()) {
            // Input handling
            {
               if (ShouldAcceptKeyboardInput(out string pressedKeys, out KeyboardKey specialKey)) {
                  List<KeyboardKey> modifiers = new List<KeyboardKey>();
                  if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)) modifiers.Add(KeyboardKey.KEY_LEFT_CONTROL);
                  if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT)) modifiers.Add(KeyboardKey.KEY_LEFT_SHIFT);
                  if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_ALT)) modifiers.Add(KeyboardKey.KEY_LEFT_ALT);
                  if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SUPER)) modifiers.Add(KeyboardKey.KEY_LEFT_SUPER);
                  if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)) modifiers.Add(KeyboardKey.KEY_RIGHT_CONTROL);
                  if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT)) modifiers.Add(KeyboardKey.KEY_RIGHT_SHIFT);
                  if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_ALT)) modifiers.Add(KeyboardKey.KEY_RIGHT_ALT);
                  if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SUPER)) modifiers.Add(KeyboardKey.KEY_RIGHT_SUPER);

                  // Handling key presses that have modifiers
                  {
                     if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) &&
                           Raylib.IsKeyPressed(KeyboardKey.KEY_S)) {
                        
                        WriteLinesToFile(filePath, lines);
                     }
                  }

                  // Handling key presses that don't have modifiers ie. normal key presses
                  {
                     if (pressedKeys != null) {
#if VISUAL_STUDIO
                        PrintPressedKeys(pressedKeys);
#endif
                        InsertTextAtCursor(lines, cursor, pressedKeys);
                     }
                  }

                  // Handling special keys, both with and without modifiers
                  {
                     if (specialKey != KeyboardKey.KEY_NULL) {

                        switch (specialKey) {
                           case KeyboardKey.KEY_BACKSPACE:
                              if (cursor.IsCursorAtBeginningOfFile()) break;

                              if (cursor.IsCursorAtBeginningOfLine()) {
                                 Line currentLine = lines[cursor.position.y];
                                 Line lineAbove = lines[cursor.position.y - 1];

                                 cursor.position.x = lineAbove.Value.Length;

                                 lineAbove.InsertTextAt(lineAbove.Value.Length, currentLine.Value);
                                 lines.RemoveAt(cursor.position.y);

                                 cursor.position.y--;

                                 for (int i = cursor.position.y + 1; i < lines.Count; i++) {
                                    lines[i].LineNumber--;
                                 }
                              } else {
                                 RemoveTextAtCursor(lines, cursor, 1);
                              }

                              break;
                           case KeyboardKey.KEY_ENTER: {
                                 Line currentLine = lines[cursor.position.y];
                                 string textAfterCursor = currentLine.Value.Substring(cursor.position.x);

                                 Line newLine = new Line(textAfterCursor, currentLine.LineNumber + 1);

                                 if (cursor.IsCursorAtEndOfFile(lines)) {
                                    lines.Add(newLine);
                                 } else {
                                    lines.Insert((int)currentLine.LineNumber + 1, newLine);
                                 }

                                 currentLine.RemoveTextAt(cursor.position.x, currentLine.Value.Length - cursor.position.x, Direction.Right);

                                 cursor.position.x = 0;
                                 cursor.position.y++;

                                 for (int i = cursor.position.y + 1; i < lines.Count; i++) {
                                    lines[i].LineNumber++;
                                 }
                              }
                              break;
                           case KeyboardKey.KEY_TAB:
                              InsertTextAtCursor(lines, cursor, new string(' ', tabSize));
                              break;
                           case KeyboardKey.KEY_DELETE:
                              if (cursor.IsCursorAtEndOfLine(lines)) {
                                 Line currentLine = lines[cursor.position.y];
                                 Line lineBelow = lines[cursor.position.y + 1];

                                 currentLine.InsertTextAt(currentLine.Value.Length, lineBelow.Value);

                                 lines.RemoveAt(cursor.position.y + 1);

                                 for (int i = cursor.position.y + 1; i < lines.Count; i++) {
                                    lines[i].LineNumber--;
                                 }
                              } else {
                                 RemoveTextAtCursor(lines, cursor, 1, Direction.Right);
                              }

                              break;
                           case KeyboardKey.KEY_RIGHT:
                              //camera.target.X += 10;
                              break;
                           case KeyboardKey.KEY_LEFT:
                              //camera.target.X -= 10;
                              break;
                           case KeyboardKey.KEY_UP:
                              if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL)) {
                                 spacingBetweenLines++;
                              }
                              //camera.target.Y -= 10;
                              break;
                           case KeyboardKey.KEY_DOWN:
                              if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL)) {
                                 spacingBetweenLines--;
                              }
                              //camera.target.Y += 10;
                              break;
                        }
                     }
                  }

                  cursor.HandleArrowKeysNavigation(lines, ref camera, fontSize, leftPadding, font);
               }

               //horizontalScrollBar.UpdateHorizontal(ref camera, FindDistanceToRightMostChar(lines, font), Raylib.GetScreenWidth());
            } // End of input handling

            Raylib.BeginDrawing();

            // World space rendering
            Raylib.BeginMode2D(camera);
            {
               Raylib.ClearBackground(BACKGROUND_COLOR);

               RenderLines(lines, font);
               cursor.Render(lines, fontSize, leftPadding, font);
            }
            Raylib.EndMode2D();

            // Screen space rendering ie. UI
            {
               //horizontalScrollBar.RenderHorizontal(Raylib.GetScreenWidth());
            }

            Raylib.EndDrawing();
         }

         Raylib.UnloadFont(font);
         Raylib.CloseWindow();
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
         PrintPressedKeys(keys);
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

         if (lines.Count == 0) lines.Add(new Line("", 0));

         return lines;
      }

      static void WriteLinesToFile(string path, List<Line> lines) {
         string[] linesToFile = new string[lines.Count];

         for (int i = 0; i < lines.Count; i++) {
            linesToFile[i] = lines[i].Value;
         }

         File.WriteAllLines(path, linesToFile);
         Console.WriteLine($"Saved to {path}");
      }

      static void RenderLines(List<Line> lines, Font font) {
         for (int i = 0; i < lines.Count; i++) {
            Raylib.DrawTextEx(font, lines[i].Value, new Vector2(leftPadding, i * (Line.Height + spacingBetweenLines)), fontSize, 0, TEXT_COLOR);
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
         cursor.position.x += direction == Direction.Left ? -count : 0;
      }

      static bool ShouldAcceptKeyboardInput(out string pressedChars, out KeyboardKey specialKey) {
         pressedChars = null;
         specialKey = KeyboardKey.KEY_NULL;

         timeSinceLastInput = lastInputTimer.ElapsedMilliseconds;

         if (timeSinceLastInput < inputDelay) return false;

         if ((pressedChars = GetPressedCharsAsString()) != null || IsSpecialKeyPressed(out specialKey)) {
            keyHoldTimer.Start();

            if (inputRushCounter > 0 && keyHoldTimer.ElapsedMilliseconds < waitTimeBeforeRapidInputRush) return false;

            inputRushCounter++;

            lastInputTimer.Restart();
            return true;
         }

         keyHoldTimer.Stop();
         keyHoldTimer.Reset();
         inputRushCounter = 0;
         return true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="specialKey">dummy when method returns false</param>
      /// <returns></returns>
      static bool IsSpecialKeyPressed(out KeyboardKey specialKey) {
         specialKey = KeyboardKey.KEY_NULL;

         KeyboardKey[] specialKeys = new KeyboardKey[] {
            KeyboardKey.KEY_BACKSPACE,
            KeyboardKey.KEY_ENTER,
            KeyboardKey.KEY_TAB,
            KeyboardKey.KEY_DELETE,
            KeyboardKey.KEY_RIGHT,
            KeyboardKey.KEY_LEFT,
            KeyboardKey.KEY_UP,
            KeyboardKey.KEY_DOWN
         };

         foreach (KeyboardKey key in specialKeys) {
            if (Raylib.IsKeyDown(key)) {
               specialKey = key;
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="lines"></param>
      /// <param name="font"></param>
      /// <returns>in pixels</returns>
      public static int FindDistanceToRightMostChar(in List<Line> lines, Font font) {
         Line longestLine = FindLongestLine(lines);
         return (int)Raylib.MeasureTextEx(font, longestLine.Value, fontSize, 0).X + leftPadding;
      }

      static Line FindLongestLine(in List<Line> lines) {
         Line longestLine = lines[0];
         for (int i = 1; i < lines.Count; i++) {
            if (lines[i].Value.Length > longestLine.Value.Length) {
               longestLine = lines[i];
            }
         }
         return longestLine;
      }

      static void PrintHelp() {
         Console.WriteLine("Usage: notepad-- [-h] file");
         Console.WriteLine("-h, --help: Print help and exit.");
      }

      static string GetExecutableDirectory() {
         Assembly entryAssembly = Assembly.GetEntryAssembly();
         string executableDirectory = Path.GetDirectoryName(entryAssembly.Location);

         return executableDirectory;
      }
   }
}