######################################

[HOW TO INSTALL MOD AND PLAY]

Copy BepInEx files to Valheim Game Folder
	Go to your Valheim game directory, and copy paste everything from PasteThisInValheimGameDirectory/ to your game directory.
	Delete copied files to uninstall.

Run the game
	Load into your world
	Once the player spawns, press F6.
	
Keybinds
	T: Start/stop recording your voice
	Y: Send your voice recording to Brain API
	
	(dev npc commands/to be removed)
	F: Order to follow player
	K: Order to attack enemies
	P: Order to patrol where you are standing
	H: Order to harvest resources
	U: Order to drop inventory
	
	
	
######################################
	
[DEVELOPMENT ENVIRONMENT SETUP] (not needed to just play the mod)

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
	
	ValheimAIModLoader's code is run once when the game starts
	ValheimAIModLivePatch's code compiles everytime you press F6, for fast-testing without reopening the game.