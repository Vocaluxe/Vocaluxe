/**********************************************************************

  FFT.cpp

  Dominic Mazzoni

  September 2000

*******************************************************************//*!

\file FFT.cpp
\brief Fast Fourier Transform routines.

  This file contains a few FFT routines, including a real-FFT
  routine that is almost twice as fast as a normal complex FFT,
  and a power spectrum routine when you know you don't care
  about phase information.

  Some of this code was based on a free implementation of an FFT
  by Don Cross, available on the web at:

    http://www.intersrv.com/~dcross/fft.html

  The basic algorithm for his code was based on Numerican Recipes
  in Fortran.  I optimized his code further by reducing array
  accesses, caching the bit reversal table, and eliminating
  float-to-double conversions, and I added the routines to
  calculate a real FFT and a real power spectrum.

*//*******************************************************************/

#include <stdlib.h>
#include <stdio.h>
#include <math.h>

#include "FFT.h"

#include "RealFFTf.h"

void DeinitFFT()
{
   // Deallocate any unused RealFFTf tables
   CleanupFFT();
}

/*
 * Real Fast Fourier Transform
 *
 */
void RealFFT(int NumSamples, float *RealIn, float *RealOut, float *ImagOut)
{
   // Remap to RealFFTf() function
   int i;
   HFFT hFFT = GetFFT(NumSamples);
   float *pFFT = new float[NumSamples];
   // Copy the data into the processing buffer
   for(i=0; i<NumSamples; i++)
      pFFT[i] = RealIn[i];

   // Perform the FFT
   RealFFTf(pFFT, hFFT);

   // Copy the data into the real and imaginary outputs
   for(i=1;i<(NumSamples/2);i++) {
      RealOut[i]=pFFT[hFFT->BitReversed[i]  ];
      ImagOut[i]=pFFT[hFFT->BitReversed[i]+1];
   }
   // Handle the (real-only) DC and Fs/2 bins
   RealOut[0] = pFFT[0];
   RealOut[i] = pFFT[1];
   ImagOut[0] = ImagOut[i] = 0;
   // Fill in the upper half using symmetry properties
   for(i++ ; i<NumSamples; i++) {
      RealOut[i] =  RealOut[NumSamples-i];
      ImagOut[i] = -ImagOut[NumSamples-i];
   }
   delete [] pFFT;
   ReleaseFFT(hFFT);
}

/*
 * InverseRealFFT
 *
 * This function computes the inverse of RealFFT, above.
 * The RealIn and ImagIn is assumed to be conjugate-symmetric
 * and as a result the output is purely real.
 * Only the first half of RealIn and ImagIn are used due to this
 * symmetry assumption.
 */
void InverseRealFFT(int NumSamples, float *RealIn, float *ImagIn, float *RealOut)
{
   // Remap to RealFFTf() function
   int i;
   HFFT hFFT = GetFFT(NumSamples);
   float *pFFT = new float[NumSamples];
   // Copy the data into the processing buffer
   for(i=0; i<(NumSamples/2); i++)
      pFFT[2*i  ] = RealIn[i];
   if(ImagIn == NULL) {
      for(i=0; i<(NumSamples/2); i++)
         pFFT[2*i+1] = 0;
   } else {
      for(i=0; i<(NumSamples/2); i++)
         pFFT[2*i+1] = ImagIn[i];
   }
   // Put the fs/2 component in the imaginary part of the DC bin
   pFFT[1] = RealIn[i];

   // Perform the FFT
   InverseRealFFTf(pFFT, hFFT);

   // Copy the data to the (purely real) output buffer
   ReorderToTime(hFFT, pFFT, RealOut);

   delete [] pFFT;
   ReleaseFFT(hFFT);
}

void RealInverseRealFFT(int NumSamples,	float *In, float *Out){
	// Remap to RealFFTf() function
	int i;
	HFFT hFFT = GetFFT(NumSamples);
	float *pFFT = new float[NumSamples];
	// Copy the data into the processing buffer
	pFFT[0] = In[0];
	for(i=1; i<NumSamples/2; i++){
		pFFT[i] = In[i];
		pFFT[NumSamples - i] = In[i];
	}
	pFFT[NumSamples/2] = In[NumSamples/2];

	// Perform the FFT
	RealFFTf(pFFT, hFFT);

	// Copy the data into the output
	for(i=1;i<NumSamples/2;i++) {
		Out[i]=pFFT[hFFT->BitReversed[i]];
	}
	// Handle the (real-only) DC
	Out[0] = pFFT[0];
	delete [] pFFT;
	ReleaseFFT(hFFT);
}

/*
 * PowerSpectrum
 *
 * This function computes the same as RealFFT, above, but
 * adds the squares of the real and imaginary part of each
 * coefficient, extracting the power and throwing away the
 * phase.
 *
 * For speed, it does not call RealFFT, but duplicates some
 * of its code.
 */

void PowerSpectrum(int NumSamples, float *In, float *Out)
{
   // Remap to RealFFTf() function
   int i;
   HFFT hFFT = GetFFT(NumSamples);
   float *pFFT = new float[NumSamples];
   // Copy the data into the processing buffer
   for(i=0; i<NumSamples; i++)
      pFFT[i] = In[i];

   // Perform the FFT
   RealFFTf(pFFT, hFFT);

   // Copy the data into the real and imaginary outputs
   for(i=1;i<NumSamples/2;i++) {
      Out[i]= (pFFT[hFFT->BitReversed[i]  ]*pFFT[hFFT->BitReversed[i]  ])
         + (pFFT[hFFT->BitReversed[i]+1]*pFFT[hFFT->BitReversed[i]+1]);
   }
   // Handle the (real-only) DC and Fs/2 bins
   Out[0] = pFFT[0]*pFFT[0];
   Out[i] = pFFT[1]*pFFT[1];
   delete [] pFFT;
   ReleaseFFT(hFFT);
}