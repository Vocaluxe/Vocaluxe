<img alt="Vocaluxe Logo" src="https://github.com/Vocaluxe/Vocaluxe/blob/develop/Output/Themes/Vocaluxe%202024/LogoMain.png" width=500/>

# Concept and Game Design Document

Version: **1.0.1**

Written by: **Marwin (Lead / Open Music Games.org)**

Last update: **9\. Nov. 2024**

Location: **Berlin**

Status:  **Done (100% complete)**

# CONCEPT

## BASIC SPECS

### Game Name:

Vocaluxe

### Genre:

music game, rhythm game, karaoke, sing-along

### Game Elements:

*What basic activities will the player be doing for fun?*

* Sing alone and collect scores
* Sing with friends together or against each other
* browse and listen to songs
* watch music videos

### Player:

*The number of players that can play the game at once*

* 1-6 Players

### Inspired by:

SingStar Celebration (2017), Let’s Sing 2023, UltraStar Deluxe (2008)

### Price:

free (non-commercial & open source), yet music have to be buyed in trusted stores

### Planned Release Date:

Unknown, depends on developers, ideally in 2025-2027

### Developer:

Open Music Games Organization (Germany)

### Publisher:

Open Music Games Organization (Germany)

## TECHNICAL SPECS

### Technical Form:

### View:

*Camera view from which the player will experience the game:*

* 3rd Person
* Front, only Screens
* Fixed Camera, no rotation

### Platform:

Windows

### Language:

C#

### Device:

Desktop PC

### Engine:

Selfmade Vocaluxe Engine (written with C# and MS Visual Studio, MS .NET)

### Developement Environment:
Recommended is MS Visual Studio Community 2022

## GAME PLAY

### Game Play Outline

Vocaluxe is a singing game with boundless possibilities. Players sing along their favorite songs and try to hit notes for points. Feel like a superstar while you break the highscore\! Invite your family and friends to rock the virtual stage together\!

Unlike Singstar or Let’s sing you can use custom songs from your own music collection on pc. There are already a zillion song txt files out there, and in case your favorite song is not yet available, why don't you start creating it yourself and share it with the community?

But customization doesn't stop with your personal song collection. Both games allow extensive theming and skinning or player profiles and avatars. As you can see, the sky is the limit. Make it your game\!

To start: Plug in up to four wireless USB-Microphones into your pc and connect your pc with your tv and your ready for a legendary singing party. For controlling you can use a gamepad or mouse.

### Key Features

*Which game elements are the most attractive to the player?*

1. **Custom Songs:**
the game use the community-grown UltraStar format. players can find many underground songs that are not hyped or charted. they can build a very individual song collection

2. **Scoring:**
the game analyzes pitch and rhythm of the voice in real time and gives immediate visual feedback to players how good they sing.

3. **High Score and statistics:**
the game collects all scores for each song over time and renders a statistics screen.

4. **Solos, Duets, Groups:**
Sing a duet where each player has different lyrics and notes. Or throw a party and sing with many players at the same time.

5. **Music video Background:**
while singing and looking at the score players can enjoy the music video as background.

6. **Party modes**:
Play multiple rounds or start a knock-out tournament.

7. **Song Queue:**
Play multiple songs from the queue without interruption.

8. **Medleys:**
Play a shortened mix of songs in the queue.

9. **Individual profile for player:**
a player can define a profile and choose an avatar.

10. **Jukebox**:
player can use the game as a media player that plays random songs without scoring. This helps to always have background music for the party.

# DESIGN

*This document describes how GameObjects behave, how they’re controlled and their properties. This is often referred to as the “mechanics” of the game. This documentation is primarily concerned with the game itself. This part of the document is meant to be modular, meaning you could have several different Game Design Documents attached to the Concept Document.*

## DESIGN GUIDELINES

*This is an important statement about any creative restrictions that need to be considered and includes brief statements about the general (i.e., overall) goal of the design.*

1. Vocaluxe must provide a similar experience like singstar celebration (2017).

2. But it must not be a simple copy, which means:

   1. It must have unique party modes

   2. It must have different visuals and sounds.

   3. Also they must have a good UI for large music collections that can handle over thousands of songs.

3. Vocaluxe must follow the [official ultrastar song format specification](https://github.com/UltraStar-Deluxe/format/blob/main/spec.md) \- a shared standard for all open music games.

4. The game is delivered without songs. This is due general license issues. Players have to get songs on their own discretion from 3rd-parties. A legal proper database is a totally separate project.

## GAME DESIGN DEFINITIONS

*This section established the definition of the game play. Definitions should include how a player wins, loses, transitions between modes, and the main focus of the gameplay.*

Vocaluxe has these following features:

| **Features** | Final Version v1.0.0 |  Current nightly build <v0.5.0 | Check |
| -- | -- | -- | -- | 
| **Song Collection** | Custom Songs. Limit is 10.000 songs. | Custom Songs. Limit is 10.000 songs. | ✔️ |
| **License** | Open Source, MIT-License | GPL-3.0 license  | ❗ |
| **Highscore** | For each song, for each difficulty, for each player | For each song, for each difficulty, for each player | ✔️ |
| **Statistics** | most played songs, songs never played | most played songs, songs never played | ❗ |
| **In Built Store** | No | No | ✔️ |
| **Files Stored** | Locally, No Youtube Embed-Stream | Locally, No Youtube Embed-Stream | ✔️ |
| **Translations (UI)**  | Czech, Dutch, English, French, German, Hungarian, Italian, Portuguese, Spanish, Swedish, Turkish, Chinese, Japanese | Czech, Dutch, English, French, German, Hungarian, Italian, Portuguese, Spanish, Swedish, Turkish, Chinese, Japanese | ✔️ |
| **Difficulty levels** | easy, medium, difficult | easy, medium, difficult | ✔️ |
| **Theme support** | yes, customization of look and feel. | yes, customization of look and feel. | ✔️ |
| **Playlist support** | yes (.xml) | yes (.xml) | ✔️ |
| **Party modes** | yes: at least 3 different party modes. description below. | yes: 2 party modes | ❗ |
| **Sing modes** | yes: Versus (Duell), Co-Op (Duet), Co-Op 2 (Pass the mic), Medley | yes: Versus (Duell), Co-Op (Duet), Medley | ❗ |
| **Freestyle notes support (F)** | yes | yes | ✔️ |
| **Golden notes support (\*)** | yes | yes | ✔️ |
| **Jukebox mode** | yes, play Songs without scoring, lyrics, and player profiles | no | ❗ |
| **Player Profiles** | yes, Player has at least own avatar image, name and individual high score | yes, Player with avatar image, name and individual high score | ✔️ |
| **Song Menu Style** | Only Grid View aka "Tile View". | Grid View aka "Tile View", List-View | ❗ |
| **Song Menu Search and Filter** | yes, filter by: year, genre, artist, decades, favorites, tags | yes, filter by: year, genre, artist, tags | ❗ |
| **Gamepad supported** | yes, xbox controller and similiar controller are supported | yes, xbox controller and similiar controller are supported, wii-mote | ❗ |
| **Smartphone supported** | yes, webbroswer app | yes, webbroswer app | ✔️  | 
| **Webcam Support** | yes, as optional video background | yes, as optional video background | ✔️  | 
| **Kinect Support** | no | no | ✔️  | 
| **Automatic Updates** | yes | no | ❗ |
| **Built-In Manual** | yes | no | ❗ |
| **Help System** | yes | no | ❗ |
| **Song formats** | UltraStar txt, official UltraStar Song format at least v1.1.0 | no full support of v1.1.0 yet | ❗ |
| **Supported text file encodings** | UTF8 Without BOM | UTF8 Without BOM | ✔️ |
| **Supported audio containers/formats (file suffix)** | .mp3 (CBR)  .ogg  .wav .opus .m4a .webm | yes all | ✔️ |
| **Supported video containers/formats (file suffix)** | MPEG MP4 (.mp4, .m4v) MPEG-1/MPEG-2 (.mpg, .mpeg, .ps) Microsoft Audio Video Interleave (.avi) DivX Media Format (.divx) Google WebM (.webm) Xiph.org Ogg (.ogv) Apple Quicktime (.mov, .qt) Matroska (.mkv) | yes all | ✔️ |
| **Supported cover/background filetypes** | JPEG (.jpg) Portable Network Graphics (.png) | yes .jpg and .png | ✔️ |

These features totally change the game experience for the players and make the Vocaluxe **stand out from other** open source singing games:

| **Features** | Final Version v1.0.0 |  Current nightly build <v0.5.0 | Check |
| -- | -- | -- | -- | 
| Online-Multiplayer | No | No | ✔️ |
| **Song Editor** | No in-built song editor | No in-built song editor | ✔️ |
| **Modification** | No Modification or Plugins by users | No Modification or Plugins by users | ✔️ |
| **Microphone types** | Classic Microphones | Classic Microphones | ✔️ |
| **Controlling / UI** | Keyboard and gamepad | To much Focus on Keyboard and mouse, game pad should be first in UI, Keyboard secondary | ❗ |
| **Party Modes** | Yes: Tic Tac Toe Mode, Challenge mode, Tournament mode | Yes: Tic Tac Toe Mode, Challenge mode | ❗ |
| **Song Grid** | 2x4 (=8 songs per page) | 4x6 (=24 songs per page) | ❗ |
| **Themeing** | choose from a predefined set of 6 themes | choose from a predefined set of 6 themes | ✔️ |
| **Animation of Notes** | static | static | ✔️ |
| **Option for classic karaoke evening** | No \- only supports classic singstar experience | No \- only supports classic singstar experience | ✔️ |
| **Max. number of simultaneous singers** | 1-6 Players | 1-6 Players | ✔️ |
| **Look & Feel** | strongly inspired by singstar celebration (2017) | strongly inspired by singstar celebration (2017) but total different UI layouts and controlling | ❗ |
| **Rap mode** | Yes, Rap-O-Meter: ignore pitch of notes score only rhythm don’t display normal notes show a Rap-O-Meter  | Only rap notes that ignore pitches | ❗ |
| **Practice mode** | No | No | ✔️ |
| **Collectables and score** | **collect fame, collect medals gold silver bronze**  | Nothing | ❗ |
| **Player Level-Up** | **yes**  | Nothing yet | ❗ |
| **Minimalistic Sound Design** | **yes, for UI and RatingScreens**  | Nothing yet | ❗ |
| **Basic Music Design** | **yes, at least main menu background and ResultScreen Fanfare**  | Only Background Music for main menu | ❗ |

## GAME FLOWCHART

*The game flow chart provides a visual of how the different game elements and their properties interact. Game flowcharts should represent Objects, Properties, and Actions present in the game.*

### Objects

| 1/n | Objects | Properties | Actions |
| :---- | :---- | :---- | :---- |
| n | **Players** | Player profile | sing throw a party change, add, delete profiles change options play songs in jukebox change, add, delete songs or playlists |
| n | **Player Profiles** | Profile Picture, Name, associated Microphone profile, Favorite songs, Favorite music styles, Profile created (Date), total score of all time, Level (experience), Number of finished songs, Badges | be changed, added or deleted |
| n | **Microphone Profiles** | Device Channel Color | be changed, added or deleted |
| n | **Song Collection** | Songs Playlists Owner File Location | update import export remove |
| n | **Songs** | Over 30 properties According to official UltraStar Song Format standard | be changed, added or deleted |
| n | **Playlists** | Entries: Songs | be changed, added or deleted |
| 1 | **Statistic Database** | Top 5 High Score for each song, for each difficulty, for each singing mode total number of songs Progress singing modes % progress party modes % progress songs completed %, Trophies Top 5 most sung songs Top 5 never sung songs etc….. | update import export reset |
| 1 | **Option Config** | Over 30 properties | be changed |

### Screens

| Layer 1 | Layer 2 | Layer 3 | Layer 4 | Layer 5 | Layer 6 |
| ----- | ----- | ----- | ----- | ----- | ----- |
|  **Loading & Title** |  **Main Menu** | **Singing Modes:** Duel, Duet, Medley, Practice, Rap |  | **Song Selection and Player Selection and High Score** |  **Sing Screen and Rating**  |
|  |  | **Party Modes** Party Mode 1 Party Mode 2 Party Mode 3 | **Party Mode Subscreens** |  |  |
|  |  | **Jukebox Mode** |  |  |  |
|  |  | **Statistics** |  |  |  |
|  |  | **Playlists (aka Mixtapes)** |  |  |  |
|  |  | **Options** |  |  |  |
|  |  | **Profiles** |  |  |  |
|  |  | **Credits** |  |  |  |
|  |  | **Manual** |  |  |  |
|  |  | **~~Community~~** |  |  |  |
|  |  | **~~Store~~** |  |  |  |

## PLAYER DEFINITION

### Player Definitions

*A suggested list may include: Health, Weapons, Actions*

* A player is a singer.

* By using his or her voice he can collect points. The better the player gets (and/or the longer the player plays) the more bonus items and features she unlocks during the game.

* A player designs a profile for himself by choosing an Artist name and avatar plus selecting favorite songs and music genres.

### Player Properties

*Each property should mention feedback as a result of the property changing.*

* High Score \- get High score when singing a song

* Profile Picture

* Name

* Microphone

* Favorite songs

* Favorite music styles

* Profile created (Date)

* total score of all time

* Level (experience) \- increased by total score

* Number of finished songs

### Player Rewards

*Make a list of all objects that affect the player in a positive way (e.g., health replenished)*

The better the player sings, the better the feedback of the game.

* A player is rewarded with rating pop ups, glitter effects, filled note bars right on the sing screen.

* On Rating screen: If the score raises, the applause sound effects gets more intense and the background music gets more uplifting.

* If the total score of a player increases there is a level-up. A Level Up allows a player to add more favorite songs to his or her profile.

* In this game the player makes continuous progress, there’s no throwback. If someone sings good he or she progresses faster.

## USER INTERFACE (UI)

*This is where you’ll include a description of the user’s control of the game. Think about which buttons on a device would be best suited for the game. A visual representation can be added where you relate the physical controls to the actions in the game.*

* The whole games consist of UI, HUD, Visual effects, Transition effects, * and music videos.
* There’s no world and no player avatar moving.
* The player navigates through a big menu with several layers, screens and sub screens.
* Vocaluxe focuses on the Keyboard / Gamepad.

## STYLE GUIDELINES

How should the game look & Sound? Even if players can customize the look of the game it needs good original design in the first place. We set these guidelines to make the game stand out:

* Theme: **Pop colorful, contemporary music tv**
* Backgrounds: **Flat, abstract, geometric, animated**
* Background Music: **calm, motivating, pop**
* Avatars: **Realistic with an animelike western style (3:2)**

<img alt="style-example-01" src="https://github.com/vocaluxe/vocaluxe/blob/main/Vocaluxe/Doc/Moodboard/style-example-01.png" width=400/>
<img alt="style-example-01" src="https://github.com/vocaluxe/vocaluxe/blob/main/Vocaluxe/Doc/Moodboard/style-example-02.png" width=400/>
<img alt="style-example-01" src="https://github.com/vocaluxe/vocaluxe/blob/main/Vocaluxe/Doc/Moodboard/style-example-03.png" width=400/>
<img alt="style-example-01" src="https://github.com/vocaluxe/vocaluxe/blob/main/Vocaluxe/Doc/Moodboard/style-example-04.png" width=400/>

# TEAM

Our Team is listed under [Team and Credits](https://github.com/Vocaluxe/Vocaluxe/wiki/Credits) in GitHub.

# APPROVALS

Following Project Owner approved this Game Design Document:

| Approval | Status | Date |
| :---- | :---- | :---- |
| Marwin (Open Music Games Lead) | Approved |  |
| Florian (Vocaluxe Maintainer) | In Progress |  |
