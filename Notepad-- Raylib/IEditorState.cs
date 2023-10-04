namespace Notepad___Raylib {
   internal interface IEditorState {
      protected void HandleInput();
      protected void PostHandleInput();
      protected void Render();
      void Update();
      protected void SetStateTo(IEditorState state);
      protected internal void EnterState();
   }
}
