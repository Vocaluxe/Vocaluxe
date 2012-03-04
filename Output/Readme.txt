Vocaluxe 0.1 README
----------------------------

=================================
= 1. About                      =
= 2. Release Notes              =
= 3. Command-Line Parameters    =
= 4. Controls                   =
= 5. Help & Support             =
= 6. Source Code                =
=================================


=================================
= 1. About                      =
=================================

Vocaluxe is a free and open source singing game. It allows up to three players (currently) to sing
along with music using microphones in order to score points, depending on the pitch of the voice
and the rhythm of singing.

Vocaluxe is an entirely new development in C#, inspired by the original UltraStar (developed by corvus5)
and the great Ultrastar Deluxe project.

Supported Operating Systems / Requirements:
- WinXP, Vista, Windows 7 with .NET 2.0
- 1 GHz CPU, 512 MB RAM, Graphics card with OpenGL 2.1 or DirectX 9.0 support


=================================
= 2. Release Notes              =
=================================

- The program supports 32bit and 64bit (Vocaluxe_x64.exe) operating systems.

- The program uses OpenGL as the default renderer. If your graphics card driver does not or not
  properly support OpenGL, you can try the Direct3D renderer by editing your Config.xml in the
  application's main directory:
  
	<Graphics>
		<!--Renderer: TR_CONFIG_SOFTWARE, TR_CONFIG_OPENGL, TR_CONFIG_DIRECT3D-->
		<Renderer>TR_CONFIG_DIRECT3D</Renderer>

- To add your existing song collection to Vocaluxe, put your songs into the 'Songs' subfolder.

  Alternatively, you can add multiple song paths to Vocaluxe by editing your Config.xml:

	<Game>
		<!--SongFolder: SongFolder1, SongFolder2, SongFolder3, ...-->
		<SongFolder1>C:\Vocaluxe\Songs</SongFolder1>
		<SongFolder2>D:\EvenMore\Songs</SongFolder2>

- To use your highscore database from UltraStar deluxe 1.0.1, UltraStar deluxe CMD Mod or 
  UltraStar deluxe 1.1, simply copy your Ultrastar.db (rename if necessary) into the main
  directory. Your highscore database will be imported into the HighscoreDB.sqlite file when
  Vocaluxe is started. Upon successful import, the old database is deleted.
  
- To add your own player avatars, simply add some picture files to the 'Profiles' subfolder.
  Supported file types: png, jpg (jpeg), bmp.
  Images should have square proportions, e.g. 400x400 px.


=================================
= 3. Command-Line Parameters    =
=================================
The following command-line parameters may be passed to the game when starting it through a shortcut
or from the console:

-ConfigFile [Path\File]: Use [Path\File] as configuration file instead of Config.xml in the main folder.
						 The path to the file has to exist and needs to be an absolute path.
						 If only a filename is specified, the file is assumed to be in the current directory.
						 Examples: 	Vocaluxe.exe -ConfigFile MyConfig.xml
									Vocaluxe.exe -ConfigFile C:\Vocaluxe\Configs\MyConfig.xml

-ScoreFile [Path\File] : Use [File] as score database instead of HighscoreDB.sqlite in the application's
						 main folder. The path to the file has to exist and needs to be an absolute path.
						 If only a filename is specified, the file is assumed to be in the current directory.
						 Examples: 	Vocaluxe.exe -ScoreFile MyHighscoreDB.sqlite
									Vocaluxe.exe -Scorefile C:\Vocaluxe\Highscores\MyHighscoreDB.sqlite

-SongFolder [Path]	   : Use [Path] as song folder. The path has to exist and needs to be an absolute path.
						 Note that any song folders set in the config file will be ignored.
						 This parameter may be used multiple times to use different song folders.
						 Examples:	Vocaluxe.exe -SongFolder D:\MySongCollection
									Vocaluxe.exe -SongFolder D:\MySongCollection -SongFolder E:\MoreSongs
						  
-SongPath [Path]	   : Deprecated, see SongFolder [Path]. This alias is provided for convenience.

Complete example:
Vocaluxe.exe -ConfigFile MyConfig.xml -ScoreFile C:\Vocaluxe\Highscores\MyHighscoreDB.sqlite -SongFolder D:\MySongCollection


=================================
= 4. Controls                   =
=================================
[Mouse]					to navigate through the screens: Left button: select/manipulate elements,
						Right button: go to previous screen.
[Cursor] 				to navigate through the screens.
[Enter]  				to confirm
[Escape] or [Back] 		to go to the previous screen.

[ALT] + [P]				to take a ScreenShot. Screenshots are saved in the directory "Screenshots".
[ALT] + [ENTER]			to toggle full screen mode.
[SHIFT] + [F1] 			to toggle theme edit mode. The theme edit mode is experimental. It allows you
						to change the size and position of the element on the screens. You can save
						your changes with [S] before leaving the theme edit mode.
[TAB]					to open the background music controls (not on all screens).

Songscreen
[R]						to select a random song
[F3] or [CTRL]+[F]		to open/close the song search menu
[A]						to sing all songs from a category


=================================
= 5. Help & Suppport            =
=================================
Support Forum (German):	http://www.ultra-star.de
Song-DataBase (USDB):	http://usdb.animux.de/


=================================
= 6. Source Code                =
=================================
Source Code (GitHub):	https://github.com/Vocaluxe/Vocaluxe
SF.Net Page:			http://sourceforge.net/projects/vocaluxe/