#pragma once
#include "gst/gst.h"
	using namespace std;
class GstreamerVideoStream
{

public:
	int ID;

	GstreamerVideoStream(int ID);
	~GstreamerVideoStream(void);

	int LoadVideo(const wchar_t* VideoFileName);
    bool CloseVideo();

    float GetVideoLength();
	UINT8* GetFrame(float Time, float &VideoTime);
    bool Skip(float Start, float Gap);
    void SetVideoLoop(bool Loop);
    void PauseVideo();
	void ResumeVideo();

    bool Finished();
};

