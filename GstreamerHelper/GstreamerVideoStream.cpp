#include "stdafx.h"
#include "GstreamerVideoStream.h"

typedef enum {
  GST_PLAY_FLAG_VIDEO         = (1 << 0),
  GST_PLAY_FLAG_AUDIO         = (1 << 1),
  GST_PLAY_FLAG_TEXT          = (1 << 2),
  GST_PLAY_FLAG_VIS           = (1 << 3),
  GST_PLAY_FLAG_SOFT_VOLUME   = (1 << 4),
  GST_PLAY_FLAG_NATIVE_AUDIO  = (1 << 5),
  GST_PLAY_FLAG_NATIVE_VIDEO  = (1 << 6),
  GST_PLAY_FLAG_DOWNLOAD      = (1 << 7),
  GST_PLAY_FLAG_BUFFERING     = (1 << 8),
  GST_PLAY_FLAG_DEINTERLACE   = (1 << 9),
  GST_PLAY_FLAG_SOFT_COLORBALANCE = (1 << 10)
} GstPlayFlags;

GstreamerVideoStream::GstreamerVideoStream(int ID)
{
	this->ID = ID;

	Loop = false;

	Running = true;

	Paused = false;
	Finished = false;
	Closed = false;
	Copying = true;

	BufferFull = false;
	for(int i = 0; i < 5; i++)
	{
		FrameBuffer[i].Sample = NULL;
		FrameBuffer[i].Buffer = NULL;
		FrameBuffer[i].Displayed = true;
	}

	Duration = -1.0;
}


GstreamerVideoStream::~GstreamerVideoStream(void)
{
	CloseVideo();
}

int GstreamerVideoStream::LoadVideo(const wchar_t* Filename)
{
	Element = gst_element_factory_make("playbin", "playbin");
	Appsink = (GstAppSink*) gst_element_factory_make("appsink", "appsink");

	Bus = gst_element_get_bus(Element);
	if(!Element || !Appsink)
	{
		LogVideoError ("Could not create element!");
		return -1;
	} 

	g_object_set(Element, "uri", Filename, NULL);
	g_object_set(Element, "video-sink", Appsink, NULL);
	g_object_set(Element, "flags", GST_PLAY_FLAG_VIDEO, NULL); 
	g_object_set (Appsink, "emit-signals", FALSE, "sync", FALSE, NULL);

	GstCaps *caps;
	caps = gst_caps_new_simple ("video/x-raw",
		"format", G_TYPE_STRING, "BGRA", //We should use RGB for better performance here! Implement later
		NULL);

	gst_app_sink_set_caps(Appsink, caps);

	gst_app_sink_set_drop(Appsink, false);
	gst_app_sink_set_max_buffers(Appsink, 5);

	gst_element_set_state(Element, GST_STATE_PLAYING);
	//gst_element_set_state(GST_ELEMENT (Appsink), GST_STATE_PLAYING);
	gst_bus_timed_pop_filtered(Bus, -1, GST_MESSAGE_ASYNC_DONE);
	CopyThread = thread (&GstreamerVideoStream::Copy, this);

	RefreshDuration();

	return ID;
}

void GstreamerVideoStream::Copy()
{
	while(Copying)
	{
		int num = -1;
		for (int i = 0; i < 5; i++)
		{
			if(FrameBuffer[i].Displayed)
			{

				num = i;
				GstSample *sample;
				sample = gst_app_sink_pull_sample(Appsink);
				Mutex.lock();
				if(FrameBuffer[i].Buffer)
				{
					if(FrameBuffer[i].Memory.data)
						gst_buffer_unmap(FrameBuffer[i].Buffer, &FrameBuffer[i].Memory);
					gst_buffer_unref(FrameBuffer[i].Buffer);
				}
				//if(FrameBuffer[i].Sample)
					//gst_sample_unref(FrameBuffer[i].Sample);


				FrameBuffer[i].Displayed = false;
				FrameBuffer[i].Sample = sample;
				Mutex.unlock();

				//gst_element_send_event (Element, gst_event_new_step (GST_FORMAT_BUFFERS, 1, 1.0, TRUE, FALSE));
				break;
			}
		}
		if(num == -1)
			BufferFull = true;
	}
}

bool GstreamerVideoStream::CloseVideo()
{
	Copying = false;
	CopyThread.join();

	for(int i= 0; i< 5; i++)
	{
		if(FrameBuffer[i].Buffer)
		{
			if(FrameBuffer[i].Memory.data)
				gst_buffer_unmap(FrameBuffer[i].Buffer, &FrameBuffer[i].Memory);
			gst_buffer_unref(FrameBuffer[i].Buffer);
		}
		//if(FrameBuffer[i].Sample)
			//gst_sample_unref(FrameBuffer[i].Sample);
	}

	if(Element)
		gst_element_set_state(Element, GST_STATE_NULL);

	if(Appsink)
		gst_object_unref(Appsink);
	if(Bus)
		gst_object_unref(Bus);
	//if(Buffer && &Mapinfo)
		//gst_buffer_unmap(Buffer, &Mapinfo);


	if(Element)
		gst_object_unref(Element);
	return true;
}

float GstreamerVideoStream::GetVideoLength()
{
	if(Duration <= 0)
		RefreshDuration();
	return Duration;
}

struct ApplicationFrame GstreamerVideoStream::GetFrame(float Time)
{
	float VideoTime = 0.0;
	int Width = 0;
	int Height = 0;
	int Size = -1;

	Mutex.lock();
	for(int i = 0; i < 5; i++)
	{
		if(!FrameBuffer[i].Displayed)
		{
			if(FrameBuffer[i].Sample) {
				GstBuffer *Buffer = gst_sample_get_buffer(FrameBuffer[i].Sample);
				GstMapInfo Mapinfo;

				GstCaps *BufferCaps = gst_sample_get_caps (FrameBuffer[i].Sample);
				GstStructure *BufferStructure = gst_caps_get_structure (BufferCaps, 0);

				gst_buffer_map(Buffer, &Mapinfo, GST_MAP_READ);

				Size = (int) Mapinfo.size;
				gst_structure_get_int (BufferStructure, "width", &Width);
				gst_structure_get_int (BufferStructure, "height", &Height);
				VideoTime = (float) (Buffer->pts / GST_SECOND);

				ReturnFrame.data = Mapinfo.data;
				ReturnFrame.height = Height;
				ReturnFrame.width = Width;
				ReturnFrame.size = Size;
				ReturnFrame.videotime = VideoTime;

				FrameBuffer[i].Memory = Mapinfo;
				FrameBuffer[i].Buffer = Buffer;

				if(BufferCaps)
					gst_caps_unref(BufferCaps);

				if(BufferStructure)
					gst_object_unref(BufferStructure);
			}
			FrameBuffer[i].Displayed = true;
			break;
		}
	}
	Mutex.unlock();
	return ReturnFrame;
}

bool GstreamerVideoStream::Skip(float Start, float Gap)
{
	if(!gst_element_seek_simple(Element, GST_FORMAT_TIME, (GstSeekFlags)(GST_SEEK_FLAG_FLUSH | GST_SEEK_FLAG_ACCURATE), (gint64)((Start + Gap) * GST_SECOND)))
	{
		LogVideoError("Seek failed");
		return true;
	}
	return false;
}

void GstreamerVideoStream::SetVideoLoop(bool Loop)
{
	if(Loop)
		g_object_set(Element, "sync", TRUE, NULL);
	else
		g_object_set(Element, "sync", FALSE, NULL);
	this->Loop = Loop;
}

void GstreamerVideoStream::PauseVideo()
{
	if(Element)
		gst_element_set_state(Element, GST_STATE_PAUSED);
}

void GstreamerVideoStream::ResumeVideo()
{
	if(Element)
		gst_element_set_state(Element, GST_STATE_PLAYING);
}

bool GstreamerVideoStream::IsFinished()
{
	return Finished;
}

void GstreamerVideoStream::RefreshDuration()
{
	if(Element)
	{
		gint64 time;
		if(!gst_element_query_duration(Element, GST_FORMAT_TIME, &time)) {
			LogVideoError("Could not query duration");
		}
		Duration = (gfloat)((gdouble)(time/GST_SECOND));
	}
}

void GstreamerVideoStream::UpdateVideo()
{
	if(Running) {
		Message = gst_bus_pop (Bus);
   
		/* Parse message */
		if (Message != NULL) {
		  GError *err;
		  gchar *debug_info;
		  string m;
       
		  switch (GST_MESSAGE_TYPE (Message)) {
			case GST_MESSAGE_ERROR:
				gst_message_parse_error (Message, &err, &debug_info);
				m = err->message;
				LogVideoError(m.c_str());
				g_clear_error (&err);
				g_free (debug_info);
				break;
			case GST_MESSAGE_DURATION_CHANGED:
				RefreshDuration();
				break;
			case GST_MESSAGE_STATE_CHANGED:
				break;
			case GST_MESSAGE_EOS:
				if(Loop)
					Skip(0, 0);
				else
					Finished = true;
				break;
			default:
				/* We should not reach here */
				//LogError ("Unexpected message received.\n");
				break;
		  }
		  gst_message_unref (Message);
		}
	}
}