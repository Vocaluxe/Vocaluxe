#pragma once
#include "gst/gst.h"
#include "GstreamerVideo.h"
#include "gst\app\gstappsink.h"

	using namespace std;
class GstreamerVideoStream
{

public:
	int ID;
	bool Closed;

	GstreamerVideoStream(int ID);
	~GstreamerVideoStream(void);

	int LoadVideo(const wchar_t* VideoFileName);
    bool CloseVideo();

    float GetVideoLength();
	guint8* GetFrame(float Time, float &VideoTime, int &Size, int &Width, int &Height);
    bool Skip(float Start, float Gap);
    void SetVideoLoop(bool Loop);
    void PauseVideo();
	void ResumeVideo();

	void UpdateVideo();

    bool IsFinished();

private:
	GstAppSink *Appsink;
	GstElement *Element;
	GstBus *Bus;
	GstMessage *Message;
	GstMapInfo Mapinfo;

	gboolean Loop;

	gboolean Running;

	gboolean Paused;
	gboolean Finished;

	gfloat Duration;

	void RefreshDuration();
};


