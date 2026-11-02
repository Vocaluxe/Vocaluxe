# Build Vocaluxe - HowTo (Linux)

Runing Vocaluxe on Linux is not offically supported yet -but you can try it yourself.  
(the problematic part is fullfilling the runtime dependencies)


* Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* Get sure gcc, g++, make and pkg-config are installed
* Install the runtime dependencies:
  - FFmpeg 8 (libavformat 62, libavcodec 62, libavutil 60, libswscale 9, libswresample 6)
  - portaudio
  - hidapi (libhidapi-libusb)
  - openal (optional, only needed for the OpenAL playback)
  - gstreamer 1.0 (optional, only needed for the GStreamer playback)
* Clone the repository and execute in its folder:
```bash
chmod ugo+x ./.build/linuxPostBuildEvent.sh
chmod ugo+x ./.build/linuxPreBuildEvent.sh
make
```
* The build should complete without errors
* change to the Output directory:
```sh
cd Output
```
* Start Vocaluxe:
```bash
./Vocaluxe
```
* If GStreamer is not installed, set `PlayBackLib` to `PortAudio` in the `Config.xml`
  
  * If yo make some progress we would be happy if you share it with us in a pull request/issue.
