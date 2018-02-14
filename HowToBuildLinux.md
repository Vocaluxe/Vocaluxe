# Build Vocaluxe - HowTo (Linux)

Runing Vocaluxe on Linux is not offically supported yet -but you can try it yourself.  
(the problematic part is fullfilling the runtime dependencies)


* Install ffmpeg 3.2 (a quick and dirty compile script can be found here: [installFFmpeg.sh](https://github.com/lukeIam/Vocaluxe/blob/travis/.travis/installFFmpeg.sh) )
* Get sure your gcc and g++ version is >= 4.8
* Install at least mono 5.8.0 ([HowTo install mono](http://www.mono-project.com/download/stable/))
* Clone the repository
You want to use this branch as it contains some linux specific changes   
(compiles on Ubuntu 14.04): [travis@lukeIam/Vocaluxe](https://github.com/lukeIam/Vocaluxe/tree/travis)
* Navigate to the travis branch and execute (commands from [.travis.yml](https://github.com/lukeIam/Vocaluxe/blob/travis/.travis.yml)):
```bash
chmod ugo+x ./.build/linuxPostBuildEvent.sh
chmod ugo+x ./.build/linuxPreBuildEvent.sh
chmod ugo+x ./.travis/gitDescribe.sh
wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -P ./.travis/

config=Linux platform=x64

# ./.travis/gitDescribe.sh
mono ./.travis/nuget.exe restore
make -C PitchTracker
make -C Vocaluxe/Lib/Video/Acinerella
msbuild /p:Configuration=Release$config /p:Platform=$platform /p:TargetFrameworkVersion=v4.7 Vocaluxe.sln
```
* The build should complete without errors
* change to the Output directory:
```sh
cd Output
```
* Start Vocaluxe:
```bash
mono Vocaluxe.exe
```
* Vocaluxe will start (you will see the splash screen) and then crash because runtime dependencies are missing    
Required dependencies (by heart - can be incorrect and incomplete):  
  - gstreamer 1.0
  - ffmpeg 3.2 (maybe we can use the build script which builds the libs at the moment)
  - hidapi
  - libgstreamersharpglue-1.0.0
  - portaudio
  - ?
  
  * If yo make some progress we would be happy if you share it with us in a pull request/issue.
