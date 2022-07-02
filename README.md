# Unity Eventer

Allows you to abstract from managing event's subscriptions by introducing few attributes for easier management.

By default, you add event's handlers in `Start` and remove them in `OnDestroy`. This is fine, but when there are lots and lots of handlers each of which 
should probably launch after another (so with correct order) it results in a hell mess with scripts execution order and many others annoying things.

`Eventer` brings two attributes - `[Subscribable]` and `[Subscriber]`. You put those on MonoBehaviours events and event handlers respectively and no longer need 
to manually manage event handlers lifecyle as for now Reflection will do it for you.

In order for this to work make sure you created an empty gameobject in your scene and added `Eventer` component to it. That object will be `DontDestroyOnLoad` and obviously has to be placed at the very first game scene

Here's quick examples:

Declaring an event:
```c#
[Subscribable("MyFirstEvent", DestroyOnLoad = false)]
public event Action<string> OnRequestOk;
```

A string `"MyFirstEvent"` will be then used by subscribers to listen for this specific event.
`DestroyOnLoad = false` means that after a new scene is loaded (in a single mode) this event wont be destroyed. You will only set this to false if 
this event is declared in an object thats will be present in all scenes and never be destroyed (Singleton with `DontDestroyOnLoad()` call).
By default `DestroyOnLoad` is set to `true`.
Event must be public to be seen.

Then in any MonoBehavior you can listen to this event:

```c#
[Subscribe("MyFirstEvent", DestroyOnLoad = false, Order = -1)]
void Listener(string s) => Debug.Log(s);
```

A string `"MyFirstEvent"` says what event we want to listen to, `DestroyOnLoad` tells whether this event handler should be destroyed when a new scene is loaded 
(similar to how it works in `[Subscribable]`) and `Order` tells order of execution this specific event handler within an event. A handlers with lower value will be 
executed earlier than those with higher.
By default `DestroyOnLoad = true` and `Order = 0`.

You can obviously point to a problem that with this approach you can no longer track what follows which event. This is obviously a huge problem, but it is pretty much 
solved by a custom editor window that allows you to see all subscribable events and their listeners. To see that window simply press `SHIFT-ALT-E` or `Window/General/Eventer`. It shows an expanded list of all events and thier handlers found in scene. You can then press "Verify" to check whether everything is fine and all handlers match their events signature. You can click on shown entries to ping and select a gameobject those entries are bound to.
If a listener subscribed to an event that can't be found in scene (either mismatch for naming or an event declared in other scene) then it will be added to `<Unknown event>` list so you can easily see all of those. They are not gonna be checked while Verifiying procedure.

## Installation
Heres few ways how you can install it to your project:

1. Open package manager inside Unity project -> press "+" -> from git url -> https://github.com/Dafiely-nyan/Unity-Eventer.git 
2. Go to releases https://github.com/Dafiely-nyan/Unity-Eventer/releases download latest (`.unitypackage`). Then inside your Unity project press right mouse click somewhere inside Assets folder -> Import package -> select downloaded package.
