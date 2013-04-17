Vocaluxe 0.2 README
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
- WinXP, Vista, Windows 7 or Windows 8 with .NET 4.0
- 1 GHz CPU, 512 MB RAM, Graphics card with OpenGL 2.1 or DirectX 9.0 support
- Visual C++ Redistributable Packages 2008 and 2012


=================================
= 2. Release Notes              =
=================================

- The program supports 32bit and 64bit (Vocaluxe_x64.exe) operating systems.

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
  
- You can use your WebCam to take profile pictures in the profile screen. Activate your camera
  before you start the program. If Vocaluxe detects the camera a button "Webcam" appears in the
  profile screen.

- Vocaluxe supports the Wiimotion remote control from Nintendo. Connect it with your bluetooth
  connection manager and start Vocaluxe. If a Wiimote is recognized it will perform two short rumbles
  and the third LED will lighten up.
  If you have troubles to connect the Wiimote try this:
  
  1. First connect the Wiimote normally but pressing 1+2 and let the bluetooth manager detect it.
  2. Connect (a pin or password is not needed).
  3. Disconnect.
  4. Press 1+2 Again but DO NOT DO ANYTHING ELSE. Let the WIIMOTE blink, until it times out (stops blinking.)
  5. THEN click connect in the bluetooth settings, when it says looking for device PRESS A or + or Z
     (it doesn't matter), just get the thing to blink. (Just don't hit 1+2 or the Sync button, this resets
	 the sync info.)
  It will then connect.
  

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

-PlaylistFolder [Path] : Use [Path] as playlist folder. The path needs to be an absolute path. Vocaluxe will
						 load playlist from this path.
						 Example:
						 Vocaluxe.exe -PlaylistFolder D:\MyPlaylists


Complete example:
Vocaluxe.exe -ConfigFile MyConfig.xml -ScoreFile C:\Vocaluxe\Highscores\MyHighscoreDB.sqlite -SongFolder D:\MySongCollection -PlaylistFolder D:\MyPlaylists


=================================
= 4. Controls                   =
=================================
[Mouse]				to navigate through the screens: Left button: select/manipulate elements,
								 Right button: go to previous screen.
[Cursor] 			to navigate through the screens.
[Enter]  			to confirm
[Escape] or [Back] 		to go to the previous screen.

[ALT] + [P]			to take a ScreenShot. Screenshots are saved in the directory "Screenshots".
[ALT] + [ENTER]			to toggle full screen mode.
[SHIFT] + [F1] 			to toggle theme edit mode. The theme edit mode is experimental. It allows you
						to change the size and position of the element on the screens. You can save
						your changes with [S] before leaving the theme edit mode.
[TAB]				to open the background music controls (not on all screens).
[SHIFT] + [+]			to increase volume
[SHIFT] + [-]			to decrease volume

Song screen
[F3]				to open/close the song search menu
[SPACE]				to open/close the song menu
[CTRL] + [R]			to select a random song
[CTRL] + [A]			to sing all songs
[CTRL] + [V]			to sing all songs from a category
[NUM_1]..[NUM_6]		to use a Joker for team 1..6
[A]..[Z]			to jump to category/song title starting with that letter

Name selection screen
[1]..[6]			to activate player selection.
[Cursor]			to select a profile.
[Enter]				to confirm a selection.

Sing screen
[P]				to toggle pause.
[CTRL] + [S]			to skip a song if there is another one in the playlist.
[T]				to change the time format of the timer.
[I]				to change view of player information.
[W]				to activate the configured webcam.


=================================
= 5. Help & Suppport            =
=================================
Bug Tracker:			http://84.200.73.138/redmine/projects/vocaluxe
Translations:			https://www.transifex.com/projects/p/vocaluxe/
GitHub Wiki:			https://github.com/Vocaluxe/Vocaluxe/wiki
Support Forum (German):	http://www.ultra-star.de
Song-DataBase (USDB):	http://usdb.animux.de/


=================================
= 6. Source Code                =
=================================
Source Code (GitHub):	https://github.com/Vocaluxe/Vocaluxe
SF.Net Page:			http://sourceforge.net/projects/vocaluxe/