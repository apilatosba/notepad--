//#define VISUAL_STUDIO
using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;

namespace Notepad___Raylib {
   internal class Program {
      public static WindowSaveData windowSaveData = new WindowSaveData();
      public static Config config = new Config();
      public const string CONFIG_FILE_NAME = "config.xml";
      public static IEditorState editorState;
      /// <summary>
      /// In milliseconds.
      /// </summary>
      static long timeSinceLastInput = 0;
      /// <summary>
      /// How many milliseconds to wait before accepting new input.
      /// </summary>
      static int inputDelay = 22;
      static char lastPressedKey;
      static KeyboardKey lastPressedSpecialKey;
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
      public static Shader flashShader;
      public static Shader bloomShader;
      public static Shader twoPassGaussianBlur;
      public static Shader rainbowShader;
      public static int flashShaderTransparencyLoc;
      public static int bloomShaderTextMaskLoc;
      public static int bloomShaderResolutionLoc;
      public static int bloomShaderStrengthLoc;
      public static int twoPassGaussianBlurShaderTextMaskLoc;
      public static int twoPassGaussianBlurShaderHorizontalLoc;
      public static int rainbowShaderTimeLoc;
      public static int rainbowShaderHighlightedLineMaskLoc;
      public static Image windowCoverImage;
      public static Texture windowCoverTexture; // Rectangle doesnt work with uv's. https://github.com/raysan5/raylib/issues/1730
      public static Texture background;
      public static Texture quarterResolutionTextMask;
      public static RenderTexture textMask;
      public static Vector2 backgroundPosition;
      public static float backgroundScale;
      public static string filePath;
      public static string directoryPath;
      public static bool isQuitButtonPressed = false;
      public static bool isDraggingWindow = false;
      public static UndoHistory<List<UndoItem>> undoHistory = new UndoHistory<List<UndoItem>>(512);
      /// <summary>
      /// Vertical form of <see cref="Config.leftPadding"/>
      /// </summary>
      public static int YMargin;
      static Int2 mousePositionLastFrame;
      static Int2 mousePositionWhenStartedDragging;
      static Int2 windowPositionWhenStartedDragging;
      static Int2 mousePositionOffsetRelativeToWindow;
      static Int2 windowSizeWhenStartedDragging;
      public readonly static Int2 minimumWindowSize = new Int2(100, 50);

#if VISUAL_STUDIO
      static string customFontsDirectory;
      static string appDirectory;
#else
      static readonly string customFontsDirectory = Path.Combine(GetExecutableDirectory(), "Fonts");
#endif

      /// <summary>
      /// This doesn't include normal key presses that have modifiers.
      /// </summary>
      public static long TimeSinceLastKeyboardInput => timeSinceLastInput;

      static unsafe void Main(string[] args) {
#if VISUAL_STUDIO
         appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
         appDirectory = Directory.GetParent(appDirectory).Parent.Parent.ToString();

         customFontsDirectory = Path.Combine(appDirectory, "Fonts");
#else
         Raylib.SetTraceLogLevel((int)TraceLogLevel.LOG_FATAL);

         string configPath = Path.Combine(GetExecutableDirectory(), CONFIG_FILE_NAME);

         if (!File.Exists(configPath)) {
            File.Create(configPath).Close();
            new Config().Serialize(configPath);
         }

         if (!File.Exists(GetWindowSaveDataPath())) {
            File.Create(GetWindowSaveDataPath()).Close();
            new WindowSaveData().Serialize(GetWindowSaveDataPath());
         }
#endif

         //ScrollBar horizontalScrollBar = new ScrollBar();

         try {
            config.Deserialize(GetConfigPath());
         }
         catch (InvalidOperationException) {
            Console.WriteLine("Your config file was corrupt. Reverting it to previous settings now.");
            config.Serialize(GetConfigPath());
         }

         try {
            windowSaveData = WindowSaveData.Deserialize(GetWindowSaveDataPath());
         }
         catch (InvalidOperationException) {
            windowSaveData.Serialize(GetWindowSaveDataPath());
         }

         // Command line arguments
         {
            if (args.Length == 0) {
               //Console.WriteLine("ERROR: No file specified\n");
               //PrintHelp();
               //return;
               directoryPath = Directory.GetCurrentDirectory();
            }

            for (int i = 0; i < args.Length; i++) {
               switch (args[i]) {
                  case "-h":
                  case "--help":
                     PrintHelp();
                     return;
                  default:
                     if (File.Exists(args[i])) {
                        if (filePath == null && directoryPath == null) {
                           filePath = args[i];
                        } else {
                           Console.WriteLine("ERROR: Specify only one file or directory\n");
                           PrintHelp();
                           return;
                        }
                     } else if (Directory.Exists(args[i])) {
                        if (filePath == null && directoryPath == null) {
                           directoryPath = args[i];
                        } else {
                           Console.WriteLine("ERROR: Specify only one directory or file\n");
                           PrintHelp();
                           return;
                        }
                     } else {
                        if (args[i].StartsWith("-")) {
                           Console.WriteLine($"Invalid flag: \"{args[i]}\"\n");
                           return;
                        } else {
                           //Console.WriteLine($"File {args[i]} does not exist.");
                           File.Create(args[i]).Close();
                           filePath = args[i];
                        }
                     }
                     break;
               }
            }
         }

         Debug.Assert(!(filePath != null && directoryPath != null));

         Raylib.InitWindow(windowSaveData.size.x, windowSaveData.size.y, "Notepad--");

         font = LoadFontWithAllUnicodeCharacters(GetFontFilePath(), config.fontSize);

         if (directoryPath != null) {
            IEditorState.SetStateTo(new EditorStateDirectoryView());
         } else if (filePath != null) {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(),
                                                                             filePath)));
            filePath = Path.GetFileName(filePath);

            lines = ReadLinesFromFile(filePath);
            IEditorState.SetStateTo(new EditorStatePlaying());
         }

         lastInputTimer.Start();

         Raylib.SetWindowIcon(Raylib.LoadImage(Path.Combine(GetImagesDirectory(), "icon4.png")));
         Raylib.SetWindowPosition(windowSaveData.position.x, windowSaveData.position.y);
         Raylib.SetWindowState(/*(windowSaveData.maximized ? ConfigFlags.FLAG_WINDOW_MAXIMIZED : 0)*/
                               /*|*/ ConfigFlags.FLAG_WINDOW_UNDECORATED
                               | ConfigFlags.FLAG_WINDOW_RESIZABLE
                               | ConfigFlags.FLAG_WINDOW_TRANSPARENT
                               | ConfigFlags.FLAG_VSYNC_HINT);

         flashShader = Raylib.LoadShader(null, Path.Combine(GetShadersDirectory(), "flash.frag"));
         flashShaderTransparencyLoc = Raylib.GetShaderLocation(flashShader, "transparency");
         windowCoverImage = Raylib.GenImageColor(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(255, 255, 255, 255));
         windowCoverTexture = Raylib.LoadTextureFromImage(windowCoverImage);

         bloomShader = Raylib.LoadShader(null, Path.Combine(GetShadersDirectory(), "bloom.frag"));
         bloomShaderTextMaskLoc = Raylib.GetShaderLocation(bloomShader, "textMask");
         bloomShaderResolutionLoc = Raylib.GetShaderLocation(bloomShader, "resolution");
         bloomShaderStrengthLoc = Raylib.GetShaderLocation(bloomShader, "strength");

         twoPassGaussianBlur = Raylib.LoadShader(null, Path.Combine(GetShadersDirectory(), "two pass gaussian blur.frag"));
         twoPassGaussianBlurShaderTextMaskLoc = Raylib.GetShaderLocation(twoPassGaussianBlur, "textMask");
         twoPassGaussianBlurShaderHorizontalLoc = Raylib.GetShaderLocation(twoPassGaussianBlur, "horizontal");

         rainbowShader = Raylib.LoadShader(null, Path.Combine(GetShadersDirectory(), "rainbow.frag"));
         rainbowShaderTimeLoc = Raylib.GetShaderLocation(rainbowShader, "time");
         rainbowShaderHighlightedLineMaskLoc = Raylib.GetShaderLocation(rainbowShader, "highlightedLineMask");

         background = Raylib.LoadTexture(Path.Combine(GetImagesDirectory(), config.backgroundImage));

         {
            int w = background.width;
            int h = background.height;
            int sw = Raylib.GetScreenWidth();
            int sh = Raylib.GetScreenHeight();

            if (h * ((float)sw / w) >= sh) {
               backgroundScale = (float)sw / w;
               backgroundPosition = new Vector2(0, -((h * backgroundScale - sh) / 2));
            } else {
               backgroundScale = (float)sh / h;
               backgroundPosition = new Vector2(-((w * backgroundScale - sw) / 2), 0);
            }
         }

         textMask = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
         Raylib.SetTextureWrap(textMask.texture, TextureWrap.TEXTURE_WRAP_CLAMP);

         Raylib.SetExitKey(KeyboardKey.KEY_NULL);
         Camera2D camera = new Camera2D() {
            zoom = 1.0f,
            target = new Vector2(0, 0),
            rotation = 0.0f,
            offset = new Vector2(0, 0),
         };

         while (!Raylib.WindowShouldClose() && !isQuitButtonPressed) {
            Raylib.BeginDrawing();

            // Window resizing and repositioning. Needs to be called before editorState.Update() because otherwise IsWindowResized() will never get triggered.
            {
               if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)) {
                  Int2 mousePosition = Program.GetMousePositionInScreenSpace();

                  if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) {
                     mousePositionWhenStartedDragging = Program.GetMousePositionInScreenSpace();
                     windowPositionWhenStartedDragging = (Int2)Raylib.GetWindowPosition();

                     mousePositionOffsetRelativeToWindow = windowPositionWhenStartedDragging - mousePositionWhenStartedDragging;

                     isDraggingWindow = true;
                  }

                  if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) {
                     Raylib.SetWindowPosition(mousePosition.x + mousePositionOffsetRelativeToWindow.x,
                                              mousePosition.y + mousePositionOffsetRelativeToWindow.y);
                  }

                  if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_RIGHT)) {
                     windowSizeWhenStartedDragging = new Int2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

                     mousePositionWhenStartedDragging = Program.GetMousePositionInScreenSpace();
                     windowPositionWhenStartedDragging = (Int2)Raylib.GetWindowPosition();

                     mousePositionOffsetRelativeToWindow = windowPositionWhenStartedDragging - mousePositionWhenStartedDragging;

                     isDraggingWindow = true;
                  }

                  if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT)) {
                     int width = windowSizeWhenStartedDragging.x + mousePosition.x - mousePositionWhenStartedDragging.x;
                     int height = windowSizeWhenStartedDragging.y + mousePosition.y - mousePositionWhenStartedDragging.y;

                     Raylib.SetWindowSize(width < minimumWindowSize.x ? minimumWindowSize.x : width,
                                          height < minimumWindowSize.y ? minimumWindowSize.y : height);
                  }
               }

               if (Raylib.IsKeyUp(KeyboardKey.KEY_LEFT_CONTROL)) {
                  isDraggingWindow = false;
               } else {
                  if (Raylib.IsMouseButtonUp(MouseButton.MOUSE_BUTTON_LEFT) && Raylib.IsMouseButtonUp(MouseButton.MOUSE_BUTTON_RIGHT)) {
                     isDraggingWindow = false;
                  }
               }

               if (Raylib.IsKeyPressed(KeyboardKey.KEY_F11)) {
                  if (Raylib.IsWindowMaximized()) {
                     Raylib.RestoreWindow();
                  } else {
                     Raylib.MaximizeWindow();
                  }
               }
            }

            editorState.Update();

            mousePositionLastFrame = GetMousePositionInScreenSpace();

            Raylib.EndDrawing();
         }

         windowSaveData.size = new Int2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
         windowSaveData.position = (Int2)Raylib.GetWindowPosition();
         windowSaveData.maximized = Raylib.IsWindowState(ConfigFlags.FLAG_WINDOW_MAXIMIZED);
         windowSaveData.Serialize(GetWindowSaveDataPath());

         Raylib.UnloadImage(windowCoverImage);
         Raylib.UnloadTexture(windowCoverTexture);
         Raylib.UnloadTexture(background);
         Raylib.UnloadRenderTexture(textMask);
         Raylib.UnloadShader(flashShader);
         Raylib.UnloadShader(bloomShader);
         Raylib.UnloadShader(twoPassGaussianBlur);
         Raylib.UnloadShader(rainbowShader);
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

      public static List<Line> ReadLinesFromFile(string path) {
         List<Line> lines = new List<Line>();
         string[] linesFromFile = File.ReadAllLines(path);

         for (int i = 0; i < linesFromFile.Length; i++) {
            lines.Add(new Line(linesFromFile[i]));
         }

         if (lines.Count == 0) lines.Add(new Line(""));

         return lines;
      }

      public static List<Line> ReadLinesFromString(string text) {
         List<Line> lines = new List<Line>();
         string[] linesAsStringArray = text.Split('\n');

         for (int i = 0; i < linesAsStringArray.Length; i++) {
            lines.Add(new Line(linesAsStringArray[i]));
         }

         return lines;
      }

      public static void WriteLinesToFile(string path, List<Line> lines) {
         string[] linesToFile = new string[lines.Count];

         for (int i = 0; i < lines.Count; i++) {
            linesToFile[i] = lines[i].Value;
         }

         for(int i = 0; ;) {
            try {
               File.WriteAllLines(path, linesToFile);
               break;
            }
            catch (IOException) {
               i++;

               if(i > 5) {
                  Console.WriteLine("Failed to save file.");
                  return;
               }
            }
         }
#if VISUAL_STUDIO
         Console.WriteLine($"Saved to {path}");
#endif
      }

      [Obsolete("Use the other overload that takes Camera2D as an argument")]
      public static void RenderLines(in List<Line> lines, in Font font) {
         for (int i = 0; i < lines.Count; i++) {
            Raylib.DrawTextEx(font, lines[i].Value, new Vector2(config.leftPadding, i * (Line.Height + config.spacingBetweenLines)), config.fontSize, 0, config.textColor);
         }
      }

      [Obsolete("Use the other overload that takes Camera2D as an argument")]
      public static void RenderLines(in List<Line> lines, in Font font, in Color color) {
         for (int i = 0; i < lines.Count; i++) {
            Raylib.DrawTextEx(font, lines[i].Value, new Vector2(config.leftPadding, i * (Line.Height + config.spacingBetweenLines)), config.fontSize, 0, color);
         }
      }

      public static void RenderLines(in List<Line> lines, in Font font, in Color color, int yOffset, in Camera2D camera) {
         for (int i = 0; i < lines.Count; i++) {
            if (IsLineIsInBoundsOfCamera(i, camera, yOffset)) {
               Raylib.DrawTextEx(font, lines[i].Value, new Vector2(config.leftPadding, i * (Line.Height + config.spacingBetweenLines) + yOffset), config.fontSize, 0, color);
            }
         }
      }

      public static void InsertTextAtCursor(List<Line> lines, Cursor cursor, string text) {
         Line line = lines[cursor.position.y];
         line.InsertTextAt(cursor.position.x, text, cursor);

         cursor.position.x += text.Length;
      }

      public static void RemoveTextAtCursor(List<Line> lines, Cursor cursor, int count, Direction direction = Direction.Left) {
         Line line = lines[cursor.position.y];
         line.RemoveTextAt(cursor.position.x, count, cursor, direction);
         //cursor.position.x += direction == Direction.Left ? -count : 0; // Use this when you use the obsolete RemoveTextAt() method.
      }

      public static bool ShouldAcceptKeyboardInput(out string pressedChars, out KeyboardKey specialKey) {
         specialKey = KeyboardKey.KEY_NULL;

         timeSinceLastInput = lastInputTimer.ElapsedMilliseconds;

         if ((pressedChars = GetPressedCharsAsString()) != null || IsSpecialKeyPressed(out specialKey)) {
            if (specialKey != lastPressedSpecialKey) {
               lastInputTimer.Restart();
               lastPressedSpecialKey = specialKey;
               inputRushCounter++;
               keyHoldTimer.Stop();
               keyHoldTimer.Reset();
               return true;
            }

            if (timeSinceLastInput < inputDelay) {
               if (pressedChars?[^1] == lastPressedKey || specialKey == lastPressedSpecialKey) {
                  return false;
               }
            }

            lastPressedKey = pressedChars?[^1] ?? lastPressedKey;
            lastPressedSpecialKey = specialKey == KeyboardKey.KEY_NULL ? lastPressedSpecialKey : specialKey;

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
            KeyboardKey.KEY_HOME,
            KeyboardKey.KEY_END,
            KeyboardKey.KEY_ESCAPE,
            KeyboardKey.KEY_DELETE,
            KeyboardKey.KEY_TAB,
            KeyboardKey.KEY_ENTER,
            KeyboardKey.KEY_BACKSPACE,
            KeyboardKey.KEY_UP,
            KeyboardKey.KEY_DOWN,
            KeyboardKey.KEY_RIGHT,
            KeyboardKey.KEY_LEFT,
         };

         foreach (KeyboardKey key in specialKeys) {
            if (Raylib.IsKeyDown(key)) {
               specialKey = key;
               return true;
            }
         }

         return false;
      }

      static bool IsSpecialKeyPressed(out List<KeyboardKey> pressedSpecialKeys) {
         pressedSpecialKeys = new List<KeyboardKey>();
         bool isKeyPressed = false;

         KeyboardKey[] specialKeys = new KeyboardKey[] {
            KeyboardKey.KEY_HOME,
            KeyboardKey.KEY_END,
            KeyboardKey.KEY_ESCAPE,
            KeyboardKey.KEY_DELETE,
            KeyboardKey.KEY_TAB,
            KeyboardKey.KEY_ENTER,
            KeyboardKey.KEY_BACKSPACE,
            KeyboardKey.KEY_UP,
            KeyboardKey.KEY_DOWN,
            KeyboardKey.KEY_RIGHT,
            KeyboardKey.KEY_LEFT,
         };

         foreach(var key in specialKeys) {
            if (Raylib.IsKeyDown(key)) {
               pressedSpecialKeys.Add(key);
               isKeyPressed = true;
            }
         }

         return isKeyPressed;
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

            Font font = Raylib.LoadFontEx(path, fontSize, fontChars, glyphCount); // Raylib.LoadFont() has rendering problems if font size is not 32. https://github.com/raysan5/raylib/issues/323
            Raylib.SetTextureFilter(font.texture, TextureFilter.TEXTURE_FILTER_POINT);

            return font;
         }
      }

      public static void InsertLinesAtCursor(List<Line> lines, Cursor cursor, in List<Line> linesToInsert) {
         Line line = lines[cursor.position.y];
         string textAfterCursorFirstLine = line.Value.Substring(cursor.position.x);

         List<UndoItem> undoItems = new List<UndoItem>();

         undoItems.Add(new UndoItem(new Line(line), cursor.position.y, cursor.position, UndoAction.Replace));

         line.RemoveTextAt(cursor.position.x, line.Value.Length - cursor.position.x, Direction.Right);

         line.InsertTextAt(cursor.position.x, linesToInsert[0].Value);

         for (int i = 1; i < linesToInsert.Count; i++) {
            undoItems.Add(new UndoItem(null, cursor.position.y + 1, cursor.position, UndoAction.Remove));

            lines.Insert(cursor.position.y + i, linesToInsert[i]);
         }

         undoHistory.Push(undoItems);

         Line lastInsertedLine = lines[cursor.position.y + linesToInsert.Count - 1];
         lastInsertedLine.InsertTextAt(lastInsertedLine.Value.Length, textAfterCursorFirstLine);

         cursor.position.y += linesToInsert.Count - 1;
         cursor.position.x = lastInsertedLine.Value.Length - textAfterCursorFirstLine.Length;
      }

      public static void HandleMouseWheelInput(float mouseWheelInput, Stopwatch timeSinceLastMouseInput, List<KeyboardKey> modifiers, ref Camera2D camera, IEditorState currentState) {
         if (mouseWheelInput != 0) {
            timeSinceLastMouseInput?.Restart();

            if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
               //camera.zoom += mouseWheelInput * 0.04f;
               if (mouseWheelInput > 0) {
                  Program.config.fontSize++;
                  Program.config.Serialize(Program.GetConfigPath());

                  Raylib.UnloadFont(Program.font);
                  Program.font = Program.LoadFontWithAllUnicodeCharacters(Program.GetFontFilePath(), Program.config.fontSize);

                  if (currentState is EditorStateDirectoryView)
                     YMargin = Line.Height;
               } else if (mouseWheelInput < 0) {
                  Program.config.fontSize--;
                  Program.config.Serialize(Program.GetConfigPath());

                  Raylib.UnloadFont(Program.font);
                  Program.font = Program.LoadFontWithAllUnicodeCharacters(Program.GetFontFilePath(), Program.config.fontSize);

                  if (currentState is EditorStateDirectoryView)
                     YMargin = Line.Height;
               }
            } else {
               camera.target.Y -= mouseWheelInput * Line.Height;
            }
         }
      }

      public static void HighlightLineCursorIsAt(Cursor cursor) {
         Raylib.BeginBlendMode(BlendMode.BLEND_ADDITIVE);
         Rectangle highlightLineRect = new Rectangle(0, Line.Height * cursor.position.y + YMargin, float.MaxValue, Line.Height);
         Raylib.DrawRectangleRec(highlightLineRect, new Color(13, 13, 13, 255));
         Raylib.EndBlendMode();
      }

      public static void DrawBackground() {
         if (Raylib.IsWindowResized()) {
            int w = Program.background.width;
            int h = Program.background.height;
            int sw = Raylib.GetScreenWidth();
            int sh = Raylib.GetScreenHeight();

            if (h * ((float)sw / w) >= sh) {
               Program.backgroundScale = (float)sw / w;
               Program.backgroundPosition = new Vector2(0, -((h * Program.backgroundScale - sh) / 2));
            } else {
               Program.backgroundScale = (float)sh / h;
               Program.backgroundPosition = new Vector2(-((w * Program.backgroundScale - sw) / 2), 0);
            }
         }

         int lucidity = (int)(Math.Clamp(Program.config.backgroundLucidity, 0, 1) * 255);
         Raylib.DrawTextureEx(Program.background, Program.backgroundPosition, 0, Program.backgroundScale, new Color(lucidity, lucidity, lucidity, 255));
      }

      public static void ClampCameraToText(in List<Line> lines, ref Camera2D camera) {
         int heightOfAllLines = lines.Count * Line.Height;
         int cameraThreshold = heightOfAllLines - Line.Height;

         if (camera.target.Y > cameraThreshold) {
            camera.target.Y = cameraThreshold;
         }
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="lineIndex"></param>
      /// <param name="camera"></param>
      /// <param name="linesYOffset"><see cref="Program.YMargin"/> is included even though it is global</param>
      /// <returns></returns>
      static bool IsLineIsInBoundsOfCamera(int lineIndex, in Camera2D camera, int linesYOffset) {
         int lineTopEdgeWorldSpacePositionY = lineIndex * Line.Height + linesYOffset;
         int lineBottomEdgeWorldSpacePositionY = lineTopEdgeWorldSpacePositionY + Line.Height;

         int cameraTopEdgeWorldSpacePositionY = (int)camera.target.Y;
         int cameraBottomEdgeWorldSpacePositionY = (int)camera.target.Y + Raylib.GetScreenHeight();

         if (lineBottomEdgeWorldSpacePositionY < cameraTopEdgeWorldSpacePositionY ||
               lineTopEdgeWorldSpacePositionY > cameraBottomEdgeWorldSpacePositionY)
            return false;
         else return true;
      }

      public static Int2 GetMousePositionInScreenSpace() {
         PublicUtility.MouseRun.Mouse.GetPosition().Deconstruct(out int x, out int y);
         return new Int2(x, y);
      }

      public static Int2 GetMouseDelta() {
         return GetMousePositionInScreenSpace() - mousePositionLastFrame;
      }

      public static string GetConfigPath() {
#if VISUAL_STUDIO
         return Path.Combine(appDirectory, CONFIG_FILE_NAME);
#else
         return Path.Combine(GetExecutableDirectory(), CONFIG_FILE_NAME);
#endif
      }

      public static string GetFontFilePath() {
#if VISUAL_STUDIO
         return Path.Combine(customFontsDirectory, config.fontName.EndsWith(".ttf") ? config.fontName : config.fontName + ".ttf");
#else
         return Path.Combine(GetExecutableDirectory(), "Fonts", config.fontName.EndsWith(".ttf") ? config.fontName : config.fontName + ".ttf");
#endif
      }

      public static string GetShadersDirectory() {
#if VISUAL_STUDIO
         return Path.Combine(appDirectory, "Shaders");
#else
         return Path.Combine(GetExecutableDirectory(), "Shaders");
#endif
      }

      public static string GetImagesDirectory() {
#if VISUAL_STUDIO
         return Path.Combine(appDirectory, "Images");
#else
         return Path.Combine(GetExecutableDirectory(), "Images");
#endif
      }

      public static string GetWindowSaveDataPath() {
#if VISUAL_STUDIO
         return Path.Combine(appDirectory, "window.xml");
#else
         return Path.Combine(GetExecutableDirectory(), "window.xml");
#endif
      }
   }
}