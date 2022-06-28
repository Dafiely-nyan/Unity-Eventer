# Unity Eventer

## tl;dr
Allows to add events from MonoBehaviours to container and easily subscribe to them without a need to care about explicitly subscribing / unsubscribing from them

## The problem
Whenever you have to deal with anything events related in Unity you can quite quickly fall into an annoying problem: you need to care way too much about subscribing and
unsubscribing from them. For example, lets take a very simple `Network` class that fires an event when a user sends request:
```c#
using System;
using UnityEngine;

public class Network : MonoBehaviour
{
    public event Action OnRequestStart;

    public void SendRequest()
    {
        // some logic could be here
        OnRequestStart?.Invoke();
    }

    // Singleton so we can easily access it from other scripts
    // Also not gonna be deleted between scenes since all scenes could rely on network events
    public static Network Instance;

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
}
```
Its pointless to have events if we dont listen to them, so lets say we have a class `Player` that has some logic that needs to run when we send a request.
In simple case it could look something like this:
```c#
using UnityEngine;

public class Player : MonoBehaviour
{
    public string Name = "John";

    private void Start()
    {
        Network.Instance.OnRequestStart += OnRequestStart;
    }

    private void OnDestroy()
    {
        Network.Instance.OnRequestStart -= OnRequestStart;
    }

    void OnRequestStart()
    {
        Debug.Log("Request started and my name is " + Name);
    }
}
```
In `Start` we subscribe and in `OnDestroy` we unsubscribe. `Player` in this example will be only within a single scene, while instance of `Network` will not be
destroyed while application is running, so when a new scene is loaded we need to make sure we unsubscribed from its events, otherwise we will get an errors.
This is where it gets annoying. If we need to add another method that needs to react to some event (handler) we again need to add it to `Start` and `OnDestroy`. 
And even something worse, what if you have to make sure a specific handler runs exactly after other handlers completed their job? This could be a disaster.
In reality there could be tens of scripts that rely on such events making you spend too much time on adding them everywhere.

This where **Eventer** enters the game. 

## The solution
**Eventer** provides two Attributes: `[Subscribable]` and `[Subscribe]` that you can apply to MonoBehaviours in your scene. Then, .NET Reflection will check every
of them and handle all subscriptions for you. Lets see how previous example will change using those attributes instead.

The only thing we need to change in `Network` class is to add `[Subscribable]` attribute on top of our event:
```c#
[Subscribable("MyEvent")]
public event Action OnRequestStart;
```
The `string` you pass to its constructor bind that event for that specific string. Then, in any MonoBehaviour you can then easily subscribe to it by adding 
`[Subscribe]` attribute on top of a method that needs to listen to that event. Our `Player` class now will look like this:
```c#
using Eventer;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string Name = "John";

    [Subscribe("MyEvent", 0, true)]
    void OnRequestStart()
    {
        Debug.Log("Request started and my name is " + Name);
    }
}
```
A `string` first param tells to which event listen, `int` second param specifies order of execution (default 0) and `bool` param specifies whether it is needed
to unsubscribe this method from event it is listening to when a new scene is loaded (default true). You'd normally set last param to `false` in case an
object this MonoBehaviour is attached to has `DontDestroyOnLoad()` somewhere.
```c#
    [Subscribe("MyEvent", 0)]
    void OnRequestStart()
    {
        Debug.Log("Request started and my name is " + Name);
    }
    
    // will execute before OnRequestStart() when event is fired
    [Subscribe("MyEvent", -1)]
    void OnRequestStart2()
    {
        Debug.Log("Request started and my name is " + Name);
    }
```

*to be continued..*
