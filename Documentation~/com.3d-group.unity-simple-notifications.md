# Unity simple notifications

Simple but powerful UI notifications package for Unity game engine.

- [How to use](#how-to-use)
- [Performance and thread safety](#performance-and-thread-safety)
- [Configuration](#configuration)

<!-- toc -->

## How to use

1. Right click scene hierarchy
2. Click UI/Notifications

![HowTo](images/HowTo.png)

Now you can send Notifications from your script like this:
```c#
Notifications.Send("Hello world");
```

![Notification](images/SimpleShowCase.gif)

Specify type (changes notification color) and click events with optional parameters:
```c#
private void Error()
{
   Notifications.Send("Spooky error!", NotificationType.Error, OnClick);
}

public void OnClick()
{
    // Do something.
}
```
There is also async overload:
```c#
await Notifications.SendAsync("Warning!", NotificationType.Warning);
```

## Performance and thread safety

- Notifications are rate limited based on duplicates sent recently and max notifications queue length 
- Notifications can be send from another thread. Creating GameObjects still always happens on main thread

Here is how it looks when billion notifications are sent simultaneously from another thread:

![Notification](images/PerformanceShowCase.gif)

Try it yourself! Code:
```c#
using UnityEngine;
using System.Threading;
using Group3d.Notifications;

public class TEST : MonoBehaviour
{
    private void Start()
    {
        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            var r = new System.Random();
            int counter = 0;
            while (counter < 1000000000)
            {
                Notifications.Send($"Test {r.Next(0, 10000)}");
                counter++;
            }
        }).Start();
    }
}

```

## Configuration

Confurable from the inspector:

![Inspector](images/Inspector.png)

Custom notification prefab can be created and assigned in the inspector. If doing so, consider these things:
- Prefab must have NotificationUI component (included in the package)
- Prefab must have RectTransform component with anchors set to top-stretch

If prefab is null, notification will be created dynamically.

Optional Font parameter is only used when notification is created dynamically - if that is the only thing you want to change.
