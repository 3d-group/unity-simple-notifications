[![npm package](https://img.shields.io/npm/v/com.3d-group.unity-simple-notifications)](https://www.npmjs.com/package/com.3d-group.unity-simple-notifications)
[![openupm](https://img.shields.io/npm/v/com.3d-group.unity-simple-notifications?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.3d-group.unity-simple-notifications/)
![Tests](https://github.com/3d-group/unity-simple-notifications/workflows/Tests/badge.svg)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

# Unity simple notifications

Simple but powerful UI notifications package for Unity game engine.

- [How to use](#how-to-use)
- [Install](#install)
  - [via npm](#via-npm)
  - [via OpenUPM](#via-openupm)
  - [via Git URL](#via-git-url)
  - [Tests](#tests)
- [Performance and thread safety](#performance-and-thread-safety-rocket)
- [Configuration](#configuration)

<!-- toc -->

## How to use

1. Right click scene hierarchy
2. Click UI/Notifications

![HowTo](Documentation~/images/HowTo.png)

Now you can send Notifications from your script like this:
```c#
Notifications.Send("Hello world");
```

![Notification](Documentation~/images/SimpleShowCase.gif)

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

## Install

### via npm

Open `Packages/manifest.json` with your favorite text editor. Add a [scoped registry](https://docs.unity3d.com/Manual/upm-scoped.html) and following line to dependencies block:
```json
{
  "scopedRegistries": [
    {
      "name": "npmjs",
      "url": "https://registry.npmjs.org/",
      "scopes": [
        "com.3d-group"
      ]
    }
  ],
  "dependencies": {
    "com.3d-group.unity-simple-notifications": "1.0.0"
  }
}
```
Package should now appear in package manager.

### via OpenUPM

The package is also available on the [openupm registry](https://openupm.com/packages/com.3d-group.unity-simple-notifications). You can install it eg. via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.3d-group.unity-simple-notifications
```

### via Git URL

Open `Packages/manifest.json` with your favorite text editor. Add following line to the dependencies block:
```json
{
  "dependencies": {
    "com.3d-group.unity-simple-notifications": "https://github.com/3d-group/unity-simple-notifications.git"
  }
}
```

### Tests

The package can optionally be set as *testable*.
In practice this means that tests in the package will be visible in the [Unity Test Runner](https://docs.unity3d.com/2017.4/Documentation/Manual/testing-editortestsrunner.html).

Open `Packages/manifest.json` with your favorite text editor. Add following line **after** the dependencies block:
```json
{
  "dependencies": {
  },
  "testables": [ "com.3d-group.unity-simple-notifications" ]
}
```

## Performance and thread safety :rocket:

- Notifications are rate limited based on duplicates sent recently and max notifications queue length 
- Notifications can be send from another thread. Creating GameObjects still always happens on main thread

Here is how it looks when billion notifications are sent simultaneously from another thread: :smile:

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

## Configuration

Confurable from the inspector:

![Inspector](Documentation~/images/Inspector.png)

Custom notification prefab can be created and assigned in the inspector. If doing so, consider these things:
- Prefab must have NotificationUI component (included in the package)
- Prefab must have RectTransform component with anchors set to top-stretch

If prefab is null, notification will be created dynamically.

Optional Font parameter is only used when notification is created dynamically - if that is the only thing you want to change.
