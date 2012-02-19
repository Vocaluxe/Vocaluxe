
PortAudioSharp - PortAudio bindings for .NET
Copyright (C) 2006-2011  Riccardo Gerosa
--------------------------------------------

About
-----
This library provides .NET bindings for the portable low-latency
audio library PortAudio v.19.
The bindings are available directly through the PortAudio class, 
or you can even use and a simplified higher-lever Audio class.
This library is being developed using C# under Linux/mono and then
tested also on other platforms (Windows/.NET only since now)
for maximum portability.
There is no "unsafe" code.


Compiling
---------
Solution and project files for MonoDevelop 0.10 and 
SharpDevelop 4.0 / Visual Studio 2008 are provided.
The solution contains three projects: The PortAudioSharp library
project, a simple test project called PortAudioSharpTest and a
WindowsFormsTest project used to test Windows Forms integration
(this feature is not yet complete).


Installing / Using
------------------
1) To use the PortAudioSharp bindings you must have the PortAudio v.19
   library installed on your system, you can find PortAudio source code at
   http://www.portaudio.com
   Under Windows the PortAudio DLL must be named "PortAudio.dll", so
   rename this file if necessary.
   If you are using a Linux/Unix system you will have to compile and install
   the "libportaudio.so" library.

2) If you are using a source distribution of PortAudioSharp you need 
   to compile it.

3) Copy PortAudioSharp.dll and PortAudioSharp.dll.config files to the 
   directory of your assembly.

4) Add a reference in your project to PortAudioSharp.dll


Known bugs
----------
This release may still contain some bugs,
it has been tested mainly under Windows and some versions of Linux (with Mono).
Should also work on Mac OS X (with Mono).
Please help us testing, debugging and improving these bindings!

