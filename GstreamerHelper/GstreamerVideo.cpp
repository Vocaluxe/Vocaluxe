#include "stdafx.h"
#include "GstreamerVideo.h"
#include "GstreamerVideoStream.h"

map<int, GstreamerVideoStream*> VideoStreams;
queue<int> VideoIDs;

DllExport void SetVideoLogCallback (LogCallback Callback)
{
	Log = Callback;
}

DllExport bool InitVideo()
{
	for(int i = 0; i < 1000; i++)
	{
		VideoIDs.push(i);
	}
#if _WIN64
	SetDllDirectory(L".\\x64\\gstreamer");
#else
		SetDllDirectory(L".\\x86\\gstreamer");
#endif

	gst_init(NULL, NULL);

	GstRegistry* registry = gst_registry_get();

#if _WIN64
	gst_registry_scan_path(registry, ".\\x64\\gstreamer");
#else
	gst_registry_scan_path(registry, ".\\x86\\gstreamer");
#endif
	return true;
}

DllExport void CloseAllVideos(void)
{
  map<int, GstreamerVideoStream*>::iterator p;
  
  for(p = VideoStreams.begin(); p != VideoStreams.end(); p++) {
	  p->second->CloseVideo();
  }
}

DllExport int LoadVideo(const wchar_t* Media)
{
	GstreamerVideoStream *s = new GstreamerVideoStream(VideoIDs.front());
	VideoStreams.insert(pair<int, GstreamerVideoStream*> (VideoIDs.front(), s));
	VideoIDs.pop();
	return s->LoadVideo(Media);
}

DllExport bool CloseVideo(int Stream)
{
	map<int,GstreamerVideoStream*>::iterator it = VideoStreams.find(Stream);
	if(it != VideoStreams.end())
	{
		delete it->second;
		VideoStreams.erase(it);
	}
	else return false;
	return true;
}

DllExport int GetVideoNumStreams()
{
	return (int) VideoStreams.size();
}

DllExport float GetVideoLength(int Stream)
{
	map<int,GstreamerVideoStream*>::iterator it = VideoStreams.find(Stream);
	if(it != VideoStreams.end())
	{
		return it->second->GetVideoLength();
	}
	else return -1.0;
}

DllExport struct ApplicationFrame GetFrame(int Stream, float Time)
{
	map<int,GstreamerVideoStream*>::iterator it = VideoStreams.find(Stream);
	if(it != VideoStreams.end())
	{
		return it->second->GetFrame(Time);
	}
	struct ApplicationFrame returnFrame;
	returnFrame.size=0;
	returnFrame.width=0;
	returnFrame.height=0;
	returnFrame.data=NULL;
	returnFrame.videotime=0;
	return returnFrame;
}

DllExport bool Skip(int Stream, float Start, float Gap)
{
	map<int,GstreamerVideoStream*>::iterator it = VideoStreams.find(Stream);
	if(it != VideoStreams.end())
	{
		return it->second->Skip(Start, Gap);
	}
	else return false;
}

DllExport void SetVideoLoop(int Stream, bool Loop)
{
	map<int,GstreamerVideoStream*>::iterator it = VideoStreams.find(Stream);
	if(it != VideoStreams.end())
	{
		it->second->SetVideoLoop(Loop);
	}
}

DllExport void PauseVideo(int Stream)
{
	map<int,GstreamerVideoStream*>::iterator it = VideoStreams.find(Stream);
	if(it != VideoStreams.end())
	{
		it->second->PauseVideo();
	}
}

DllExport void ResumeVideo(int Stream)
{
	map<int,GstreamerVideoStream*>::iterator it = VideoStreams.find(Stream);
	if(it != VideoStreams.end())
	{
		it->second->ResumeVideo();
	}
}

DllExport bool Finished(int Stream)
{
	map<int,GstreamerVideoStream*>::iterator it = VideoStreams.find(Stream);
	if(it != VideoStreams.end())
	{
		return it->second->IsFinished();
	}
	else return true;
}

DllExport void UpdateVideo(void)
{
  map<int, GstreamerVideoStream*>::iterator p = VideoStreams.begin();

  while(p != VideoStreams.end())
  {
	  if(p->second->Closed)
	  {
		delete p->second;
		VideoStreams.erase(p++);
	  }
	  else {
		p->second->UpdateVideo();
		++p;
	  }
  }
}

void LogVideoError(const char* msg)
{
	Log(msg);
}