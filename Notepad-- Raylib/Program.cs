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
      public static Config config = new Config();
      public const string CONFIG_FILE_NAME = "config.xml";
      public static IEditorState editorState = new EditorStatePlaying();
      /// <summary>
      /// In milliseconds.
      /// </summary>
      static long timeSinceLastInput = 0;
      /// <summary>
      /// How many milliseconds to wait before accepting new input.
      /// </summary>
      static int inputDelay = 22;
      /// <summary>
      /// In milliseconds. This means that writing/deleting/(moving cursor) will wait after moving one character even if you are holding down the key.
      /// </summary>
      readonly static long waitTimeBeforeRapidInputRush = 400;
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
      public static string filePath;
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
         //ScrollBar horizontalScrollBar = new ScrollBar();
         config.Deserialize(GetConfigPath());

         Raylib.SetWindowState(ConfigFlags.FLAG_WINDOW_RESIZABLE | ConfigFlags.FLAG_WINDOW_TRANSPARENT);
         Raylib.SetWindowOpacity(0.1f);

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

         Raylib.InitWindow(1150, 560, "Notepad--");
         Raylib.SetExitKey(KeyboardKey.KEY_NULL);
         Camera2D camera = new Camera2D() {
            zoom = 1.0f,
            target = new Vector2(0, 0),
            rotation = 0.0f,
            offset = new Vector2(0, 0),
         };

         font = LoadFontWithAllUnicodeCharacters(GetFontFilePath(), config.fontSize);

         while (!Raylib.WindowShouldClose()) {
            Raylib.BeginDrawing();

            editorState.Update();
            
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

      public static void PrintPressedKeys(List<char> pressedKeys) {
         string keys = new string(pressedKeys.ToArray());
         PrintPressedKeys(keys);
      }

      public static void PrintPressedKeys(string pressedKeys) {
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

      public static List<Line> ReadLinesFromString(string text) {
         List<Line> lines = new List<Line>();
         string[] linesAsStringArray = text.Split('\n');

         for (int i = 0; i < linesAsStringArray.Length; i++) {
            lines.Add(new Line(linesAsStringArray[i], (uint)i));
         }

         return lines;
      }

      public static void WriteLinesToFile(string path, List<Line> lines) {
         string[] linesToFile = new string[lines.Count];

         for (int i = 0; i < lines.Count; i++) {
            linesToFile[i] = lines[i].Value;
         }

         File.WriteAllLines(path, linesToFile);
#if VISUAL_STUDIO
         Console.WriteLine($"Saved to {path}");
#endif
      }

      public static void RenderLines(List<Line> lines, Font font) {
         for (int i = 0; i < lines.Count; i++) {
            Raylib.DrawTextEx(font, lines[i].Value, new Vector2(config.leftPadding, i * (Line.Height + config.spacingBetweenLines)), config.fontSize, 0, config.textColor);
         }
      }

      public static void InsertTextAtCursor(List<Line> lines, Cursor cursor, string text) {
         Line line = lines[cursor.position.y];
         line.InsertTextAt(cursor.position.x, text);

         cursor.position.x += text.Length;
      }

      public static void RemoveTextAtCursor(List<Line> lines, Cursor cursor, int count, Direction direction = Direction.Left) {
         Line line = lines[cursor.position.y];
         line.RemoveTextAt(cursor.position.x, count, direction);
         cursor.position.x += direction == Direction.Left ? -count : 0;
      }

      public static bool ShouldAcceptKeyboardInput(out string pressedChars, out KeyboardKey specialKey) {
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
            KeyboardKey.KEY_ESCAPE,
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
         return (int)Raylib.MeasureTextEx(font, longestLine.Value, config.fontSize, 0).X + config.leftPadding;
      }

      public static Line FindLongestLine(in List<Line> lines) {
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

      public static string GetExecutableDirectory() {
         Assembly entryAssembly = Assembly.GetEntryAssembly();
         string executableDirectory = Path.GetDirectoryName(entryAssembly.Location);

         return executableDirectory;
      }

      public static void MakeSureCameraNotBelowZeroInBothAxes(ref Camera2D camera) {
         if (camera.target.X < 0) camera.target.X = 0;
         if (camera.target.Y < 0) camera.target.Y = 0;
      }

      public static Font LoadFontWithAllUnicodeCharacters(string path, int fontSize) {
         // Font loading. Loading all characters in the Unicode range.
         unsafe {
            int startCodePoint = 0x0000;
            int endCodePoint = 0xFFFF;

            int glyphCount = endCodePoint - startCodePoint + 1;
            int* fontChars = stackalloc int[glyphCount];

            for (int i = 0; i < glyphCount; i++) {
               fontChars[i] = startCodePoint + i;
            }

            return Raylib.LoadFontEx(path, fontSize, fontChars, glyphCount); // Raylib.LoadFont() has rendering problems if font size is not 32. https://github.com/raysan5/raylib/issues/323
         }
      }

      public static void InsertLinesAtCursor(List<Line> lines, Cursor cursor, in List<Line> linesToInsert) {
         Line line = lines[cursor.position.y];
         string textAfterCursorFirstLine = line.Value.Substring(cursor.position.x);
         line.RemoveTextAt(cursor.position.x, line.Value.Length - cursor.position.x, Direction.Right);

         line.InsertTextAt(cursor.position.x, linesToInsert[0].Value);

         for (int i = 1; i < linesToInsert.Count; i++) {
            linesToInsert[i].LineNumber = (uint)(cursor.position.y + i);
            lines.Insert(cursor.position.y + i, linesToInsert[i]);
         }

         Line lastInsertedLine = lines[cursor.position.y + linesToInsert.Count - 1];
         lastInsertedLine.InsertTextAt(lastInsertedLine.Value.Length, textAfterCursorFirstLine);

         cursor.position.y += linesToInsert.Count - 1;
         cursor.position.x = lastInsertedLine.Value.Length - textAfterCursorFirstLine.Length;
      }

      public static string GetConfigPath() {
#if VISUAL_STUDIO
         return CONFIG_FILE_NAME;
#else
         return Path.Combine(GetExecutableDirectory(), CONFIG_FILE_NAME);
#endif
      }

      public static string GetFontFilePath() {
#if VISUAL_STUDIO
         return Path.Combine(customFontsDirectory, config.fontName.EndsWith(".ttf") ? config.fontName : config.fontName + ".ttf");
#else
         return Path.Combine(GetExecutableDirectory(), "Fonts", config.font.EndsWith(".ttf") ? config.font : config.font + ".ttf");
#endif
      }
   }
}