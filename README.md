# Notepad--
It is gonna be like microsoft notepad but with shaders and stuff.  

## How to Use
```cmd
notepad-- file
```

## How to Build/Download 
Get binaries from [release section](https://github.com/apilatosba/notepad--/releases).  
Put the contents of the zip file in a folder that is in your PATH environment variable.  
  
Or  
  
Build it from source.  
Go to [Program.cs](https://github.com/apilatosba/notepad--/blob/main/Notepad--%20Raylib/Program.cs) and comment out the line(probably first line) that says <mark>#define VISUAL_STUDIO</mark> 
```cmd
dotnet publish -c Release
```
Put the contents of the publish folder (bin/Release/net7.0/publish) in a folder that is in your PATH environment variable.  
Also you need to add [Fonts folder](https://github.com/apilatosba/notepad--/tree/main/Notepad--%20Raylib/Fonts) to the folder that you put the executable in.  

## References
Raylib: https://www.raylib.com/ - Great library, big thumbs up.  
Bloom: https://learnopengl.com/Advanced-Lighting/Bloom  
Rgb to hsv conversion: https://stackoverflow.com/questions/15095909/from-rgb-to-hsv-in-opengl-glsl
