// DICOMSharpJPEGCompression.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "DICOMSharpJPEGCompression.h"

#include "OPJ_Shim.h"

//OpenJPEG -- for JPEG2k support
extern "C" {
#include "openjpeg-v2.1.2-windows-x86/include/openjpeg-2.1/openjpeg.h"
}

//DCMTK -- for JPEG support
#include "dcmtk-3.6.0/dcmjpeg/include/dcmtk/dcmjpeg/djcparam.h"

#include "dcmtk-3.6.0/dcmjpeg/include/dcmtk/dcmjpeg/djeijg8.h"
#include "dcmtk-3.6.0/dcmjpeg/include/dcmtk/dcmjpeg/djeijg12.h"
#include "dcmtk-3.6.0/dcmjpeg/include/dcmtk/dcmjpeg/djeijg16.h"

#include "dcmtk-3.6.0/dcmjpeg/include/dcmtk/dcmjpeg/djdijg8.h"
#include "dcmtk-3.6.0/dcmjpeg/include/dcmtk/dcmjpeg/djdijg12.h"
#include "dcmtk-3.6.0/dcmjpeg/include/dcmtk/dcmjpeg/djdijg16.h"

//CharLS -- for JPEG-LS support
#define CHARLS_DLL
#include "charls-2.0.0/src/charls.h"

DICOMSHARPJPEGCOMPRESSION_API unsigned char * UncompressJPEG(unsigned char * dataIn, int length, int bitsAllocated, int width, int height,
    int samplesPerPixel, int planarConfiguration, bool ybr, int * lenOut)
{
    if (bitsAllocated == 8)
    {
        DJCodecParameter param(
            ECC_lossyRGB,
            ybr ? EDC_always : EDC_never,
            EUC_never,
            planarConfiguration ? EPC_colorByPlane : EPC_colorByPixel
            );

        int outSize = width*height*samplesPerPixel;
        BYTE *outBuf = new BYTE[outSize];

        DJDecompressIJG8Bit *decop = new DJDecompressIJG8Bit(param, ybr);
        OFCondition check1 = decop->init();
        OFCondition check2 = decop->decode(dataIn, length, outBuf, outSize, false);
        delete decop;

        *lenOut = outSize;
        return outBuf;
    }
    else if (bitsAllocated == 12)
    {
        DJCodecParameter param(
            ECC_lossyRGB,
            ybr ? EDC_always : EDC_never,
            EUC_never,
            planarConfiguration ? EPC_colorByPlane : EPC_colorByPixel
            );

        int outSize = width*height*samplesPerPixel*2;
        BYTE *outBuf = new BYTE[outSize];

        DJDecompressIJG12Bit *decop = new DJDecompressIJG12Bit(param, ybr);
        OFCondition check1 = decop->init();
        OFCondition check2 = decop->decode(dataIn, length, outBuf, outSize, false);
        delete decop;

        *lenOut = outSize;
        return outBuf;
    }
    else if (bitsAllocated == 16)
    {
        DJCodecParameter param(
            ECC_lossyRGB,
            ybr ? EDC_always : EDC_never,
            EUC_never,
            planarConfiguration ? EPC_colorByPlane : EPC_colorByPixel
            );

        int outSize = width*height*samplesPerPixel*2;
        BYTE *outBuf = new BYTE[outSize];

        DJDecompressIJG16Bit *decop = new DJDecompressIJG16Bit(param, ybr);
        OFCondition check1 = decop->init();
        OFCondition check2 = decop->decode(dataIn, length, outBuf, outSize, false);
        delete decop;

        *lenOut = outSize;
        return outBuf;
    }

    return NULL;
}

DICOMSHARPJPEGCOMPRESSION_API unsigned char * UncompressJPEGLS(unsigned char * dataIn, int length, int bitsAllocated, int width, int height,
    int samplesPerPixel, int planarConfiguration, int * lenOut)
{
    //can do without the header reading, just remove the &info from the JpegLsDecode... might be nice to have to verify or something, though
    JlsParameters info;
    CharlsApiResultType error = JpegLsReadHeader(dataIn, length, &info, NULL);
    if (error != CharlsApiResultType::OK)
        return NULL;

    int uncompLen = width * height * samplesPerPixel * ((bitsAllocated == 8) ? 1 : 2);
    BYTE *newdata = new BYTE[uncompLen];
    error = JpegLsDecode(newdata, uncompLen, dataIn, length, &info, NULL);
    if (error != CharlsApiResultType::OK)
    {
        delete []newdata;
        return NULL;
    }

    *lenOut = uncompLen;
    return newdata;
}

DICOMSHARPJPEGCOMPRESSION_API unsigned char * UncompressJ2K(unsigned char * dataIn, int length, int bitsAllocated, int width, int height,
    int samplesPerPixel, int planarConfiguration, int * lenOut)
{
    opj_codec_t * codec = opj_create_decompress(OPJ_CODEC_J2K);

    opj_input_memory_stream streamInfo;
    streamInfo.dataSize = length;
    streamInfo.offset = 0;
    streamInfo.pData = dataIn;

    opj_stream_t * stream = opj_stream_default_create(1);
    if (!stream){
        return NULL;
    }
    opj_stream_set_read_function(stream, opj_input_memory_stream_read);
    opj_stream_set_seek_function(stream, opj_input_memory_stream_seek);
    opj_stream_set_skip_function(stream, opj_input_memory_stream_skip);
    opj_stream_set_user_data(stream, &streamInfo, NULL);
    opj_stream_set_user_data_length(stream, streamInfo.dataSize);

    opj_dparameters_t params;
    opj_set_default_decoder_parameters(&params);
    opj_setup_decoder(codec, &params);

    opj_image_t *imptr;
    opj_read_header(stream, codec, &imptr);
    opj_decode(codec, stream, imptr);

    void *vOutBits = NULL;
    if (bitsAllocated == 8)
    {
        BYTE *newdata = new BYTE[imptr->x1 * imptr->y1 * imptr->numcomps];
        for (unsigned int c=0; c<imptr->numcomps; c++)
            for (unsigned int i = 0; i< imptr->x1 * imptr->y1; i++)
                newdata[i*imptr->numcomps + c] = (BYTE) ((DWORD *) (imptr->comps[c].data))[i];
        vOutBits = (void *) newdata;
    }
    else if (bitsAllocated == 12 || bitsAllocated == 16)
    {
        WORD *newdata = new WORD[imptr->x1 * imptr->y1];
        for (unsigned int i = 0; i< imptr->x1 * imptr->y1; i++)
            newdata[i] = (WORD) ((DWORD *) (imptr->comps[0].data))[i];
        vOutBits = (void *) newdata;
    }

    *lenOut = imptr->x1 * imptr->y1 * ((bitsAllocated == 8) ? 1 : 2) * imptr->numcomps;

    opj_image_destroy(imptr);
    opj_stream_destroy(stream);
    opj_destroy_codec(codec);

    return (BYTE *) vOutBits;
}

DICOMSHARPJPEGCOMPRESSION_API unsigned char * CompressJPEG(unsigned char * dataIn, int bitsAllocated, int width, int height, int samplesPerPixel, int planarConfiguration, int compressMode, int firstOrder, int pointTrans, int * lenOut)
{
    DJCodecParameter param(
        (samplesPerPixel == 3) ? ECC_lossyYCbCr : ECC_monochrome,
        EDC_always,
        EUC_never,
        EPC_colorByPixel
        );

    int maxSize = width*height*samplesPerPixel*(bitsAllocated == 8 ? 1 : 2);
    BYTE *outBuf = (BYTE *) malloc(maxSize);
    Uint32 outLen;

    //make this more configurable somehow?  don't know what's needed...
    Uint8 quality = (compressMode == EJM_lossless) ? 100 : 80;

    //configure the monochrome1?
    EP_Interpretation interpretation = (samplesPerPixel == 3) ? EPI_RGB : EPI_Monochrome1;

    OFCondition check1;
    if (bitsAllocated == 8)
    {
        DJCompressIJG8Bit *comp;
        if (compressMode == EJM_lossless)
            comp = new DJCompressIJG8Bit(param, (EJ_Mode) compressMode, firstOrder, pointTrans);
        else
            comp = new DJCompressIJG8Bit(param, (EJ_Mode) compressMode, quality);
        check1 = comp->encode(width, height, interpretation, samplesPerPixel, dataIn, outBuf, outLen);
        delete comp;
    }
    else if (bitsAllocated == 12)
    {
        DJCompressIJG12Bit *comp;
        if (compressMode == EJM_lossless)
            comp = new DJCompressIJG12Bit(param, (EJ_Mode) compressMode, firstOrder, pointTrans);
        else
            comp = new DJCompressIJG12Bit(param, (EJ_Mode) compressMode, quality);
        check1 = comp->encode(width, height, interpretation, samplesPerPixel, (Uint16 *) dataIn, outBuf, outLen);
        delete comp;
    }
    else if (bitsAllocated == 16)
    {
        DJCompressIJG16Bit *comp = new DJCompressIJG16Bit(param, (EJ_Mode) compressMode, firstOrder, pointTrans);
        check1 = comp->encode(width, height, interpretation, samplesPerPixel, (Uint16 *) dataIn, outBuf, outLen);
        delete comp;
    }

    if (check1 != ECC_Normal)
    {
        free(outBuf);
        return NULL;
    }

    //drop it in size to save memory
    realloc(outBuf, outLen);
    *lenOut = outLen;
    return outBuf;

    return NULL;
}

DICOMSHARPJPEGCOMPRESSION_API unsigned char * CompressJPEGLS(unsigned char * dataIn, int bitsAllocated, int width, int height,
    int samplesPerPixel, int planarConfiguration, int compressMode, int * lenOut)
{
    int maxSize = width*height*samplesPerPixel*(bitsAllocated == 8 ? 1 : 2);
    BYTE *outBuf = (BYTE *) malloc(maxSize);

    JlsParameters info = JlsParameters();
    info.components = samplesPerPixel;
    info.bitsPerSample = bitsAllocated;
    info.height = height;
    info.width = width;
    info.allowedLossyError = compressMode;

    if (samplesPerPixel == 3)
    {
        info.interleaveMode = CharlsInterleaveModeType::Line;
        info.colorTransformation = CharlsColorTransformationType::HP1;
    }

    size_t compressedLength;
    CharlsApiResultType err = JpegLsEncode(outBuf, maxSize, &compressedLength, dataIn, maxSize, &info, NULL);
    if (err == CharlsApiResultType::OK)
    {
        *lenOut = compressedLength;
        return outBuf;
    }

    delete []outBuf;
    return NULL;
}


DICOMSHARPJPEGCOMPRESSION_API unsigned char * CompressJ2K(unsigned char * dataIn, int bitsAllocated, int width, int height, int samplesPerPixel, int planarConfiguration, int * lenOut)
{
    opj_cparameters_t params;
    opj_set_default_encoder_parameters(&params);
    params.cod_format = OPJ_CODEC_J2K;
    params.irreversible = 0;
    params.tcp_numlayers = 1;
    params.cp_disto_alloc = 1;

    opj_image_cmptparm_t *cmptparm = new opj_image_cmptparm_t[samplesPerPixel];

    for (int i=0; i<samplesPerPixel; i++)
    {
        cmptparm[i].prec = bitsAllocated;
        cmptparm[i].bpp = bitsAllocated;
        cmptparm[i].sgnd = 0;
        cmptparm[i].dx = 1;
        cmptparm[i].dy = 1;
        cmptparm[i].x0 = 0;
        cmptparm[i].y0 = 0;
        cmptparm[i].w = width;
        cmptparm[i].h = height;
    }

    opj_image_t *jp2_image = opj_image_create(samplesPerPixel, cmptparm, (samplesPerPixel == 1) ? OPJ_CLRSPC_GRAY : OPJ_CLRSPC_SRGB);

    jp2_image->x0 = 0;
    jp2_image->y0 = 0;
    jp2_image->x1 = width;
    jp2_image->y1 = height;

    //dump the source image into the components
    if (samplesPerPixel == 1)
    {
        if (bitsAllocated == 8)
        {
            for (int p=0; p<width*height; p++)
                jp2_image->comps[0].data[p] = dataIn[p];
        }
        else
        {
            //2-byte
            for (int p=0; p<width*height; p++)
                jp2_image->comps[0].data[p] = ((WORD *)dataIn)[p];
        }
    }
    else
    {
        //always 8-bit, to my understanding

        if (planarConfiguration == 0)	//0 = R1G1B1 R2G2B2, 1 = R1R2R3..G1G2G3...B1B2B3...
        {
            for (int i=0; i<samplesPerPixel; i++)
                for (int p=0; p<width*height; p++)
                    jp2_image->comps[i].data[p] = dataIn[p*samplesPerPixel + i];
        }
        else
        {
            for (int i=0; i<samplesPerPixel; i++)
                for (int p=0; p<width*height; p++)
                    jp2_image->comps[i].data[p] = dataIn[i*width*height + p];
        }
    }

    opj_codec_t * codec = opj_create_compress(OPJ_CODEC_J2K);
    opj_setup_encoder(codec, &params, jp2_image);


    opj_output_memory_stream streamInfo;
    streamInfo.dataSize = 0;

    opj_stream_t * stream = opj_stream_default_create(0);
    if (!stream){
        return NULL;
    }
    opj_stream_set_write_function(stream, opj_output_memory_stream_write);
    opj_stream_set_seek_function(stream, opj_output_memory_stream_seek);
    opj_stream_set_skip_function(stream, opj_output_memory_stream_skip);
    opj_stream_set_user_data(stream, &streamInfo, NULL);

    opj_start_compress(codec, jp2_image, stream);
    opj_encode(codec, stream);
    opj_end_compress(codec, stream);

    delete []cmptparm;

    *lenOut = streamInfo.dataSize;
    BYTE * vOutBits = (BYTE *) malloc(streamInfo.dataSize), *fillPtr = vOutBits;
    for (std::vector<opj_output_memory_stream_chunk>::iterator i = streamInfo.chunks.begin(); i != streamInfo.chunks.end(); i++)
    {
        memcpy(fillPtr, i->data, i->dataLen);
        delete i->data;
        fillPtr += i->dataLen;
    }    

    opj_stream_destroy(stream);
    opj_destroy_codec(codec);
    opj_image_destroy(jp2_image);

    return (BYTE *) vOutBits;
}

DICOMSHARPJPEGCOMPRESSION_API void FreePtr(unsigned char * ptrToFree)
{
    free(ptrToFree);
}
