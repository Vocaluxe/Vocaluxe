/**********************************************************************

  FFT.h

  Dominic Mazzoni

  September 2000

  This file contains a few FFT routines, including a real-FFT
  routine that is almost twice as fast as a normal complex FFT,
  and a power spectrum routine which is more convenient when
  you know you don't care about phase information.  It now also
  contains a few basic windowing functions.

  Some of this code was based on a free implementation of an FFT
  by Don Cross, available on the web at:

    http://www.intersrv.com/~dcross/fft.html

  The basic algorithm for his code was based on Numerical Recipes
  in Fortran.  I optimized his code further by reducing array
  accesses, caching the bit reversal table, and eliminating
  float-to-double conversions, and I added the routines to
  calculate a real FFT and a real power spectrum.

  Note: all of these routines use single-precision floats.
  I have found that in practice, floats work well until you
  get above 8192 samples.  If you need to do a larger FFT,
  you need to use doubles.

**********************************************************************/

/*
 * This is the function you will use the most often.
 * Given an array of floats, this will compute the power
 * spectrum by doing a Real FFT and then computing the
 * sum of the squares of the real and imaginary parts.
 * Note that the output array is half the length of the
 * input array, and that NumSamples must be a power of two.
 */

void PowerSpectrum(int NumSamples, float *In, float *Out);

/*
 * Computes an FFT when the input data is real but you still
 * want complex data as output.  The output arrays are the
 * same length as the input, but will be conjugate-symmetric
 * NumSamples must be a power of two.
 */

void RealFFT(int NumSamples,
             float *RealIn, float *RealOut, float *ImagOut);

/*
 * Computes an Inverse FFT when the input data is conjugate symmetric
 * so the output is purely real.  NumSamples must be a power of
 * two.
 * Requires: EXPERIMENTAL_USE_REALFFTF
 */
void InverseRealFFT(int NumSamples,
					float *RealIn, float *ImagIn, float *RealOut);

void RealInverseRealFFT(int NumSamples,	float *In, float *Out);

void DeinitFFT();
