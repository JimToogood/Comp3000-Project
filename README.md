## 4-Player Mahjong Simulator, by Jim Toogood.  

This is the repository my Comp3000 Dissertation Project "4-Player Mahjong Simulator" built in Unity 6000.2.13f1. This project is a 3D simulation of the 4-player strategy game Mahjong, using a variation of the classical Chinese ruleset, and featuring high-quality visuals, a fully functional tile and rule management system, and accurate gameplay mechanics.  

---

## Assets  
All textures, models and sounds used in this project are copyright-free and licensed for free use. All rights remain with their respective creators as listed below. This project does not claim ownership of any of the third-party assets cited.  

**Visual Assets Used:**  
Tile textures by FluffyStuff (edited by me) - https://github.com/FluffyStuff/riichi-mahjong-tiles  
Room model "The Billiards Room" by The Hallwyl Museum (texture and model edited by me) - https://sketchfab.com/3d-models/the-billiards-room-79615d823a9149069dcd06c20bc9707f  
Table model "Victorian Wooden Table" by Enzo Amanrich (texture edited by me) - https://sketchfab.com/3d-models/victorian-wooden-table-1fe13399313e483ca04e34e56ba1c1c7  

**Sound Assets Used:**  
click.ogg - https://pixabay.com/sound-effects/film-special-effects-click-sound-432501/  
Tchaikovsky Op 37a The Seasons December - https://www.classicals.de/tchaikovsky-seasons  
Beethoven Op 13 No 8 Pathetique - [IMSLP, performed by Gabriel Antonio Hernandez Romero](https://imslp.org/wiki/Piano_Sonata_No.8,_Op.13_(Beethoven,_Ludwig_van))  
Robert Schumann Op 15 No 7 Kinderszenen - https://imslp.org/wiki/Kinderszenen%2C_Op.15_(Schumann%2C_Robert)  

---

## How to run game  
1) Go to [Releases](https://github.com/JimToogood/Comp3000-Project/releases) and select the **latest** release  
2) Download `MacOS_Release.zip` for the Mac version, or `Windows_Release.zip` for the Windows version  

**MacOS Instructions**  
3) Double click the downloaded .zip file to unzip it into an application  
4) Open the `Mahjong Simulator` application, and that's it!  

**Windows Instructions**  
3) Extract all the downloaded .zip file to unzip it into a folder  
4) Ensure following file structure exists inside the folder:  
```text
MahjongSimulator/  
 ├── Mahjong_Simulator.exe  
 ├── UnityCrashHandler64.exe  
 ├── UnityPlayer.dll  
 ├── Mahjong_Simulator_Data/  
 ├── MonoBleedingEdge/  
 └── D3D12/  
```
5) Open `Mahjong_Simulator.exe` and that's it!  

**PLEASE NOTE:** The Windows version has only been tested on Windows 11. Theoretically, it should work on Windows 10, but this is untested and as such cannot be guaranteed.  
**PLEASE NOTE:** The MacOS version has only been tested on MacOS Sonoma. Theoretically, it should work on other modern versions of MacOS such as Sequoia or Tahoe, but this is untested and as such cannot be guaranteed.  

---

## How to open the project in Unity  
1) Go to [Releases](https://github.com/JimToogood/Comp3000-Project/releases) and select the **latest** release  
2) Download `Unity_Project.zip`  
3) Extract all the downloaded .zip file to unzip it into a folder  
4) Ensure following file structure exists inside the folder:  
```text
Mahjong_Simulator/  
 ├── Assets/  
 ├── Packages/  
 └── ProjectSettings/  
```
5) Open Unity Hub, click `Add` -> `Add project from disk`, select the `Mahjong_Simulator` folder, open the project  
6) Once the project has opened, go to `Assets` -> `Scenes` then open `MainScene (Scene Asset)` and that's it!  

**PLEASE NOTE:** The Unity project was created and developed using Unity 6000.2.13f1, and is the only version of Unity I recommend attempting to open the project with.  