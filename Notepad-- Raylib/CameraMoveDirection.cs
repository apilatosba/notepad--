namespace Notepad___Raylib {
   internal enum CameraMoveDirection {
      NoMove = 0b00,
      Down = 0b01,
      Right = 0b10,
      Both = Down | Right /* 0b11 */
   }
}
