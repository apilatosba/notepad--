namespace Notepad___Raylib {
   internal interface IEditorState {
      void HandleInput();
      void PostHandleInput();
      void Render();
      void Update();
   }
}
