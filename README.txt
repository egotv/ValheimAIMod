Copy BepInEx files to Valheim Game Folder
	Go to your Valheim game directory, and copy paste everything from PasteThisInValheimGameDirectory/ to your game directory.

Setup project references in Visual Studio
	Open ValheimILSpy/Solution.sln
	Once Visual Studio (VS) loads up, it should show all the projects in the solution: ValheimAIModLoader, ValheimAIModLivePatch, and Valheim's assembly_xxx folders.
	Expand ValheimAIModLoader, right click on References, click Add Reference, go to Browse
		Add all of the files (only files not folders) in "References" folder
		Add assembly_valheim and assembly_utils from References/publicized_assemblies
	Repeat above step for ValheimAIModLivePatch project

	Right click on Solution in Solution Explorer, go to Properties -> Configuration Properties
		Make sure only ValheimAIModLoader and ValheimAIModLivePatch are checked for Build

Compile and Install Mod
	Press Ctrl+Shift+B to build in VS
	Output ValheimAIModLoader.dll and ValheimAIModLivePatch.dll files should be in ValheimAIModLoader/bin/Debug/
	Copy ValheimAIModLoader.dll to Valheim/BepInEx/plugins
	Copy ValheimAIModLivePatch .dll to Valheim/BepInEx/scripts

Run the game
	ValheimAIModLoader's code is run once when the game starts
	ValheimAIModLivePatch's code compiles everytime you press F6, for fast-testing without reopening the game.