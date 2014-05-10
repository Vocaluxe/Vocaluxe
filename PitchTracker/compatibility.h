//Contains some definitions to make the code work with compilers that do not support all feautures

#pragma once

#ifdef _MSC_VER
	#define constexpr const
	#if(_MSC_VER < 1800) // < VS2013
		namespace std
		{
			float round(float d);
			double round(double d);
			long double round(long double d);
		}

		double remainder(double num, double denom);
	#endif
#endif