<div align="center" style="text-align: center">
  <img src="https://vlly-web.s3.amazonaws.com/vlly.svg" alt="Vlly Unity SDK" height="200"/>
</div>

# Table of Contents

<!-- MarkdownTOC -->
- [Introduction](#introduction)
- [Quick Start Guide](#quick-start-guide)
    - [Install Vlly](#1-install-vlly)
    - [Initialize Vlly](#2-initialize-vlly)
    - [Capture a Clip](#3-capture-a-clip)
    - [Wath Clip In Dashboard](#4-watch-clip-in-dashboard)
- [FAQ](#faq)

<!-- /MarkdownTOC -->

# Overview
Welcome to the official Vlly Unity SDK. The Vlly Unity SDK is an open-source project, and we'd love to see your contributions!

<!-- Check out our [official documentation]() to learn how to make use of all the features we currently support! -->

# Quick Start Guide
Supported Unity Version >= 2018.3. For older versions, you need to have `.NET 4.x Equivalent` selected as the scripting runtime version in your editor settings.
## 1. Install Vlly
This library can be installed using the unity package manager system with git. We support Unity 2018.3 and above. For older versions of Unity, you need to have .NET 4.x Equivalent selected as the scripting runtime version in your editor settings.

* In your unity project root open ./Packages/manifest.json
<!-- * Add the following line to the dependencies section "com.mixpanel.unity": "https://github.com/mixpanel/mixpanel-unity.git#master", -->
<!-- * Open Unity and the package should download automatically
Alternatively you can go to the [releases page](https://github.com/mixpanel/mixpanel-unity/releases) and download the .unitypackage file and have unity install that. -->
## 2. Initialize Vlly
To start capturing clips with the Vlly Unity Library, you must first initialize it with your API Key. You can find your API Key on the homepage of the dashboard.

To initialize the library, first open the unity project settings menu for Vlly. (Edit -> Project Settings -> Vlly) Then, enter your api key into the Api Key input field within the inspector.
Please note if you prefer to initialize Vlly manually, you can select the `Vlly Initialization` in the settings and call `Vlly.Init()` to initialize.
<img width="633" alt="Screenshot 2023-08-09 at 2 03 53 AM" src="https://github.com/tremayne-stewart/vlly-unity/assets/1385885/d005fa66-5189-4782-8f73-a0b9ad386d5b">


## 3. Capture a Clip
let's get started by creating your first clip. You can trigger recording a clip from anywhere in your application. 
```csharp
using  vlly;

// Then, you can start capturing a clip with
Vlly.StartRecording("TriggerKey");
```

## 4. Watch Clip in Dashboard
[Open up the Vlly Dashboard]() to view the incoming clips.  Within hours you’ll see captured clips to be viewed in the dashboard.
![web-vlly-screencast](https://github.com/tremayne-stewart/vlly-unity/assets/1385885/9d9d176f-df01-4ebe-9f30-c39c4579ec1c)


# FAQ

**How does privacy work**
* **[How does Vlly ensure privacy?](https://bit.ly/vlly-ensuring-privacy)**
* **[Data Processing Addendum](https://bit.ly/vlly-dpa-from-privacy-doc)**

## Want to Contribute?

The Vlly library for Unity is an open source project, and we'd love to see your contributions!
We'd also love for you to come and work with us! 
