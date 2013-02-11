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
	if(!Element)
	{
		LogVideoError ("Could not create element!");
		return -1;
	} 
	g_object_set(Element, "uri", Filename, NULL);
	//g_object_set(Element, "video-sink", Appsink, NULL);
	g_object_set(Element, "flags", GST_PLAY_FLAG_VIDEO, NULL); 

	//GstCaps *caps = gst_caps_new_any();

	//gst_caps_set_value(caps, "format", (GValue*)"RGBA");
	//gst_app_sink_set_caps(Appsink, caps);

	gst_element_set_state(Element, GST_STATE_PLAYING);
	//gst_bus_timed_pop_filtered(Bus, -1, GST_MESSAGE_ASYNC_DONE);
	Paused = true;
	RefreshDuration();

	return ID;
}

bool GstreamerVideoStream::CloseVideo()
{
	if(Element)
		gst_element_set_state(Element, GST_STATE_NULL);
	if(Appsink)
		gst_object_unref(Appsink);
	if(Bus)
		gst_object_unref(Bus);
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

guint8* GstreamerVideoStream::GetFrame(float Time, float &VideoTime, int &Size, int &Width, int &Height)
{
	/*GstSample *sample;
	GstBuffer *buffer;
	GstElement *sink;
	
	sample = gst_app_sink_pull_sample(Appsink);
	buffer = gst_sample_get_buffer(sample);

	g_object_get(Element, "video-sink", &sink, NULL);

	gst_buffer_map(buffer, &Mapinfo, GST_MAP_READ);

	VideoTime = (float) (buffer->pts / GST_SECOND);
	Size = Mapinfo.size;
	return Mapinfo.data; */
	return 0;
}

bool GstreamerVideoStream::Skip(float Start, float Gap)
{
	if(!gst_element_seek_simple(Element, GST_FORMAT_TIME, (GstSeekFlags)(GST_SEEK_FLAG_FLUSH | GST_SEEK_FLAG_ACCURATE), (Start + Gap) * GST_SECOND))
	{
		LogVideoError("Seek failed");
		return true;
	}
	return false;
}

void GstreamerVideoStream::SetVideoLoop(bool Loop)
{
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