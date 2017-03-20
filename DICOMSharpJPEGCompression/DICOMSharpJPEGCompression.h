#ifdef DICOMSHARPJPEGCOMPRESSION_EXPORTS
#define DICOMSHARPJPEGCOMPRESSION_API __declspec(dllexport)
#else
#define DICOMSHARPJPEGCOMPRESSION_API __declspec(dllimport)
#endif

DICOMSHARPJPEGCOMPRESSION_API unsigned char * UncompressJPEG(unsigned char * dataIn, int length, int bitsAllocated, int width, int height,
	int samplesPerPixel, int planarConfiguration, bool ybr, int * lenOut);
DICOMSHARPJPEGCOMPRESSION_API unsigned char * UncompressJPEGLS(unsigned char * dataIn, int length, int bitsAllocated, int width, int height,
	int samplesPerPixel, int planarConfiguration, int * lenOut);
DICOMSHARPJPEGCOMPRESSION_API unsigned char * UncompressJ2K(unsigned char * dataIn, int length, int bitsAllocated, int width, int height,
	int samplesPerPixel, int planarConfiguration, int * lenOut);

DICOMSHARPJPEGCOMPRESSION_API unsigned char * CompressJPEG(unsigned char * dataIn, int bitsAllocated, int width, int height,
	int samplesPerPixel, int planarConfiguration, int compressMode, int firstOrder, int pointTrans, int * lenOut);
DICOMSHARPJPEGCOMPRESSION_API unsigned char * CompressJPEGLS(unsigned char * dataIn, int bitsAllocated, int width, int height,
	int samplesPerPixel, int planarConfiguration, int compressMode, int * lenOut);
DICOMSHARPJPEGCOMPRESSION_API unsigned char * CompressJ2K(unsigned char * dataIn, int bitsAllocated, int width, int height,
	int samplesPerPixel, int planarConfiguration, int * lenOut);

DICOMSHARPJPEGCOMPRESSION_API void FreePtr(unsigned char * ptrToFree);