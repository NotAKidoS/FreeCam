# FreeCam
Free cam script for the Game view tab. Modified version of Unity FreeCam by ashleydavis for use with the VRChat Creator Companion.

![Unity_J0wEo26MCX](https://user-images.githubusercontent.com/37721153/180728216-92bde4c8-1217-4fe0-b59f-523b315cd1b9.png)

# Info:
## FreeCam Configuration
Handles movement speeds, with Shift being the fast multiplier.

## PlayMode Persistance
- Start From SceneView
> Moves FreeCam to SceneView camera position on entering playmode.
- Persist From Playmode
> Last FreeCam position in PlayMode will transfer to EditMode.

## Avatar Dynamics
- Basic Contact Testing
> Activate contact receivers via mouseclick like PhysBones. Only on/off with no capsule (height) or proximity support.
- Autofix Physbone Helper
> Temporarily disables all other cameras while regenerating PhysBoneGrabHelper to guarantee FreeCam takes priority.
https://feedback.vrchat.com/avatar-dynamics-reports-and-feedback/p/bug-multiple-active-cameras-obstruct-grabbing-on-unity-debugging 

## FreeCam Controls
- General Movement

> W | UpArrow

> A | LeftArrow

> S | DownArrow

> D | RightArrow


- Move Camera Up/Down Local
> E | Q

- Move Camera Up/Down World 
> R | F

- Fast Multiplier
> LShift | RShift

# How To Use:
**Extract ZIP into folder, add to VRChat Creator Companion as a User Package.**

If you are not using VCC, just extract the ZIP into your project. Shouldn't harm anything.

**Find "NotAKid/FreeCam" from the top toolbar, click Add FreeCam to scene.**

Original:
https://gist.github.com/ashleydavis/f025c03a9221bc840a2b
