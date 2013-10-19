#include "stdafx.h"
#include "GstreamerAudioStream.h"


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

GstreamerAudioStream::GstreamerAudioStream(int id)
{
	ID = id;
	MaxVolume = 1.0;
	Volume = 1.0;
	Duration = -1;
	Loop = false;
	Running = true;

	Finished = false;

	//Fading
	FadeTimer = g_timer_new();
	FadeTime = 0.0;
    TargetVolume = 1.0;
    StartVolume = 1.0;
    CloseStreamAfterFade = false;
    PauseStreamAfterFade = false;
    Fading = false;

	Closed = false;
}


GstreamerAudioStream::~GstreamerAudioStream(void)
{
	Close();
}

// Loads a stream but does not wait for it to get initialized, 
// so querying duration or position directly after initialization might
// return strange values! To work around this use the prescan load function!
int GstreamerAudioStream::Load(const wchar_t* Media)
{
	Element = gst_element_factory_make("playbin", "playbin");

	Convert = gst_element_factory_make("audioconvert", "audioconvert");
	Audiosink = gst_element_factory_make("directsoundsink", "directsoundsink");
	SinkBin = gst_bin_new("SinkBin");
	gst_bin_add_many (GST_BIN (SinkBin), Convert, Audiosink, NULL);
	gst_element_link_many (Convert, Audiosink, NULL);
	Pad = gst_element_get_static_pad (Convert, "sink");
	GhostPad = gst_ghost_pad_new ("sink", Pad);
	gst_pad_set_active (GhostPad, TRUE);
	gst_element_add_pad (SinkBin, GhostPad);

	Bus = gst_element_get_bus(Element);
	if(!Element)
	{
		LogError ("Could not create element!");
		return -1;
	} 
	Running = true;
	gst_element_set_state(Element, GST_STATE_NULL);
	g_object_set(Element, "uri", Media, NULL);
	g_object_set(Element, "audio-sink", SinkBin, NULL);
	g_object_set(Element, "flags", GST_PLAY_FLAG_AUDIO, NULL); 

	gst_element_set_state(Element, GST_STATE_PAUSED);

	return ID;
}

//Loads a stream and waits for its initialization
int GstreamerAudioStream::Load(const wchar_t* Media, bool Prescan)
{
	Load(Media);
	if(Prescan){
		gst_bus_timed_pop_filtered(Bus, -1, GST_MESSAGE_ASYNC_DONE);
		//Workaround: Waiting for the stream to be loaded (Length>0)
		for(int i=0;i<40;i++){
			if(GetLength()>0.f)
				break;
			Sleep(5);
		}
	}
	return ID;
}

void GstreamerAudioStream::Close(void)
{
	if(Element)
	{
		gst_element_set_state(Element, GST_STATE_NULL);
		gst_object_unref(GST_OBJECT (Element));
	}

	/* without that there are no crashes any more. but i don't know if there is a memory leak if we do that.
	if(Bus)
		gst_object_unref(Bus);
	if(Message)
		gst_message_unref(Message);
	if(Convert)
		gst_object_unref(Convert);
	if(Audiosink)
		gst_object_unref(Audiosink);
	if(Pad)
		gst_object_unref(Pad);
	if(GhostPad)
		gst_object_unref(GhostPad);
	if(SinkBin)
		gst_object_unref(SinkBin);
	*/

	if(FadeTimer)
		g_timer_destroy (FadeTimer);

	Running = false;
	Closed = true;
}

void GstreamerAudioStream::Play(void)
{
	if(Element)
	{
		PauseStreamAfterFade = false;
		Running = true;
		if(gst_element_set_state(Element, GST_STATE_PLAYING) == GST_STATE_CHANGE_ASYNC)
			gst_bus_timed_pop_filtered(Bus, -1, GST_MESSAGE_ASYNC_DONE);
		SetStreamVolume(Volume * 100.0);
	}

}

void GstreamerAudioStream::Play(bool Loop)
{
	this->Loop = Loop;
	Play();
}

void GstreamerAudioStream::Pause()
{
	Running = true;
	if(Element)
		gst_element_set_state(Element, GST_STATE_PAUSED);
	SetStreamVolume(Volume * 100.0);
}

void GstreamerAudioStream::Stop()
{
	if(Element)
	{
		gst_element_set_state(Element, GST_STATE_NULL);
		gst_element_seek_simple(Element, GST_FORMAT_TIME, GST_SEEK_FLAG_FLUSH, 0);
	}
}

void GstreamerAudioStream::SetStreamVolume(float Volume)
{
	this->Volume = Volume / 100.0;
	if(Element)
	{
		g_object_set(this->Element, "volume", (gdouble)(Volume / 100.0) * MaxVolume, NULL);
	}
}

void GstreamerAudioStream::SetStreamVolumeMax(float MaxVolume)
{
	this->MaxVolume = (gdouble) (MaxVolume / 100.0);
	if(Element)
	{
		g_object_set(this->Element, "volume", Volume * this->MaxVolume, NULL);
	}
}

float GstreamerAudioStream::GetLength()
{
	if(Duration <= 0.f)
		RefreshDuration();
	return Duration;
}

void GstreamerAudioStream::RefreshDuration()
{
	if(Element)
	{
		gint64 time = -1000;
		if(gst_element_query_duration(Element, GST_FORMAT_TIME, &time))
			Duration = (gfloat)((gdouble)time/GST_SECOND);
	}
}

float GstreamerAudioStream::GetPosition()
{
	if(Element)
	{
		gint64 time = 0;
		if (gst_element_query_position(Element, GST_FORMAT_TIME, &time))
			return (float)((gdouble)time/GST_SECOND);
		else
			return -1;
	}
	else return -1;
}

void GstreamerAudioStream::SetPosition(float Position)
{
	if(Element)
	{
		//Not sure if we need GST_SEEK_FLAG_ACCURATE
		if(!gst_element_seek_simple(Element, GST_FORMAT_TIME, (GstSeekFlags)(GST_SEEK_FLAG_FLUSH | GST_SEEK_FLAG_ACCURATE), Position * GST_SECOND))
			LogError("Seek failed");
	}
}

void GstreamerAudioStream::Fade(float TargetVolume, float Seconds)
{
	Fading = true;
	g_timer_stop(FadeTimer);
    StartVolume = Volume;
    this->TargetVolume = TargetVolume / 100.0;
    FadeTime = (gdouble)Seconds;
    g_timer_start(FadeTimer);
}

void GstreamerAudioStream::FadeAndPause(float TargetVolume, float Seconds)
{
    PauseStreamAfterFade = true;

    Fade(TargetVolume, Seconds);
}

void GstreamerAudioStream::FadeAndStop(float TargetVolume, float Seconds)
{
    CloseStreamAfterFade = true;

    Fade(TargetVolume, Seconds);
}

bool GstreamerAudioStream::IsPlaying()
{
	GstStateChangeReturn ret;
	GstState current, pending;

	//This might block!!!
	ret = gst_element_get_state (Element, &current, &pending, GST_CLOCK_TIME_NONE);
	return current == GST_STATE_PLAYING && !Finished;
}

bool GstreamerAudioStream::IsPaused()
{
	GstStateChangeReturn ret;
	GstState current, pending;

	//This might block!!!
	ret = gst_element_get_state (Element, &current, &pending, GST_CLOCK_TIME_NONE);
	return current == GST_STATE_PAUSED && !Finished;
}

bool GstreamerAudioStream::IsFinished()
{
	return Finished;
}

void GstreamerAudioStream::UpdateVolume()
{
	if (Fading)
	{
		if (g_timer_elapsed(FadeTimer, NULL) < FadeTime) {
			gdouble vol = (StartVolume + (TargetVolume - StartVolume) * (g_timer_elapsed(FadeTimer, NULL) / FadeTime));
			SetStreamVolume(100.0 * vol);
		}
		else
		{
			SetStreamVolume (TargetVolume * 100.0);
			g_timer_stop(FadeTimer);
			Fading = false;

			if (CloseStreamAfterFade)
			{
				Closed = true;
			}

			if (PauseStreamAfterFade)
				Pause();
		}
	}
}

void GstreamerAudioStream::Update()
{
	if(Running) {
		UpdateVolume();
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
				LogError(m.c_str());
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
					SetPosition(0);
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

