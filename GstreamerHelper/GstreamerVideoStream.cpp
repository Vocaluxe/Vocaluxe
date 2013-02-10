#include "stdafx.h"
#include "GstreamerVideoStream.h"


GstreamerVideoStream::GstreamerVideoStream(int ID)
{
	this->ID = ID;
}


GstreamerVideoStream::~GstreamerVideoStream(void)
{
}

int GstreamerVideoStream::LoadVideo(const wchar_t* Filename)
{
	return ID;
}


bool GstreamerVideoStream::CloseVideo()
{
	return true;
}

float GstreamerVideoStream::GetVideoLength()
{
	return 0.0;
}

UINT8* GstreamerVideoStream::GetFrame(float Time, float &VideoTime)
{
	return 0;
}

bool GstreamerVideoStream::Skip(float Start, float Gap)
{
	return true;
}

void GstreamerVideoStream::SetVideoLoop(bool Loop)
{
}

void GstreamerVideoStream::PauseVideo()
{
}

void GstreamerVideoStream::ResumeVideo()
{
}

bool GstreamerVideoStream::Finished()
{
	return true;
}
