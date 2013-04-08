/*
    This file is part of Acinerella.

    Acinerella is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Acinerella is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Acinerella.  If not, see <http://www.gnu.org/licenses/>.
*/

#include <stdlib.h>
#include <stdbool.h>
#include "acinerella.h"
#include <libavformat/avformat.h>
#include <libavformat/avio.h>
#include <libavcodec/avcodec.h>
#include <libavutil/avutil.h>
#include <libswscale/swscale.h>
#include <libswresample/swresample.h>
#include <string.h>

#define AUDIO_BUFFER_BASE_SIZE AVCODEC_MAX_AUDIO_FRAME_SIZE

#define CODEC_TYPE_VIDEO AVMEDIA_TYPE_VIDEO
#define CODEC_TYPE_AUDIO AVMEDIA_TYPE_AUDIO


//This struct represents one Acinerella video object.
//It contains data needed by FFMpeg.

#define AC_BUFSIZE 1024*64

struct _ac_data {
  ac_instance instance;
  AVFormatContext *pFormatCtx;
  void* buffer; 
};

typedef struct _ac_data ac_data;
typedef ac_data* lp_ac_data;

struct _ac_decoder_data {
  ac_decoder decoder;
};

typedef struct _ac_decoder_data ac_decoder_data;
typedef ac_decoder_data* lp_ac_decoder_data;

struct _ac_video_decoder {
  ac_decoder decoder;
  AVCodec *pCodec;
  AVCodecContext *pCodecCtx;
  AVFrame *pFrame;
  AVFrame *pFrameRGB; 
  struct SwsContext *pSwsCtx;  
};

typedef struct _ac_video_decoder ac_video_decoder;
typedef ac_video_decoder* lp_ac_video_decoder;

struct _ac_audio_decoder {
  ac_decoder decoder;
  int max_buffer_size;
  AVCodec *pCodec;
  AVCodecContext *pCodecCtx;
};

typedef struct _ac_audio_decoder ac_audio_decoder;
typedef ac_audio_decoder* lp_ac_audio_decoder;

struct _ac_package_data {
  ac_package package;
  AVPacket ffpackage;
  int pts;
};

typedef struct _ac_package_data ac_package_data;
typedef ac_package_data* lp_ac_package_data;

//
//--- Initialization and Stream opening---
//

void init_info(lp_ac_file_info info)
{
  info->duration = -1;
}

int av_initialized = 0;
void ac_init_ffmpeg()
{
  if(!av_initialized)
  {
    avcodec_register_all();
    av_register_all();
    av_initialized = 1;
  }
}

lp_ac_instance CALL_CONVT ac_init(void) { 
  ac_init_ffmpeg();
  
  //Allocate a new instance of the videoplayer data and return it
  lp_ac_data ptmp;  
  ptmp = (lp_ac_data)av_malloc(sizeof(ac_data));
  
  //Initialize the created structure
  memset(ptmp, 0, sizeof(ac_data));
  
  ptmp->instance.opened = 0;
  ptmp->instance.stream_count = 0;
  ptmp->instance.output_format = AC_OUTPUT_RGBA32;
  init_info(&(ptmp->instance.info));
  return (lp_ac_instance)ptmp;  
}

void CALL_CONVT ac_free(lp_ac_instance pacInstance) {
  //Close the decoder. If it is already closed, this won't be a problem as ac_close checks the streams state
  ac_close(pacInstance);
  
  if (pacInstance != NULL) {
    av_free((lp_ac_data)pacInstance);
  }
}

int CALL_CONVT ac_open(lp_ac_instance pacInstance, const char* filename)
{ 
	pacInstance->opened = 0; 

	/* open input file, and allocate format context */
	int ret = 0;
	if ((ret = avformat_open_input(&((lp_ac_data)pacInstance)->pFormatCtx, filename, NULL, NULL)) < 0)
	{
		return ret;
	}

    /* retrieve stream information */
    if ((ret = avformat_find_stream_info(((lp_ac_data)pacInstance)->pFormatCtx, NULL)) < 0)
	{
		return ret;
	}
	else
		pacInstance->info.duration = ((lp_ac_data)pacInstance)->pFormatCtx->duration * 1000 / AV_TIME_BASE;

	//Set some information in the instance variable 
	pacInstance->stream_count = ((lp_ac_data)pacInstance)->pFormatCtx->nb_streams;
	pacInstance->opened = pacInstance->stream_count > 0;  

	return 0;
}

void CALL_CONVT ac_close(lp_ac_instance pacInstance) {
  if (pacInstance->opened) {    
    //Close the opened file   
    avformat_close_input(&(((lp_ac_data)(pacInstance))->pFormatCtx));
    pacInstance->opened = 0;

    //If the seek proc has not been specified, the input buffer is not automatically
    //freed, as ffmpeg didn't get the original pointer to the buffer
    if (((lp_ac_data)(pacInstance))->buffer) {
      av_free(((lp_ac_data)(pacInstance))->buffer);
    }

  }
}

void CALL_CONVT ac_get_stream_info(lp_ac_instance pacInstance, int nb, lp_ac_stream_info info) {
  if (!(pacInstance->opened)) { 
    return;
  }
  
  switch (((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->codec->codec_type) {
    case CODEC_TYPE_VIDEO:
      //Set stream type to "VIDEO"
      info->stream_type = AC_STREAM_TYPE_VIDEO;
      
      //Store more information about the video stream
      info->video_info.frame_width = 
        ((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->codec->width;
      info->video_info.frame_height = 
        ((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->codec->height;
	
	  double pixel_aspect_num = ((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->codec->sample_aspect_ratio.num;
	  double pixel_aspect_den = ((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->codec->sample_aspect_ratio.den;
		
	  //Sometime "pixel aspect" may be zero or have other invalid values. Correct this.
	  if (pixel_aspect_num <= 0.0 || pixel_aspect_den <= 0.0)
        info->video_info.pixel_aspect = 1.0;
      else
	    info->video_info.pixel_aspect = pixel_aspect_num / pixel_aspect_den;  
      
      info->video_info.frames_per_second =
        (double)((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->r_frame_rate.num /
        (double)((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->r_frame_rate.den;
    break;
    case CODEC_TYPE_AUDIO:
      //Set stream type to "AUDIO"
      info->stream_type = AC_STREAM_TYPE_AUDIO;
      
      //Store more information about the video stream
      info->audio_info.samples_per_second =
        ((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->codec->sample_rate;        
      info->audio_info.channel_count = 2;
        //((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->codec->channels;
		
      // Set bit depth (its always 16Bit because of the conversion!     
	  info->audio_info.bit_depth = 16;
        
    break;
    default:
      info->stream_type = AC_STREAM_TYPE_UNKNOWN;
  }
}

//
//---Package management---
//

lp_ac_package CALL_CONVT ac_read_package(lp_ac_instance pacInstance) {
  //Try to read package
  AVPacket Package;  
  if (av_read_frame(((lp_ac_data)(pacInstance))->pFormatCtx, &Package) >= 0) {
    //Reserve memory
    lp_ac_package_data pTmp = (lp_ac_package_data)(av_malloc(sizeof(ac_package_data)));
	memset(pTmp, 0, sizeof(ac_package_data));
    
    //Set package data
    pTmp->package.stream_index = Package.stream_index;
    pTmp->ffpackage = Package;
	
	if (Package.dts != AV_NOPTS_VALUE) {
		pTmp->pts = Package.dts;
	}
    
    return (lp_ac_package)(pTmp);
  } else {
    return NULL;
  }
}

//Frees the currently loaded package
void CALL_CONVT ac_free_package(lp_ac_package pPackage)
{
  //Free the packet
  if (pPackage != NULL) {        
    AVPacket* pkt = &((lp_ac_package_data)pPackage)->ffpackage;
    if (pkt) {
      if (pkt->destruct) pkt->destruct(pkt);
      pkt->data = NULL; pkt->size = 0;
    }     
    av_free((lp_ac_package_data)pPackage);
  }
}

//
//--- Decoder management ---
//

enum PixelFormat convert_pix_format(ac_output_format fmt) {
  switch (fmt) {
    case AC_OUTPUT_RGB24: return PIX_FMT_RGB24;
    case AC_OUTPUT_BGR24: return PIX_FMT_BGR24;
    case AC_OUTPUT_RGBA32: return PIX_FMT_RGB32;
    case AC_OUTPUT_BGRA32: return PIX_FMT_BGR32;        
  }
  return PIX_FMT_RGB32;
}

lp_ac_decoder CALL_CONVT ac_create_video_decoder(lp_ac_instance pacInstance) {
	//Allocate memory for a new decoder instance
	lp_ac_video_decoder pDecoder;  
	pDecoder = (lp_ac_video_decoder)(av_malloc(sizeof(ac_video_decoder)));
	memset(pDecoder, 0, sizeof(ac_video_decoder));

	int nb = av_find_best_stream(((lp_ac_data)pacInstance)->pFormatCtx, AVMEDIA_TYPE_VIDEO, -1, -1, NULL, 0);
    if (nb < 0)
        return NULL;
	
	ac_stream_info info;
	ac_get_stream_info(pacInstance, nb, &info);
	
	//Set a few properties
	pDecoder->decoder.pacInstance = pacInstance;
	pDecoder->decoder.type = AC_DECODER_TYPE_VIDEO;
	pDecoder->decoder.stream_index = nb;
	pDecoder->decoder.video_clock = 0;
	pDecoder->pCodecCtx = ((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->codec;
	pDecoder->decoder.stream_info = info;  
	pDecoder->pCodecCtx->thread_count = 1; 	//this is for HT CPUs, it should be
											// tested if there is a better solution
	
	//Find correspondenting codec
	if (!(pDecoder->pCodec = avcodec_find_decoder(pDecoder->pCodecCtx->codec_id)))
		return NULL; //Codec could not have been found

	//Open codec
	if (avcodec_open2(pDecoder->pCodecCtx, pDecoder->pCodec, NULL) < 0)
		return NULL; //Codec could not have been opened

	//Reserve frame variables
	pDecoder->pFrame = avcodec_alloc_frame();
	pDecoder->pFrameRGB = avcodec_alloc_frame();

	pDecoder->pSwsCtx = NULL;

	//Reserve buffer memory
	pDecoder->decoder.buffer_size = avpicture_get_size(convert_pix_format(pacInstance->output_format), 
	pDecoder->pCodecCtx->width, pDecoder->pCodecCtx->height);
	pDecoder->decoder.pBuffer = (uint8_t*)av_malloc(pDecoder->decoder.buffer_size);

	//Link decoder to buffer
	avpicture_fill(
	(AVPicture*)(pDecoder->pFrameRGB), 
	pDecoder->decoder.pBuffer, convert_pix_format(pacInstance->output_format),
	pDecoder->pCodecCtx->width, pDecoder->pCodecCtx->height);

	return (void*)pDecoder;
}

lp_ac_decoder CALL_CONVT ac_create_audio_decoder(lp_ac_instance pacInstance)
{
	//Allocate memory for a new decoder instance
	lp_ac_audio_decoder pDecoder;
	pDecoder = (lp_ac_audio_decoder)(av_malloc(sizeof(ac_audio_decoder)));
	memset(pDecoder, 0, sizeof(ac_audio_decoder));

	int nb = av_find_best_stream(((lp_ac_data)pacInstance)->pFormatCtx, AVMEDIA_TYPE_AUDIO, -1, -1, NULL, 0);
    if (nb < 0)
        return NULL;
	
	ac_stream_info info;
	ac_get_stream_info(pacInstance, nb, &info);
  
	//Set a few properties
	pDecoder->decoder.pacInstance = pacInstance;
	pDecoder->decoder.type = AC_DECODER_TYPE_AUDIO;
	pDecoder->decoder.stream_index = nb;
	pDecoder->decoder.stream_info = info;
	pDecoder->decoder.video_clock = 0;
											
	//Temporary store codec context pointer
	AVCodecContext *pCodecCtx = ((lp_ac_data)pacInstance)->pFormatCtx->streams[nb]->codec;
	pDecoder->pCodecCtx = pCodecCtx;  
	//pDecoder->pCodecCtx->thread_count = 1; 	//this is for HT CPUs, it should be
											// tested if there is a better solution
											
	//Find correspondenting codec
	if (!(pDecoder->pCodec = avcodec_find_decoder(pCodecCtx->codec_id)))
		return NULL;

	//Open codec
	if (avcodec_open2(pCodecCtx, pDecoder->pCodec, NULL) < 0)
		return NULL;
		
	//Initialize the buffers
	pDecoder->decoder.pBuffer = av_malloc(AUDIO_BUFFER_BASE_SIZE);
	pDecoder->decoder.buffer_size = 0;
	pDecoder->max_buffer_size = AUDIO_BUFFER_BASE_SIZE;
	
	return (void*)pDecoder;
}

double ac_sync_video(lp_ac_package pPackage, lp_ac_decoder pDec, AVFrame *src_frame, double pts){
  double frame_delay;
  
  if(pts != 0){
    pDec->video_clock = pts;
  } else {
    pts = pDec->video_clock;
  }
  
  frame_delay = av_q2d(((lp_ac_data)pDec->pacInstance)->pFormatCtx->streams[pPackage->stream_index]->time_base);
  frame_delay += src_frame->repeat_pict * (frame_delay * 0.5);
  pDec->video_clock += frame_delay;
  return pts;
}

int ac_decode_video_package(lp_ac_package pPackage, lp_ac_video_decoder pDecoder, lp_ac_decoder pDec)
{
  int finished = 0;
  double pts = 0;
  int len = 0;
  
  AVPacket pkt_tmp = ((lp_ac_package_data)pPackage)->ffpackage;
  
  while (pkt_tmp.size > 0) {
    len = avcodec_decode_video2(
	  pDecoder->pCodecCtx, pDecoder->pFrame,
	  &finished, &pkt_tmp);
            
	if (len < 0) {
	  return 0;
    }

	pkt_tmp.size -= len;
    pkt_tmp.data += len;
  }
    
  if (finished != 0) {
    pDecoder->pSwsCtx = sws_getCachedContext(pDecoder->pSwsCtx,
        pDecoder->pCodecCtx->width, pDecoder->pCodecCtx->height, pDecoder->pCodecCtx->pix_fmt,
        pDecoder->pCodecCtx->width, pDecoder->pCodecCtx->height, convert_pix_format(pDecoder->decoder.pacInstance->output_format),
                                  SWS_FAST_BILINEAR, NULL, NULL, NULL);
                                  
      sws_scale(
        pDecoder->pSwsCtx,
        (const uint8_t* const*)(pDecoder->pFrame->data),
        pDecoder->pFrame->linesize,
        0, //?
        pDecoder->pCodecCtx->height, 
        pDecoder->pFrameRGB->data, 
        pDecoder->pFrameRGB->linesize);
		
	
    if(pkt_tmp.dts == AV_NOPTS_VALUE &&
	  *(uint64_t*)pDecoder->pFrame->opaque != AV_NOPTS_VALUE ){
	  pts = *(uint64_t*)pDecoder->pFrame->opaque;
    } else if(pkt_tmp.dts != AV_NOPTS_VALUE){
      pts = pkt_tmp.dts;
    }
	
	if(((lp_ac_data)pDec->pacInstance)->pFormatCtx->streams[pPackage->stream_index]->start_time != AV_NOPTS_VALUE){
      pts -= ((lp_ac_data)pDec->pacInstance)->pFormatCtx->streams[pPackage->stream_index]->start_time;
	}

    pts *= av_q2d(((lp_ac_data)pDec->pacInstance)->pFormatCtx->streams[pPackage->stream_index]->time_base);
	
    pts = ac_sync_video(pPackage, pDec, pDecoder->pFrame, pts);
	pDec->timecode = pts;
		   
    return 1;
  }
  return 0;
}

int alloc_samples_array_and_data(uint8_t ***data, int *linesize, int nb_channels, int nb_samples, enum AVSampleFormat sample_fmt, int align)
{
    int nb_planes = av_sample_fmt_is_planar(sample_fmt) ? nb_channels : 1;

    *data = av_malloc(sizeof(*data) * nb_planes);
    if (!*data)
        return AVERROR(ENOMEM);
    return av_samples_alloc(*data, linesize, nb_channels,
                            nb_samples, sample_fmt, align);
}

int ac_decode_audio_package(lp_ac_package pPackage, lp_ac_audio_decoder pDecoder, lp_ac_decoder pDec) {
  double pts;
  //Variables describing the destination buffer
  int dest_buffer_pos = pDecoder->decoder.buffer_size;
  
  //Make a copy of the package read by avformat, so that we can move the data pointers around
  AVPacket pkt_tmp = ((lp_ac_package_data)pPackage)->ffpackage;
  AVFrame *decoded_frame = NULL;

  while (pkt_tmp.size > 0) {  
  
	if (!decoded_frame) {
		if (!(decoded_frame = avcodec_alloc_frame())) {
            return 0;
        }
    } else
        avcodec_get_frame_defaults(decoded_frame);
			
	int got_frame = 0;
	
	int len1 = avcodec_decode_audio4(pDecoder->pCodecCtx, decoded_frame, &got_frame, &pkt_tmp);

    //If an error occured, skip the frame
    if (len1 < 0){
      return 0;    
    }
	    
    //Increment the source buffer pointers     
	pkt_tmp.size -= len1;
	pkt_tmp.data += len1;
    
	if (got_frame){
		struct SwrContext *swr_ctx;
		int dst_nb_samples = 0, max_dst_nb_samples = 0, dst_linesize = 0;
		uint8_t **dst_data = NULL;
		int ret = 0;
		int dst_sample_rate = (pDecoder->pCodecCtx)->sample_rate;
		
		swr_ctx = swr_alloc();
	  
		/* set options */
		av_opt_set_int(swr_ctx, "in_channel_layout",    (pDecoder->pCodecCtx)->channel_layout, 0);
		av_opt_set_int(swr_ctx, "in_sample_rate",       (pDecoder->pCodecCtx)->sample_rate, 0);
		av_opt_set_sample_fmt(swr_ctx, "in_sample_fmt", (pDecoder->pCodecCtx)->sample_fmt, 0);

		av_opt_set_int(swr_ctx, "out_channel_layout",    AV_CH_LAYOUT_STEREO, 0);
		av_opt_set_int(swr_ctx, "out_sample_rate",       dst_sample_rate, 0);
		av_opt_set_sample_fmt(swr_ctx, "out_sample_fmt", AV_SAMPLE_FMT_S16, 0);
		
		if (swr_init(swr_ctx) < 0)
			return 0;

		/* compute the number of converted samples: buffering is avoided
		* ensuring that the output buffer will contain at least all the
		* converted input samples */
		max_dst_nb_samples = dst_nb_samples = av_rescale_rnd(
			decoded_frame->nb_samples, dst_sample_rate, (pDecoder->pCodecCtx)->sample_rate, AV_ROUND_UP);
		
		if (alloc_samples_array_and_data(&dst_data, &dst_linesize, 2, dst_nb_samples, AV_SAMPLE_FMT_S16, 0) < 0)
			return 0;		
			
		/* compute destination number of samples */
        dst_nb_samples = av_rescale_rnd(swr_get_delay(swr_ctx, (pDecoder->pCodecCtx)->sample_rate) +
                                        decoded_frame->nb_samples, dst_sample_rate, (pDecoder->pCodecCtx)->sample_rate, AV_ROUND_UP);
								
        if (dst_nb_samples > max_dst_nb_samples) {
            av_free(dst_data[0]);
            ret = av_samples_alloc(dst_data, &dst_linesize, 2,
                                   dst_nb_samples, AV_SAMPLE_FMT_S16, 1);
            if (ret < 0)
                return 0;
            max_dst_nb_samples = dst_nb_samples;
        }	
						
		if ((ret = swr_convert(swr_ctx, dst_data, dst_nb_samples, (const uint8_t **)decoded_frame->data, decoded_frame->nb_samples)) < 0)
			return 0;
		
		int data_size = av_samples_get_buffer_size(&dst_linesize, 2, ret, AV_SAMPLE_FMT_S16, 1);
											   
		//Reserve enough memory for coping the result data
		if (dest_buffer_pos + data_size > pDecoder->max_buffer_size) {
			pDecoder->decoder.pBuffer = av_realloc(pDecoder->decoder.pBuffer, dest_buffer_pos + data_size);
			pDecoder->max_buffer_size = dest_buffer_pos + data_size;
		}
		
		memcpy(pDecoder->decoder.pBuffer + dest_buffer_pos, dst_data[0], data_size);
		
		//Increment the destination buffer pointers, copy the result to the output buffer
		dest_buffer_pos += data_size;
		pDecoder->decoder.buffer_size += data_size;
		
		pts=0;

		if(((lp_ac_package_data)pPackage)->ffpackage.dts != AV_NOPTS_VALUE){
			pts = ((lp_ac_package_data)pPackage)->ffpackage.dts * av_q2d(((lp_ac_data)pDec->pacInstance)->pFormatCtx->streams[pPackage->stream_index]->time_base);
	  
			pDec->video_clock = pts;
		} else {
			pts = pDec->video_clock;
		}
	  
		double bytes_per_second = 4 * pDec->stream_info.audio_info.samples_per_second;
		if (bytes_per_second > 0)
			pDec->video_clock += data_size / bytes_per_second;
		
		pDec->timecode = pts;
		
		if (dst_data)
			av_freep(&dst_data[0]);
		av_freep(&dst_data);

		swr_free(&swr_ctx);
		return 1;
    }	  
  }
  
  return 0;
}

int CALL_CONVT ac_decode_package(lp_ac_package pPackage, lp_ac_decoder pDecoder)
{
	if (pDecoder->type == AC_DECODER_TYPE_AUDIO)
		return ac_decode_audio_package(pPackage, (lp_ac_audio_decoder)pDecoder, pDecoder);
	else if (pDecoder->type == AC_DECODER_TYPE_VIDEO)
		return ac_decode_video_package(pPackage, (lp_ac_video_decoder)pDecoder, pDecoder);

	return 0;
}

int CALL_CONVT ac_drop_decode_package(lp_ac_package pPackage, lp_ac_decoder pDecoder)
{
  if (pDecoder->type == AC_DECODER_TYPE_VIDEO) {
    return ac_drop_decode_video_package(pPackage, (lp_ac_video_decoder)pDecoder, pDecoder);
  }
  return 0;
}

int ac_drop_decode_video_package(lp_ac_package pPackage, lp_ac_video_decoder pDecoder, lp_ac_decoder pDec) {
  int finished;
  double pts;
  int len = 0;

  AVPacket pkt_tmp = ((lp_ac_package_data)pPackage)->ffpackage;
  
  while (pkt_tmp.size > 0) {
    len = avcodec_decode_video2(
	  pDecoder->pCodecCtx, pDecoder->pFrame,
	  &finished, &pkt_tmp);
            
	if (len < 0) {
	  return 0;
    }

	pkt_tmp.size -= len;
    pkt_tmp.data += len;
  }
  
  if (finished) {
	pts=0;
	
    if(((lp_ac_package_data)pPackage)->ffpackage.dts == AV_NOPTS_VALUE &&
	  *(uint64_t*)pDecoder->pFrame->opaque != AV_NOPTS_VALUE ){
	  pts = *(uint64_t*)pDecoder->pFrame->opaque;
    } else if(((lp_ac_package_data)pPackage)->ffpackage.dts != AV_NOPTS_VALUE){
      pts = ((lp_ac_package_data)pPackage)->ffpackage.dts;
    } else {
	  pts = 0;
    }
	
	if(((lp_ac_data)pDec->pacInstance)->pFormatCtx->streams[pPackage->stream_index]->start_time != AV_NOPTS_VALUE){
      pts -= ((lp_ac_data)pDec->pacInstance)->pFormatCtx->streams[pPackage->stream_index]->start_time;
	}

    pts *= av_q2d(((lp_ac_data)pDec->pacInstance)->pFormatCtx->streams[pPackage->stream_index]->time_base);
	
    pts = ac_sync_video(pPackage, pDec, pDecoder->pFrame, pts);
	pDec->timecode = pts;

    return 1;
  }
  return 0;
}

int CALL_CONVT ac_get_audio_frame(lp_ac_instance pacInstance, lp_ac_decoder pDecoder)
{
	lp_ac_package pPackage = ac_read_package(pacInstance);
	((lp_ac_audio_decoder)pDecoder)->decoder.buffer_size = 0;
	
	int done = 0;
	while(done == 0 && pPackage != NULL){		
		if (((lp_ac_package_data)pPackage)->package.stream_index == pDecoder->stream_index)
		{
			done = ac_decode_package(pPackage, pDecoder);
			ac_free_package(pPackage);
				
			if (done == 0)
				pPackage = ac_read_package(pacInstance);
		} else {
			ac_free_package(pPackage);
			pPackage = ac_read_package(pacInstance);
		}
	}
	
	if (done == 0)
		return 0;
	else
		return 1;
}

int CALL_CONVT ac_get_frame(lp_ac_instance pacInstance, lp_ac_decoder pDecoder)
{
	lp_ac_package pPackage = ac_read_package(pacInstance);
	int done = 0;
	int pcount = 0;
	while(done == 0 && pPackage != NULL){		
		if (((lp_ac_package_data)pPackage)->package.stream_index == pDecoder->stream_index)
		{
			done = ac_decode_package(pPackage, pDecoder);
			ac_free_package(pPackage);
			pcount++;
			if (done == 0)
				pPackage = ac_read_package(pacInstance);
		} else
		{
			ac_free_package(pPackage);
			pPackage = ac_read_package(pacInstance);
		}
	}

	if (done == 0)
		return 0;
	else
		return pcount;
}

int CALL_CONVT ac_skip_frames(lp_ac_instance pacInstance, lp_ac_decoder pDecoder, int num)
{
	lp_ac_package pPackage = ac_read_package(pacInstance);
	
	int done = 0;
	int i;
	for(i=0; i<num; i++)
	{
		if (i>0)
			pPackage = ac_read_package(pacInstance);
			
		done = 0;
		while(done == 0 && pPackage != NULL){		
			if (((lp_ac_package_data)pPackage)->package.stream_index == pDecoder->stream_index)
			{
				done = ac_drop_decode_package(pPackage, pDecoder);
				ac_free_package(pPackage);
				
				if (done == 0)
					pPackage = ac_read_package(pacInstance);
			} else 
			{
				ac_free_package(pPackage);
				pPackage = ac_read_package(pacInstance);
			}
		}
		
		if (done == 0)
			return 0;
	}
		
	if (done == 0)
		return 0;
	else 
		return 1;
}

//Seek function
int CALL_CONVT ac_seek(lp_ac_decoder pDecoder, int dir, int64_t target_pos)
{
	AVRational timebase = ((lp_ac_data)pDecoder->pacInstance)->pFormatCtx->streams[pDecoder->stream_index]->time_base;
	int flags = dir < 0 ? AVSEEK_FLAG_BACKWARD : 0;    
	int64_t pos = av_rescale(target_pos, AV_TIME_BASE, 1000);
	pDecoder->timecode = target_pos / 1000.0;
	pDecoder->video_clock = target_pos / 1000.0;
  
	if (av_seek_frame(((lp_ac_data)pDecoder->pacInstance)->pFormatCtx, pDecoder->stream_index, 
			av_rescale_q(pos, AV_TIME_BASE_Q, timebase), flags) >= 0)
	{
	
		if (pDecoder->type == AC_DECODER_TYPE_AUDIO)
		{
			if (((lp_ac_audio_decoder)pDecoder)->pCodecCtx->codec->flush != NULL)
				avcodec_flush_buffers(((lp_ac_audio_decoder)pDecoder)->pCodecCtx);
		}
		
		if (pDecoder->type == AC_DECODER_TYPE_VIDEO)
		{
			if (((lp_ac_video_decoder)pDecoder)->pCodecCtx->codec->flush != NULL)
				avcodec_flush_buffers(((lp_ac_video_decoder)pDecoder)->pCodecCtx);
		}
		return 1;
	}
	return 0;  
}

//Free video decoder
void ac_free_video_decoder(lp_ac_video_decoder pDecoder)
{  
	av_free(pDecoder->pFrame);
	av_free(pDecoder->pFrameRGB); 

	if (pDecoder->pSwsCtx != NULL)
		sws_freeContext(pDecoder->pSwsCtx);

	avcodec_close(pDecoder->pCodecCtx);

	//Free reserved memory for the buffer
	av_free(pDecoder->decoder.pBuffer);

	//Free reserved memory for decoder record
	av_free(pDecoder);
}

//Free video decoder
void ac_free_audio_decoder(lp_ac_audio_decoder pDecoder)
{
	avcodec_close(pDecoder->pCodecCtx);

	//Free reserved memory for the buffer
	av_free(pDecoder->decoder.pBuffer);

	//Free reserved memory for decoder record
	av_free(pDecoder);
}

void CALL_CONVT ac_free_decoder(lp_ac_decoder pDecoder)
{
	if (pDecoder->type == AC_DECODER_TYPE_VIDEO)
		ac_free_video_decoder((lp_ac_video_decoder)pDecoder);
	else if (pDecoder->type == AC_DECODER_TYPE_AUDIO)
		ac_free_audio_decoder((lp_ac_audio_decoder)pDecoder);  
}
