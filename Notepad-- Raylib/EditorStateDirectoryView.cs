#define VISUAL_STUDIO
using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Notepad___Raylib {
   internal class EditorStateDirectoryView : IEditorState {
      List<string> directories = new List<string>();
      List<string> files;
      List<Line> lines = new List<Line>();
      Cursor cursor; // I have this cursor to use HandleArrowKeysNavigation method. I wont be rendering it.
      Camera2D camera = new Camera2D() {
         zoom = 1.0f,
         target = new Vector2(0, 0),
         rotation = 0.0f,
         offset = new Vector2(0, 0),
      };
      Stopwatch timeSinceLastMouseInput = new Stopwatch();

      public void EnterState(IEditorState _) {
         directories.Clear();
         lines.Clear();

         directories.Add("..");
         directories.AddRange(Directory.GetDirectories(Program.directoryPath).ToList());
         files = Directory.GetFiles(Program.directoryPath).ToList();

         {
            foreach (string directory in directories) {
               lines.Add(new Line(directory));
            }

            foreach (string file in files) {
               lines.Add(new Line(file));
            }
         }

         cursor = new Cursor();
      }

      public void HandleInput() {
         List<KeyboardKey> modifiers = new List<KeyboardKey>();
         if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)) modifiers.Add(KeyboardKey.KEY_LEFT_CONTROL);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT)) modifiers.Add(KeyboardKey.KEY_LEFT_SHIFT);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_ALT)) modifiers.Add(KeyboardKey.KEY_LEFT_ALT);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SUPER)) modifiers.Add(KeyboardKey.KEY_LEFT_SUPER);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)) modifiers.Add(KeyboardKey.KEY_RIGHT_CONTROL);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT)) modifiers.Add(KeyboardKey.KEY_RIGHT_SHIFT);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_ALT)) modifiers.Add(KeyboardKey.KEY_RIGHT_ALT);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SUPER)) modifiers.Add(KeyboardKey.KEY_RIGHT_SUPER);

         if (Program.ShouldAcceptKeyboardInput(out string pressedKeys, out KeyboardKey specialKey)) {
            if (specialKey != KeyboardKey.KEY_NULL) {
#if VISUAL_STUDIO
               Console.WriteLine(specialKey);
#endif
               switch (specialKey) {
                  case KeyboardKey.KEY_KP_ENTER:
                  case KeyboardKey.KEY_ENTER:
                     // todo
                     // open file if it is a file
                     // open directory if it is a directory
                     break;
                  case KeyboardKey.KEY_ESCAPE:
                     IEditorState.SetStateTo(new EditorStatePaused());
                     break;
                  case KeyboardKey.KEY_UP:
                     if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                        camera.target.Y -= Line.Height;
                     }
                     break;
                  case KeyboardKey.KEY_DOWN:
                     if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                        camera.target.Y += Line.Height;
                     }
                     break;
               }
            }

            cursor.HandleArrowKeysNavigation(lines,
                                             ref camera,
                                             Program.config.fontSize,
                                             Program.config.leftPadding,
                                             Program.font,
                                             modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL));
         }

         Program.HandleMouseWheelInput(Raylib.GetMouseWheelMove(), timeSinceLastMouseInput, modifiers, ref camera);
      }

      public void PostHandleInput() {
         Program.MakeSureCameraNotBelowZeroInBothAxes(ref camera);
         Program.ClampCameraToText(lines, ref camera);
      }

      public void Render() {
         //////////////////////////////////////
         // Screen space rendering (background)
         //////////////////////////////////////
         {
            Program.DrawBackground();
         }

         ////////////////////////
         // World space rendering
         ////////////////////////
         {
            Raylib.BeginMode2D(camera);
            {
               Program.HighlightLineCursorIsAt(cursor);
               Program.RenderLines(lines, Program.font);
            }
            Raylib.EndMode2D();
         }
      }

      public void Update() {
         HandleInput();
         PostHandleInput();
         Render();
      }
   }
}
