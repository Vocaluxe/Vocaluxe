#pragma once
#include <mutex>
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
	struct ApplicationFrame GetFrame(float Time);
    bool Skip(float Start, float Gap);
    void SetVideoLoop(bool Loop);
    void PauseVideo();
	void ResumeVideo();

	void UpdateVideo();

    bool IsFinished();

private:

	GMainLoop *MainLoop;
	GMainContext *Context;

	GstAppSink *Appsink;
	GstElement *Element;
	GstBus *Bus;
	GstMessage *Message;

	ApplicationFrame Frame;
	GstMapInfo Mapinfo;
	GstSample *Sample;
	GstBuffer *Buffer;
	GstCaps *BufferCaps;
	GstStructure *BufferStructure;
	mutex Mutex;

	gboolean Loop;

	gboolean Running;

	gboolean Paused;
	gboolean Finished;

	gfloat Duration;

	void RefreshDuration();

	GstFlowReturn NewBufferRecieved(GstAppSink *Sink);
	static GstFlowReturn fake_callback(GstAppSink *sink ,gpointer data);
};


