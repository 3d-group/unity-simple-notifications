# Unity simple notifications

Simple but powerful UI notifications package for Unity game engine.

## Usage

Usage:
1. Right click scene hierarchy
2. Click UI/Notifications

![HowTo](Documentation~/images/HowTo.png)

Now you can send Notifications from your script like this:
```c#
Notifications.Send("Hello world");
```

![Notification](Documentation~/images/SimpleShowCase.gif)

## Performance and thread safe :rocket:

Here is how it looks when billion notifications are sent simultaneously from another thread:

![Notification](Documentation~/images/PerformanceShowCase.gif)

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
