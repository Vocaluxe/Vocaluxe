#pragma once
#include <mutex>
#include <thread>
#include "gst/gst.h"
#include "GstreamerVideo.h"
#include "gst\app\gstappsink.h"
#include "VocaluxeClock.h"

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
	struct BufferFrame {
		GstSample *Sample;
		GstBuffer *Buffer;
		GstMapInfo Memory;
		gboolean Displayed;
	};

	thread CopyThread;

	GstAppSink *Appsink;
	GstElement *Element;
	GstBus *Bus;
	GstMessage *Message;

	BufferFrame FrameBuffer[5];
	gboolean BufferFull;
	mutex Mutex;

	gboolean Loop;

	gboolean Running;
	volatile gboolean Copying;

	gboolean Paused;
	bool Finished;

	gfloat Duration;

	ApplicationFrame ReturnFrame;

	void RefreshDuration();

	void Copy();
};


