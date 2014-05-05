#include "stdafx.h"
#include "GstreamerAudio.h"

#define DllExport extern "C" __declspec(dllexport)

map<int, GstreamerAudioStream*> Streams;
queue<int> IDs;
float GlobalVolume = 1.f;
EXTERN_C IMAGE_DOS_HEADER __ImageBase;
DllExport void SetLogCallback (LogCallback Callback)
{
	Log = Callback;
}

DllExport bool Init()
{
	for(int i = 0; i < 1000; i++)
	{
		IDs.push(i);
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


DllExport void SetGlobalVolume(float Volume)
{
	map<int, GstreamerAudioStream*>::iterator p;

	for(p = Streams.begin(); p != Streams.end(); p++) {
		p->second->SetStreamVolumeMax(Volume);
	}
	GlobalVolume = Volume;
}

DllExport float GetGlobalVolume(){
	return GlobalVolume;
}

DllExport int GetStreamCount(void)
{
	return Streams.size();
}

DllExport void CloseAll(void)
{
  map<int, GstreamerAudioStream*>::iterator p;
  
  for(p = Streams.begin(); p != Streams.end(); p++) {
	  p->second->Close();
  }
}

DllExport int Load(const wchar_t* Media)
{
	GstreamerAudioStream *s = new GstreamerAudioStream(IDs.front());
	Streams.insert(pair<int, GstreamerAudioStream*> (IDs.front(), s));
	IDs.pop();
	int result = s->Load(Media);
	s->SetStreamVolume(1.f);
	s->SetStreamVolumeMax(GlobalVolume);
	return result;
}

DllExport int LoadPrescan(const wchar_t* Media, bool Prescan)
{
	GstreamerAudioStream *s = new GstreamerAudioStream(IDs.front());
	Streams.insert(pair<int, GstreamerAudioStream*> (IDs.front(), s));
	IDs.pop();
	int result = s->Load(Media, Prescan);
	s->SetStreamVolume(1.f);
	s->SetStreamVolumeMax(GlobalVolume);
	return result;
}

DllExport void Close(int Stream)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		delete it->second;
		IDs.push(it->first);
		Streams.erase(it);
	}
}

DllExport void Play(int Stream)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		it->second->Play();
	}
}

DllExport void PlayLoop(int Stream, bool Loop)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		it->second->Play(Loop);
	}
}

DllExport void Pause(int Stream)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		it->second->Pause();
	}
}

DllExport void Stop(int Stream)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		it->second->Stop();
	}
}

DllExport void Fade(int Stream, float TargetVolume, float Seconds)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		it->second->Fade(TargetVolume, Seconds);
	}
}

DllExport void FadeAndPause(int Stream, float TargetVolume, float Seconds)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		it->second->FadeAndPause(TargetVolume, Seconds);
	}
}

DllExport void FadeAndStop(int Stream, float TargetVolume, float Seconds)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		it->second->FadeAndStop(TargetVolume, Seconds);
	}
}

DllExport void SetStreamVolume(int Stream, float Volume)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		return it->second->SetStreamVolume(Volume);
	}
}

DllExport float GetLength(int Stream)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		return it->second->GetLength();
	}
	else return -1.0;
}

DllExport float GetPosition(int Stream)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		return it->second->GetPosition();
	}
	else return -1;
}

DllExport bool IsPlaying(int Stream)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		return it->second->IsPlaying();
	}
	else return false;
}

DllExport bool IsPaused(int Stream)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		return it->second->IsPaused();
	}
	else return false;
}

DllExport bool IsFinished(int Stream)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		return it->second->IsFinished();
	}
	else return false;
}

DllExport void SetPosition(int Stream, float Position)
{
	map<int,GstreamerAudioStream*>::iterator it = Streams.find(Stream);
	if(it != Streams.end())
	{
		return it->second->SetPosition(Position);
	}
}

DllExport void Update(void)
{
  map<int, GstreamerAudioStream*>::iterator p = Streams.begin();

  while(p != Streams.end())
  {
	  if(p->second->Closed)
	  {
		delete p->second;
		IDs.push(p->first);
		Streams.erase(p++);
	  }
	  else {
		p->second->Update();
		++p;
	  }
  }
}

void LogError(const char* msg)
{
	Log(msg);
}