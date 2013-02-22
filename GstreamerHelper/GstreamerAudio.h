#pragma once
#include <string>
#include <map>
#include <queue>
#include "gst/gst.h"
#include "GstreamerAudioStream.h"
#include "gst\gst.h"

#define DllExport extern "C" __declspec(dllexport)

using namespace std;

	typedef void (__stdcall * LogCallback)(const char* text);
	static LogCallback Log;

	DllExport void SetLogCallback(LogCallback Callback);

	DllExport bool Init();
	DllExport void SetGlobalVolume(float Volume);
	DllExport int GetStreamCount(void);
	DllExport void CloseAll(void);

	//Stream handling
	DllExport int Load(const wchar_t* Media);
	DllExport int LoadPrescan(const wchar_t* Media, bool Prescan);
	DllExport void Close(int Stream);

	DllExport void Play(int Stream);
	DllExport void PlayLoop(int Stream, bool Loop);
	DllExport void Pause(int Stream);
	DllExport void Stop(int Stream);
	DllExport void Fade(int Stream, float TargetVolume, float Seconds);
	DllExport void FadeAndPause(int Stream, float TargetVolume, float Seconds);
	DllExport void FadeAndStop(int Stream, float TargetVolume, float Seconds);
	DllExport void SetStreamVolume(int Stream, float Volume);
	DllExport void SetStreamVolumeMax(int Stream, float Volume);

	DllExport float GetLength(int Stream);
	DllExport float GetPosition(int Stream);

	DllExport bool IsPlaying(int Stream);
	DllExport bool IsPaused(int Stream);
	DllExport bool IsFinished(int Stream);

	DllExport void SetPosition(int Stream, float Position);

	DllExport void Update();

	void LogError(const char* msg);
