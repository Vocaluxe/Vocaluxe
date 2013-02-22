#pragma once
#pragma warning(disable: 4244) // possible loss of data
#pragma warning(disable: 4267) // possible loss of data
#pragma warning(disable: 4800)

#include <string>
#include "gst/gst.h"
#include "GstreamerAudio.h"

using namespace std;

class GstreamerAudioStream
{
public:
	int ID;
	bool Closed;

	GstreamerAudioStream(int ID);
	~GstreamerAudioStream(void);

	int Load(const wchar_t* Media);
	int Load(const wchar_t* Media, bool Prescan);
	void Close();

	void Play();
	void Play(bool Loop);
	void Pause();
	void Stop();

	void Fade(float TargetVolume, float Seconds);
	void FadeAndPause(float TargetVolume, float Seconds);
	void FadeAndStop(float TargetVolume, float Seconds);
	void SetStreamVolume(float Volume);
	void SetStreamVolumeMax(float Volume);

	float GetLength();
	float GetPosition();
	void SetPosition(float Position);

	bool IsPlaying();
	bool IsPaused();
	bool IsFinished();

	void Update();
private:
	GstElement *Element, *Audiosink, *Convert, *SinkBin;
	GstPad *Pad, *GhostPad;
	GstBus *Bus;
	GstMessage *Message;

	gboolean Running;
	gboolean Loop;

	gboolean Finished;

	gdouble MaxVolume;
	gdouble Volume;

	gfloat Duration;

	GTimer *FadeTimer;

    gdouble FadeTime;
    gdouble TargetVolume;
    gdouble StartVolume;
    gboolean CloseStreamAfterFade;
    gboolean PauseStreamAfterFade;
    gboolean Fading;

	void RefreshDuration();
	void UpdateVolume();
};

