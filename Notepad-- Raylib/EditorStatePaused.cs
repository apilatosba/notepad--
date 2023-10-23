﻿#define VISUAL_STUDIO
using Raylib_CsLo;
using System.Diagnostics;
using System.IO;

namespace Notepad___Raylib {
   internal class EditorStatePaused : IEditorState {
      Rectangle window;
      Color windowColor = new Color(51, 51, 51, 255);
      IEditorState previousState;

      public EditorStatePaused() {
      }

      public void HandleInput() {
         if (Program.ShouldAcceptKeyboardInput(out _, out KeyboardKey specialKey)) {
            switch (specialKey) {
               case KeyboardKey.KEY_ESCAPE:
                  IEditorState.SetStateTo(previousState);
                  break;
            }
         }
      }

      public void PostHandleInput() {
         window = new Rectangle(Raylib.GetScreenWidth() / 4, 0, Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight());
      }

      public void Render() {
         previousState.Render();

         Raylib.DrawRectangleRec(window, windowColor);

         Int2 centerOfWindow = new Int2((int)(window.x + window.width / 2), (int)(window.y + window.height / 2));
         Rectangle settings = new Rectangle(centerOfWindow.x - window.width / 7, centerOfWindow.y - window.height / 4, 2 * window.width / 7, 2 * window.height / 13);
         //Raylib.DrawRectangleRec(settings, Raylib.RED);
         if (RayGui.GuiButton(settings, "Edit Settings")) {
            Process.Start(new ProcessStartInfo("notepad--", $"\"{Program.GetConfigPath()}\"") { // notepad-- only works on windows. on linux it is "dotnet notepad--.dll". problem
               UseShellExecute = true,
               CreateNoWindow = true,
#if VISUAL_STUDIO
#else
               WorkingDirectory = Program.GetExecutableDirectory()
#endif
            });
         }

         if(previousState is EditorStatePlaying) {
            Rectangle openDirectoryRect = new Rectangle(centerOfWindow.x - window.width / 7, centerOfWindow.y - window.height / 13, 2 * window.width / 7, 2 * window.height / 13);

            if(RayGui.GuiButton(openDirectoryRect, "Open Containing Folder")) {
               EditorStatePlaying.lastKnownCursorPosition = null;
               EditorStatePlaying.lastKnownCameraTarget = null;

               Program.directoryPath = Path.GetDirectoryName(Program.filePath);
               IEditorState.SetStateTo(new EditorStateDirectoryView());
            }
         }

         Rectangle quit = new Rectangle(centerOfWindow.x - window.width / 7, centerOfWindow.y + window.height / 4, 2 * window.width / 7, 2 * window.height / 13);

         if(RayGui.GuiButton(quit, "Quit")) {
            Program.isQuitButtonPressed = true;
         }
      }

      public void Update() {
         HandleInput();
         PostHandleInput();
         Render();
      }

      public void EnterState(IEditorState previousState) {
         this.previousState = previousState;
      }

      public void ExitState() {
      }
   }
}
