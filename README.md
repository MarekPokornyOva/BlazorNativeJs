# BlazorNativeJs

[![Package Version](https://img.shields.io/nuget/v/BlazorNativeJs.svg)](https://www.nuget.org/packages/BlazorNativeJs)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BlazorNativeJs.svg)](https://www.nuget.org/packages/BlazorNativeJs)
[![License](https://img.shields.io/github/license/MarekPokornyOva/BlazorNativeJs.svg)](https://github.com/MarekPokornyOva/BlazorNativeJs/blob/master/LICENSE)

### Description
BlazorNativeJs is experimental project aiming to support direct access to DOM/JS objects (e.g. window,document,events) from C# code in Blazor WASM application.

### How to use?
1) Add Nuget package https://www.nuget.org/packages/BlazorNativeJs.Server/ to your Blazor.Server project.
2) Add `app.UseNativeJsHack();` in your Blazor server app (see \Samples\01\BlazorApp.Server\Startup.cs). This allows BlazorNativeJs to handle event objects and also provide necessary JS code.
3) Add Nuget package https://www.nuget.org/packages/BlazorNativeJs/ to your Blazor.Client project.
4) Call `NativeJs.Initialize(...)` from Program.cs (see \Samples\01\BlazorApp.Client\Program.cs).
5) Sample usage: `NativeJs.GetDocument().getElementsByTagName("h1")[0].innerText="BlazorNativeJs is great!";`
6) See \Samples\01\BlazorApp.Client\Pages for further usage examples.

### How does it work?
Every object to be returned from JS area is stored to a specific dictionary in browser and a referencing key is returned to C# area instead. C# instantiates a Dynamic object keeping the reference. Accessing a member of the Dynamic object causes call a specific JS function which translates the reference back to the real JS object and process the requested action.

### What if I find a scenario where the solution fails?
This project is experimental and in early development phase. It's expected such case will happen. Don't hesitate to create issue with minimal repro steps.

### Call for help
In order to make all this functional, it's needed to inject some specific JS code (see \src\BlazorNativeJs.Server\NativeJsApplicationBuilderExtensions.cs) into blazor.webassembly.js. I haven't found a way to do it during Blazor framework initialization nor page load so it's done on server side. If anybody success on this field, don't hesitate to make a PR.

### Notes
- all is provided as is without any warranty.
- the target of this concept has been "make it functional for any price". Therefore some pieces are bit "hacky".
- developed with version 3.2.0-preview1.20073.1 wasm.

### Thanks to Blazor team members for their work
