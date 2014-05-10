#include "compatibility.h"

#include <math.h> 

#ifdef _MSC_VER
#if(_MSC_VER < 1800) // < VS2013
namespace std
{
	float round(float d)
	{
		return static_cast<float>(floor(d + 0.5));
	}

	double round(double d)
	{
		return floor(d + 0.5);
	}

	long double round(long double d)
	{
		return floor(d + 0.5);
	}

}

double remainder(double num, double denom){
	return num - std::round(num/denom) * denom;
}
#endif
#endif