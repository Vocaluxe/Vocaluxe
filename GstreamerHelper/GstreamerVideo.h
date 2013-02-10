#pragma once
#include <string>
#include <map>
#include <queue>
#include "gst/gst.h"
#define DllExport extern "C" __declspec(dllexport)

using namespace std;

	typedef void (__stdcall * LogCallback)(const char* text);
	static LogCallback Log;

	DllExport void SetVideoLogCallback(LogCallback Callback);

    DllExport bool InitVideo();
    DllExport void CloseAllVideos();

    DllExport int LoadVideo(const wchar_t* Media);
    DllExport bool CloseVideo(int StreamID);
    DllExport int GetVideoNumStreams();

    DllExport float GetVideoLength(int StreamID);
	DllExport UINT8* GetFrame(int StreamID, float Time, float &VideoTime);
    DllExport bool Skip(int StreamID, float Start, float Gap);
    DllExport void SetVideoLoop(int StreamID, bool Loop);
    DllExport void PauseVideo(int StreamID);
	DllExport void ResumeVideo(int StreamID);

    DllExport bool Finished(int StreamID);

	void LogVideoError(const char* msg);

