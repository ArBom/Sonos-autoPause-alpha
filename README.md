# Sonos-autoPause-alpha
Simple app witch pause one's Sonos player when one is watching movie, YouTube, or listening to music at PC.

It uses [System Media Transport Controls](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/integrate-with-systemmediatransportcontrols) to detect active players and changes of them.
Thanks [ByteDev](https://github.com/ByteDev) for for publishing NuGet package helpful to control one's Sonos speaker.

```
PM> NuGet\Install-Package ByteDev.Sonos
```
Best way to use it is (in my opinion) is make shortcut to autorun folder, and add params of programm working like at attached pic below.

![shortcut win](https://github.com/user-attachments/assets/9ee166c6-e635-4997-b0f2-72aa9a53325b)

The selected text at "Element docelowy"/"Target" represent params send to app as string[] args
```cs
public static void Main(string[] args)
```

| Param name:   | NoWindow              | ProvideForFocusSession         | MuteInsteadStop             | StopDelayTime           | StartDelayTime         | SonosesIPs     |
| ------------- | --------------------- | ------------------------------ | --------------------------- | ----------------------- | ---------------------- | -------------- |
| param no.     | args[0]               | args[1]                        | args[2]                     | args[3]                 | args[4]                | args[>=5]      |
| type:         | bool                  | bool                           | bool                        | int                     | int                    | IP             |
| description:  | Hide window after run | Dont play music at quiet hours | Dont stop but make it muted | Wait time (ms) to pause | Wait time (ms) to play | Your Sonos' IP |
| Example:      | true                  | true                           | false                       | 450                     | 1600                   | 192.168.0.101  |

Common target example: C:\<rest of your path>\Sonos-autoPause-alpha.exe true true false 450 1600 192.168.0.101

Presentation of working below:
[![YouTube link](https://img.youtube.com/vi/1KN3II7e4lU/0.jpg)](https://www.youtube.com/watch?v=1KN3II7e4lU)

---
License: [Creative Commons Attribution-NonCommercial-ShareAlike 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode)
