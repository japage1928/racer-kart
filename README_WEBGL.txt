WebGL Build + Play

1) Open project in Unity 2022.3 LTS.
2) Create and save an empty scene at Assets/Scenes/Racer_Main.unity.
3) Open File > Build Settings and add Racer_Main to Scenes In Build.
4) Switch platform to WebGL.
5) Build to a folder, for example Builds/WebGL.
6) Serve the build folder from a local web server (do not open index.html with file://).
   Example: python -m http.server 8000 --directory Builds/WebGL
7) Open http://localhost:8000 in a browser.

Controls
- Keyboard: W/S accelerate/brake, A/D steer, Left Shift drift, Space handbrake.
- Browser on-screen buttons: auto-shown in WebGL/mobile builds.

Notes
- The game scene is assembled automatically at runtime by RacerRuntimeBootstrap.
- Return button emits OnMiniGameFinished payload and restarts the race.
