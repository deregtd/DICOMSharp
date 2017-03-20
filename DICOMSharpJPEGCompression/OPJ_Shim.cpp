// OpenJPEG Memory Shims
#include "stdafx.h"
#include "OPJ_Shim.h"



// Reader From https://groups.google.com/forum/#!topic/openjpeg/8cebr0u7JgY

OPJ_SIZE_T opj_input_memory_stream_read(void * p_buffer, OPJ_SIZE_T p_nb_bytes, void * p_user_data)
{
    opj_input_memory_stream* l_stream = (opj_input_memory_stream*)p_user_data;
    OPJ_SIZE_T l_nb_bytes_read = p_nb_bytes;

    if (l_stream->offset >= l_stream->dataSize) {
        return (OPJ_SIZE_T)-1;
    }
    if (p_nb_bytes > (l_stream->dataSize - l_stream->offset)) {
        l_nb_bytes_read = l_stream->dataSize - l_stream->offset;
    }
    memcpy(p_buffer, &(l_stream->pData[l_stream->offset]), l_nb_bytes_read);
    l_stream->offset += l_nb_bytes_read;
    return l_nb_bytes_read;
}

OPJ_OFF_T opj_input_memory_stream_skip(OPJ_OFF_T p_nb_bytes, void * p_user_data)
{
    opj_input_memory_stream* l_stream = (opj_input_memory_stream*)p_user_data;

    if (p_nb_bytes < 0) {
        return -1;
    }

    l_stream->offset += (OPJ_SIZE_T)p_nb_bytes;

    return p_nb_bytes;
}

OPJ_BOOL opj_input_memory_stream_seek(OPJ_OFF_T p_nb_bytes, void * p_user_data)
{
    opj_input_memory_stream* l_stream = (opj_input_memory_stream*)p_user_data;

    if (p_nb_bytes < 0) {
        return OPJ_FALSE;
    }

    l_stream->offset = (OPJ_SIZE_T)p_nb_bytes;

    return OPJ_TRUE;
}


// Writer adapted from above and made it up

OPJ_BOOL opj_output_memory_stream_seek(OPJ_OFF_T p_nb_bytes, void * p_user_data)
{
    opj_output_memory_stream* stream = (opj_output_memory_stream*)p_user_data;

    return OPJ_TRUE;
}

OPJ_OFF_T opj_output_memory_stream_skip(OPJ_OFF_T p_nb_bytes, void * p_user_data)
{
    opj_output_memory_stream* stream = (opj_output_memory_stream*)p_user_data;

    return p_nb_bytes;
}

OPJ_SIZE_T opj_output_memory_stream_write(void * p_buffer, OPJ_SIZE_T p_nb_bytes, void * p_user_data)
{
    opj_output_memory_stream* stream = (opj_output_memory_stream*)p_user_data;

    opj_output_memory_stream_chunk newChunk;
    newChunk.dataLen = p_nb_bytes;
    newChunk.data = new BYTE[p_nb_bytes];
    memcpy(newChunk.data, p_buffer, p_nb_bytes);
    
    stream->dataSize += p_nb_bytes;

    stream->chunks.push_back(newChunk);

    return p_nb_bytes;
}
