#ifndef ENCODING_HELPER_HLSL
#define ENCODING_HELPER_HLSL

// Reference: https://github.com/nvpro-samples/vk_raytrace/blob/master/shaders/compress.glsl

inline float roundEven(float x)
{
    int   Integer = asint(x);
    float IntegerPart = asfloat(Integer);
    float FractionalPart = (x - floor(x));

    if (FractionalPart > 0.5f || FractionalPart < 0.5f)
    {
        return round(x);
    }
    else if ((Integer % 2) == 0)
    {
        return IntegerPart;
    }
    else if (x <= 0)  // Work around...
    {
        return IntegerPart - 1;
    }
    else
    {
        return IntegerPart + 1;
    }
}

#define C_Stack_Max 3.402823466e+38f
inline uint compress_unit_float(float3 nv)
{
  // map to octahedron and then flatten to 2D (see 'Octahedron Environment Maps' by Engelhardt & Dachsbacher)
  if((nv.x < C_Stack_Max) && !isinf(nv.x))
  {
    const float d = 32767.0f / (abs(nv.x) + abs(nv.y) + abs(nv.z));
    int         x = int(roundEven(nv.x * d));
    int         y = int(roundEven(nv.y * d));

    if(nv.z < 0.0f)
    {
      const int maskx = x >> 31;
      const int masky = y >> 31;
      const int tmp   = 32767 + maskx + masky;
      const int tmpx  = x;
      x               = (tmp - (y ^ masky)) ^ maskx;
      y               = (tmp - (tmpx ^ maskx)) ^ masky;
    }

    uint packed = (uint(y + 32767) << 16) | uint(x + 32767);
    if(packed == ~0u)
      return ~0x1u;
    return packed;
  }
  else
  {
    return ~0u;
  }
}

inline float short_to_floatm11(const int v)  // linearly maps a short 32767-32768 to a float -1-+1 //!! opt.?
{
    return (v >= 0) ? (asfloat(0x3F800000u | (uint(v) << 8)) - 1.0f) :
        (asfloat((0x80000000u | 0x3F800000u) | (uint(-v) << 8)) + 1.0f);
}

inline float3 decompress_unit_float(uint packed)
{
  if(packed != ~0u)  // sanity check, not needed as isvalid_unit_float is called earlier
  {
    int x = int(packed & 0xFFFFu) - 32767;
    int y = int(packed >> 16) - 32767;

    const int maskx = x >> 31;
    const int masky = y >> 31;
    const int tmp0  = 32767 + maskx + masky;
    const int ymask = y ^ masky;
    const int tmp1  = tmp0 - (x ^ maskx);
    const int z     = tmp1 - ymask;
    float     zf;
    if(z < 0)
    {
      x  = (tmp0 - ymask) ^ maskx;
      y  = tmp1 ^ masky;
      zf = asfloat((0x80000000u | 0x3F800000u) | (uint(-z) << 8)) + 1.0f;
    }
    else
    {
      zf = asfloat(0x3F800000u | (uint(z) << 8)) - 1.0f;
    }

    return normalize(float3(short_to_floatm11(x), short_to_floatm11(y), zf));
  }
  else
  {
    return float3(C_Stack_Max, C_Stack_Max, C_Stack_Max);
  }
}

inline float compress_two_floats(float a, float b) {
    uint a16 = f32tof16(a);
    uint b16 = f32tof16(b);
    uint abPacked = (a16 << 16) | b16;
    return asfloat(abPacked);
}
inline void unpack_two_floats(float input, out float a, out float b) {
    uint uintInput = asuint(input);
    a = f16tof32(uintInput >> 16);
    b = f16tof32(uintInput);
}

#endif