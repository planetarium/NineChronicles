Contributor guide
=================

Note: This document at present is for only code contributors.
We should expand it so that it covers reporting bugs, filing issues,
and writing docs.


Questions & online chat  [![Discord](https://img.shields.io/discord/928926944937013338.svg?color=7289da&logo=discord&logoColor=white)][Discord server]
-----------------------

We have a [Discord server] to discuss Nine Chronicles.  There are some channels
for purposes in the *Nine Chronicles* category:

 -  *#general*: A space for general discussions related to Nine Chronicles or community interactions.
 -  *#9c-unity*: Chat with maintainers and contributors of Nine Chronicles.
    Ask questions to *hack*  Nine Chronicles and to make a patch for it.  People here
    usually speak in Korean, but feel free to speak in English.

[Discord server]: https://planetarium.dev/discord


Nine Chronicles Developer Portal
-----------------------
For a comprehensive overview of the game, development resources, and documentation, visit the [Nine Chronicles Developer Portal]. The portal provides insights into the game’s decentralized nature, blockchain mechanics, and various guides for developers and players alike.

[Nine Chronicles Developer Portal]: https://nine-chronicles.dev/


Prerequisites
-------------
Before contributing to the NineChronicles repository, ensure you have the following:

Unity Version: You need to have [Unity 2021.3.37f1] installed. This is the required version for developing and running NineChronicles.

[Unity 2021.3.37f1]: https://unity.com/kr/releases/editor/whats-new/2022.3.37#notes


Installation
-------------
 1. Install [Unity Hub]
 1. Install Unity 2021.3.37f1 version
 1. Clone repository
    ```
    git clone https://github.com/planetarium/NineChronicles.git
    ```
 1. Navigate to the cloned directory and run the command:
    ```
    git config core.hooksPath hooks
    git submodule update --init --recursive
    ```
 1. Run Unity and build project

To launch Nine Chronicles from the Unity editor, please follow the [step-by-step guide][9c-unity-guide].

[9c-unity-guide]: https://nine-chronicles.dev/forum-trunk/playing-the-nine-chronicles-local-network-with-the-unity-editor


How to play in the editor
-------------
If you want to run on the editor, please press the run button on 'IntroScene', or press 'Donguri button' at the top to go to that scene and start the game.


Lib9c Submodule
-------------
This repository uses **lib9c** as a submodule, which contains core logic and blockchain elements essential to NineChronicles. To initialize and update the submodule
```bash
git submodule init
git submodule update
```

It is located under `Assets/_Scripts/Lib9c` in the repository.

For contributions related to **lib9c**, refer to the [lib9c repository](https://github.com/planetarium/lib9c) and ensure changes are reflected when updating the submodule.


Coding Style
-----------------------

### Indentation
- Follow the default settings for indentation.
- Disable alignment of multi-line assignments based on operators.

Example:
```csharp
// Disabled alignment
{
    x = 1;
    yyy = 2;
    zzzz = 3;
}
```

### Naming
- Use default naming conventions.

### Syntax Style

#### Braces
- Always enforce braces for `if`, `for`, `foreach`, `while`, `do-while`, `using`, `lock`, and `fixed` statements.
- Set notifications to **Suggestion** level.

#### Code Body
- Prefer block body over expression body for functions.  
- Recommend expression body for properties, indexers, and events.

```csharp
// Block body
private int Add(int a, int b)
{
    return a + b;
}
```

#### Attributes
- Separate each attribute with its own square brackets.  

### Braces Layout
- Use BSD style (default setting).

### Blank Lines
- Follow the default settings.

### Line Breaks and Wrapping
- Stick to the default settings unless otherwise needed.
- Wrap limit: 300 characters.

#### Arrangement of Attributes
- Place field or property/indexer/event attribute on the same line

### Spaces
- Use default settings except for **Between attribute sections**, which is set to `false`.