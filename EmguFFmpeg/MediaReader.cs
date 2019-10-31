﻿using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.IO;

namespace EmguFFmpeg
{
    public unsafe partial class MediaReader : MediaMux
    {
        public new InFormat Format => base.Format as InFormat;

        public MediaReader(Stream stream, InFormat iformat = null, MediaDictionary options = null)
        {
            baseStream = stream;
            avio_Alloc_Context_Read_Packet = ReadFunc;
            avio_Alloc_Context_Seek = SeekFunc;
            pFormatContext = ffmpeg.avformat_alloc_context();
            pIOContext = ffmpeg.avio_alloc_context((byte*)ffmpeg.av_malloc(bufferLength), bufferLength, 0, null,
                avio_Alloc_Context_Read_Packet, null, avio_Alloc_Context_Seek);
            pFormatContext->pb = pIOContext;
            fixed (AVFormatContext** ppFormatContext = &pFormatContext)
            {
                ffmpeg.avformat_open_input(ppFormatContext, null, iformat, options).ThrowExceptionIfError();
            }
            ffmpeg.avformat_find_stream_info(pFormatContext, null).ThrowExceptionIfError();
            base.Format = iformat ?? new InFormat(pFormatContext->iformat);

            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                AVStream* pStream = pFormatContext->streams[i];
                MediaDecode codec = MediaDecode.CreateDecode(pStream->codecpar->codec_id, _ =>
                {
                    ffmpeg.avcodec_parameters_to_context(_, pStream->codecpar);
                });
                streams.Add(new MediaStream(pStream) { Codec = codec });
            }
        }

        public MediaReader(string file, InFormat iformat = null, MediaDictionary options = null)
        {
            fixed (AVFormatContext** ppFormatContext = &pFormatContext)
            {
                ffmpeg.avformat_open_input(ppFormatContext, file, iformat, options).ThrowExceptionIfError();
            }
            ffmpeg.avformat_find_stream_info(pFormatContext, null).ThrowExceptionIfError();
            base.Format = iformat ?? new InFormat(pFormatContext->iformat);

            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                AVStream* pStream = pFormatContext->streams[i];
                MediaDecode codec = MediaDecode.CreateDecode(pStream->codecpar->codec_id, _ =>
                {
                    ffmpeg.avcodec_parameters_to_context(_, pStream->codecpar);
                });
                streams.Add(new MediaStream(pStream) { Codec = codec });
            }
        }

        public override void DumpInfo()
        {
            for (int i = 0; i < pFormatContext->nb_streams; i++)
            {
                ffmpeg.av_dump_format(pFormatContext, i, ((IntPtr)pFormatContext->url).PtrToStringUTF8(), 0);
            }
        }

        public void Seek(TimeSpan time, int streamIndex = -1)
        {
            long timestamp = (long)(time.TotalSeconds * ffmpeg.AV_TIME_BASE);
            if (streamIndex >= 0)
                timestamp = ffmpeg.av_rescale_q(timestamp, ffmpeg.av_get_time_base_q(), streams[streamIndex].TimeBase);
            ffmpeg.avformat_seek_file(pFormatContext, streamIndex, long.MinValue, timestamp, timestamp, 0).ThrowExceptionIfError();
        }

        #region IEnumerable<MediaPacket>

        public IEnumerable<MediaPacket> Packets
        {
            get
            {
                using (MediaPacket packet = new MediaPacket())
                {
                    int ret;
                    do
                    {
                        ret = ReadPacket(packet);
                        if (ret < 0 && ret != ffmpeg.AVERROR_EOF)
                            ret.ThrowExceptionIfError();
                        yield return packet;
                        packet.Clear();
                    } while (ret >= 0);
                }
            }
        }

        private int ReadPacket(MediaPacket packet)
        {
            return ffmpeg.av_read_frame(pFormatContext, packet);
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (pFormatContext != null)
            {
                fixed (AVFormatContext** ppFormatContext = &pFormatContext)
                {
                    if (baseStream != null)
                    {
                        ffmpeg.avio_context_free(&pFormatContext->pb);
                        baseStream.Dispose();
                    }
                    ffmpeg.avformat_close_input(ppFormatContext);
                    avio_Alloc_Context_Read_Packet = null;
                    avio_Alloc_Context_Write_Packet = null;
                    avio_Alloc_Context_Seek = null;
                    pFormatContext = null;
                    pIOContext = null;
                }
            }
        }

        #endregion
    }
}