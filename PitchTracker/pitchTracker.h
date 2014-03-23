#pragma once

#ifndef dywapitchtrack__H
#define dywapitchtrack__H

#define DllExport extern "C" __declspec(dllexport)

DllExport void Init(double baseToneFrequency, int minHalfTone, int maxHalfTone);
DllExport void DeInit();
DllExport int GetTone(double *samples, int sampleCt, float *weights);

typedef void (__stdcall * LogCallback)(const char* text);
static LogCallback Log;
DllExport void SetLogCallback(LogCallback Callback);

#endif

