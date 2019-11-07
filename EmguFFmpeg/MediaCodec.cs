﻿using FFmpeg.AutoGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace EmguFFmpeg
{
    public unsafe abstract class MediaCodec : IDisposable
    {
        protected AVCodec* pCodec = null;

        protected AVCodecContext* pCodecContext = null;

        public abstract void Initialize(Action<MediaCodec> setBeforeOpen = null, int flags = 0, MediaDictionary opts = null);

        /// <summary>
        /// Get value if <see cref="Id"/> is not <see cref="AVCodecID.AV_CODEC_ID_NONE"/>
        /// </summary>
        /// <exception cref="FFmpegException"/>
        public AVCodec AVCodec => pCodec == null ? throw new FFmpegException(FFmpegMessage.NullReference) : *pCodec;

        /// <summary>
        /// Get value after <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/>
        /// </summary>
        /// <exception cref="FFmpegException"/>
        public AVCodecContext AVCodecContext => pCodecContext == null ? throw new FFmpegException(FFmpegMessage.NullReference) : *pCodecContext;

        public AVMediaType Type => pCodec == null ? AVMediaType.AVMEDIA_TYPE_UNKNOWN : pCodec->type;
        public AVCodecID Id => pCodec == null ? AVCodecID.AV_CODEC_ID_NONE : pCodec->id;
        public string Name => pCodec == null ? null : ((IntPtr)pCodec->name).PtrToStringUTF8();
        public string LongName => pCodec == null ? null : ((IntPtr)pCodec->long_name).PtrToStringUTF8();
        public bool IsDecoder => pCodec == null ? false : ffmpeg.av_codec_is_decoder(pCodec) > 0;
        public bool IsEncoder => pCodec == null ? false : ffmpeg.av_codec_is_encoder(pCodec) > 0;

        #region Supported

        public List<AVCodecHWConfig> SupportedHardware
        {
            get
            {
                List<AVCodecHWConfig> result = new List<AVCodecHWConfig>();
                if (pCodec == null) return result;
                for (int i = 0; ; i++)
                {
                    AVCodecHWConfig* config = ffmpeg.avcodec_get_hw_config(pCodec, i);
                    if (config == null)
                        return result;
                    result.Add(*config);
                }
            }
        }

        public List<AVPixelFormat> SupportedPixelFmts
        {
            get
            {
                List<AVPixelFormat> result = new List<AVPixelFormat>();
                if (pCodec == null) return result;
                AVPixelFormat* p = pCodec->pix_fmts;
                if (p != null)
                {
                    while (*p != AVPixelFormat.AV_PIX_FMT_NONE)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result;
            }
        }

        public List<AVRational> SupportedFrameRates
        {
            get
            {
                List<AVRational> result = new List<AVRational>();
                if (pCodec == null) return result;
                AVRational* p = pCodec->supported_framerates;
                if (p != null)
                {
                    while (p->num != 0)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result;
            }
        }

        public List<AVSampleFormat> SupportedSampelFmts
        {
            get
            {
                List<AVSampleFormat> result = new List<AVSampleFormat>();
                if (pCodec == null) return result;
                AVSampleFormat* p = pCodec->sample_fmts;
                if (p != null)
                {
                    while (*p != AVSampleFormat.AV_SAMPLE_FMT_NONE)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result;
            }
        }

        public List<int> SupportedSampleRates
        {
            get
            {
                List<int> result = new List<int>();
                if (pCodec == null) return result;
                int* p = pCodec->supported_samplerates;
                if (p != null)
                {
                    while (*p != 0)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result;
            }
        }

        public List<ulong> SupportedChannelLayout
        {
            get
            {
                List<ulong> result = new List<ulong>();
                if (pCodec == null) return result;
                ulong* p = pCodec->channel_layouts;
                if (p != null)
                {
                    while (*p != 0)
                    {
                        result.Add(*p);
                        p++;
                    }
                }
                return result;
            }
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }

        public static implicit operator AVCodec*(MediaCodec value)
        {
            if (value == null) return null;
            return value.pCodec;
        }

        public static implicit operator AVCodecContext*(MediaCodec value)
        {
            if (value == null) return null;
            return value.pCodecContext;
        }

        #region IDisposable Support

        private bool disposedValue = false;

        ~MediaCodec()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                fixed (AVCodecContext** ppCodecContext = &pCodecContext)
                {
                    ffmpeg.avcodec_free_context(ppCodecContext);
                }
                disposedValue = true;
            }
        }

        #endregion
    }

    public unsafe class MediaDecode : MediaCodec
    {
        public static MediaDecode CreateDecode(AVCodecID codecId, Action<MediaCodec> setBeforeOpen = null, MediaDictionary opts = null)
        {
            MediaDecode encode = new MediaDecode(codecId);
            encode.Initialize(setBeforeOpen, 0, opts);
            return encode;
        }

        public static MediaDecode CreateDecode(string codecName, Action<MediaCodec> setBeforeOpen = null, MediaDictionary opts = null)
        {
            MediaDecode encode = new MediaDecode(codecName);
            encode.Initialize(setBeforeOpen, 0, opts);
            return encode;
        }

        /// <summary>
        /// Find decoder by id
        /// <para>
        /// Must call <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/> before decode
        /// </para>
        /// </summary>
        /// <param name="codecId">codec id</param>
        public MediaDecode(AVCodecID codecId)
        {
            pCodec = ffmpeg.avcodec_find_decoder(codecId);
            if (codecId != AVCodecID.AV_CODEC_ID_NONE && pCodec == null)
                throw new FFmpegException(ffmpeg.AVERROR_DECODER_NOT_FOUND);
        }

        /// <summary>
        /// Find decoder by name
        /// <para>
        /// Must call <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/> before decode
        /// </para>
        /// </summary>
        /// <param name="codecName">codec name</param>
        public MediaDecode(string codecName)
        {
            pCodec = ffmpeg.avcodec_find_decoder_by_name(codecName);
            if (pCodec == null)
                throw new FFmpegException(ffmpeg.AVERROR_DECODER_NOT_FOUND);
        }

        internal MediaDecode(AVCodec* codec)
        {
            pCodec = codec;
        }

        /// <summary>
        /// alloc <see cref="AVCodecContext"/> and <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </summary>
        /// <param name="setBeforeOpen">
        /// set <see cref="AVCodecContext"/> after <see cref="ffmpeg.avcodec_alloc_context3(AVCodec*)"/> and before <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </param>
        /// <param name="flags">no used</param>
        /// <param name="opts">options for "avcodec_open2"</param>
        public override void Initialize(Action<MediaCodec> setBeforeOpen = null, int flags = 0, MediaDictionary opts = null)
        {
            pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
            setBeforeOpen?.Invoke(this);
            if (pCodec != null)
            {
                ffmpeg.avcodec_open2(pCodecContext, pCodec, opts).ThrowExceptionIfError();
            }
        }

        /// <summary>
        /// decode packet to get frame.
        /// <para>
        /// <see cref="SendPacket(MediaPacket)"/> and <see cref="ReceiveFrame(MediaFrame)"/>
        /// </para>
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public virtual IEnumerable<MediaFrame> DecodePacket(MediaPacket packet)
        {
            SendPacket(packet);
            using (MediaFrame frame = new MediaFrame())
            {
                while (ReceiveFrame(frame) >= 0)
                {
                    yield return frame;
                }
            }
        }

        /// <summary>
        /// send packet to decoder
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public int SendPacket([In]MediaPacket packet)
        {
            return ffmpeg.avcodec_send_packet(pCodecContext, packet);
        }

        /// <summary>
        /// receive frame from decoder
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public int ReceiveFrame([Out]MediaFrame frame)
        {
            return ffmpeg.avcodec_receive_frame(pCodecContext, frame);
        }

        /// <summary>
        /// get all decodes
        /// </summary>
        public static List<MediaDecode> Decodes
        {
            get
            {
                List<MediaDecode> result = new List<MediaDecode>();
                void* i = null;
                AVCodec* p;
                while ((p = ffmpeg.av_codec_iterate(&i)) != null)
                {
                    if (ffmpeg.av_codec_is_decoder(p) != 0)
                        result.Add(new MediaDecode(p));
                }

                return result;
            }
        }
    }

    public unsafe class MediaEncode : MediaCodec
    {
        public static MediaEncode CreateEncode(AVCodecID codecId, int flags, Action<MediaCodec> setBeforeOpen = null, MediaDictionary opts = null)
        {
            MediaEncode encode = new MediaEncode(codecId);
            encode.Initialize(setBeforeOpen, flags, opts);
            return encode;
        }

        public static MediaEncode CreateEncode(string codecName, int flags, Action<MediaCodec> setBeforeOpen = null, MediaDictionary opts = null)
        {
            MediaEncode encode = new MediaEncode(codecName);
            encode.Initialize(setBeforeOpen, flags, opts);
            return encode;
        }

        public static MediaEncode CreateVideoEncode(OutFormat oformat, int width, int height, int fps, long bitRate = 0, AVPixelFormat format = AVPixelFormat.AV_PIX_FMT_NONE)
        {
            return CreateVideoEncode(oformat.VideoCodec, oformat.Flags, width, height, fps, bitRate, format);
        }

        /// <summary>
        /// Create and init video encode
        /// </summary>
        /// <param name="videoCodec"></param>
        /// <param name="flags"><see cref="MediaFormat.Flags"/></param>
        /// <param name="width">width pixel, must be greater than 0</param>
        /// <param name="height">height pixel, must be greater than 0</param>
        /// <param name="fps">fps, must be greater than 0</param>
        /// <param name="bitRate">default is auto bit rate, must be greater than or equal to 0</param>
        /// <param name="format">default is first supported pixel format</param>
        /// <returns></returns>
        public static MediaEncode CreateVideoEncode(AVCodecID videoCodec, int flags, int width, int height, int fps, long bitRate = 0, AVPixelFormat format = AVPixelFormat.AV_PIX_FMT_NONE)
        {
            return CreateEncode(videoCodec, flags, _ =>
            {
                AVCodecContext* pCodecContext = _;
                if (_.SupportedPixelFmts.Count() <= 0)
                    throw new FFmpegException(FFmpegMessage.CodecIDError);
                if (width <= 0 || height <= 0 || fps <= 0 || bitRate < 0)
                    throw new FFmpegException(FFmpegMessage.NonNegative);
                if (format == AVPixelFormat.AV_PIX_FMT_NONE)
                    format = _.SupportedPixelFmts[0];
                else if (_.SupportedPixelFmts.Where(__ => __ == format).Count() <= 0)
                    throw new FFmpegException(FFmpegMessage.FormatError);
                pCodecContext->width = width;
                pCodecContext->height = height;
                pCodecContext->time_base = new AVRational { num = 1, den = fps };
                pCodecContext->pix_fmt = format;
                pCodecContext->bit_rate = bitRate;
            });
        }

        public static MediaEncode CreateAudioEncode(OutFormat Oformat, AVChannelLayout channelLayout, int sampleRate = 0, long bitRate = 0, AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_NONE)
        {
            return CreateAudioEncode(Oformat.AudioCodec, Oformat.Flags, channelLayout, sampleRate, bitRate, format);
        }

        public static MediaEncode CreateAudioEncode(OutFormat Oformat, int channels, int sampleRate = 0, long bitRate = 0, AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_NONE)
        {
            return CreateAudioEncode(Oformat.AudioCodec, Oformat.Flags, (AVChannelLayout)ffmpeg.av_get_default_channel_layout(channels), sampleRate, bitRate, format);
        }

        public static MediaEncode CreateAudioEncode(AVCodecID audioCodec, int flags, int channels, int sampleRate = 0, long bitRate = 0, AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_NONE)
        {
            return CreateAudioEncode(audioCodec, flags, (AVChannelLayout)ffmpeg.av_get_default_channel_layout(channels), sampleRate, bitRate, format);
        }

        /// <summary>
        /// Create and init audio encode
        /// </summary>
        /// <param name="audioCodec"></param>
        /// <param name="flags"><see cref="MediaFormat.Flags"/></param>
        /// <param name="channelLayout">channel layout</param>
        /// <param name="sampleRate">default is first supported sample rates, must be greater than 0</param>
        /// <param name="bitRate">default is auto bit rate, must be greater than or equal to 0</param>
        /// <param name="format">default is first supported pixel format</param>
        /// <returns></returns>
        public static MediaEncode CreateAudioEncode(AVCodecID audioCodec, int flags, AVChannelLayout channelLayout, int sampleRate = 0, long bitRate = 0, AVSampleFormat format = AVSampleFormat.AV_SAMPLE_FMT_NONE)
        {
            return CreateEncode(audioCodec, flags, _ =>
            {
                AVCodecContext* pCodecContext = _;
                if (_.SupportedSampelFmts.Count() <= 0 || _.SupportedSampleRates.Count <= 0)
                    throw new FFmpegException(FFmpegMessage.CodecIDError);
                // check or set sampleRate
                if (sampleRate <= 0)
                    sampleRate = _.SupportedSampleRates[0];
                else if (_.SupportedSampleRates.Where(__ => __ == sampleRate).Count() <= 0)
                    throw new FFmpegException(FFmpegMessage.SampleRateError);
                // check or set format
                if (format == AVSampleFormat.AV_SAMPLE_FMT_NONE)
                    format = _.SupportedSampelFmts[0];
                else if (_.SupportedSampelFmts.Where(__ => __ == format).Count() <= 0)
                    throw new FFmpegException(FFmpegMessage.FormatError);
                // check channelLayout when SupportedChannelLayout.Count() > 0
                if (_.SupportedChannelLayout.Count() > 0
                     && _.SupportedChannelLayout.Where(__ => __ == (ulong)channelLayout).Count() <= 0)
                    throw new FFmpegException(FFmpegMessage.ChannelLayoutError);
                pCodecContext->sample_rate = sampleRate;
                pCodecContext->channel_layout = (ulong)channelLayout;
                pCodecContext->sample_fmt = format;
                pCodecContext->channels = ffmpeg.av_get_channel_layout_nb_channels(pCodecContext->channel_layout);
                // set 0 to use auto bitrate by ffmpeg
                if (bitRate >= 0)
                    pCodecContext->bit_rate = bitRate;
            });
        }

        /// <summary>
        /// Find encoder by id
        /// <para>
        /// Must call <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/> before encode
        /// </para>
        /// </summary>
        /// <param name="codecId">codec id</param>
        public MediaEncode(AVCodecID codecId)
        {
            pCodec = ffmpeg.avcodec_find_encoder(codecId);
            if (codecId != AVCodecID.AV_CODEC_ID_NONE && pCodec == null)
                throw new FFmpegException(ffmpeg.AVERROR_ENCODER_NOT_FOUND);
        }

        /// <summary>
        /// Find encoder by name
        /// <para>
        /// Must call <see cref="Initialize(Action{MediaCodec}, int, MediaDictionary)"/> before encode
        /// </para>
        /// </summary>
        /// <param name="codecName">codec name</param>
        public MediaEncode(string codecName)
        {
            pCodec = ffmpeg.avcodec_find_encoder_by_name(codecName);
            if (pCodec == null)
                throw new FFmpegException(ffmpeg.AVERROR_ENCODER_NOT_FOUND);
        }

        internal MediaEncode(AVCodec* codec)
        {
            pCodec = codec;
        }

        /// <summary>
        /// alloc <see cref="AVCodecContext"/> and <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </summary>
        /// <param name="setBeforeOpen">
        /// set <see cref="AVCodecContext"/> after <see cref="ffmpeg.avcodec_alloc_context3(AVCodec*)"/> and before <see cref="ffmpeg.avcodec_open2(AVCodecContext*, AVCodec*, AVDictionary**)"/>
        /// </param>
        /// <param name="flags">
        /// check <see cref="MediaFormat.Flags"/> &amp; <see cref="ffmpeg.AVFMT_GLOBALHEADER"/> set <see cref="ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER"/>
        /// </param>
        /// <param name="opts">options for "avcodec_open2"</param>
        public override void Initialize(Action<MediaCodec> setBeforeOpen = null, int flags = 0, MediaDictionary opts = null)
        {
            pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
            setBeforeOpen?.Invoke(this);
            if ((flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
            {
                pCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }
            if (pCodec != null)
            {
                ffmpeg.avcodec_open2(pCodecContext, pCodec, opts).ThrowExceptionIfError();
            }
        }

        /// <summary>
        /// pre process frame
        /// </summary>
        /// <param name="frame"></param>
        private void PreProcessFrame(MediaFrame frame)
        {
            if (frame != null)
            {
                ffmpeg.av_frame_make_writable(frame).ThrowExceptionIfError();
                // Make sure Closed Captions will not be duplicated
                if (AVCodecContext.codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                    ffmpeg.av_frame_remove_side_data(frame, AVFrameSideDataType.AV_FRAME_DATA_A53_CC);
            }
        }

        public virtual IEnumerable<MediaPacket> EncodeFrame(MediaFrame frame)
        {
            PreProcessFrame(frame);
            SendFrame(frame);
            using (MediaPacket packet = new MediaPacket())
            {
                while (true)
                {
                    int ret = ReceivePacket(packet);
                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                        break;
                    ret.ThrowExceptionIfError();
                    yield return packet;
                }
            }
        }

        public int SendFrame([In]MediaFrame frame)
        {
            return ffmpeg.avcodec_send_frame(pCodecContext, frame);
        }

        public int ReceivePacket([Out]MediaPacket packet)
        {
            return ffmpeg.avcodec_receive_packet(pCodecContext, packet);
        }

        public static List<MediaEncode> Encodes
        {
            get
            {
                List<MediaEncode> result = new List<MediaEncode>();
                void* i = null;
                AVCodec* p;
                while ((p = ffmpeg.av_codec_iterate(&i)) != null)
                {
                    if (ffmpeg.av_codec_is_encoder(p) != 0)
                        result.Add(new MediaEncode(p));
                }

                return result;
            }
        }
    }

    [Flags]
    public enum AVChannelLayout : ulong
    {
        /// <summary>
        ///     AV_CH_BACK_CENTER = 0x00000100
        /// </summary>
        AV_CH_BACK_CENTER = 256,

        /// <summary>
        ///     AV_CH_BACK_LEFT = 0x00000010
        /// </summary>
        AV_CH_BACK_LEFT = 16,

        /// <summary>
        ///     AV_CH_BACK_RIGHT = 0x00000020
        /// </summary>
        AV_CH_BACK_RIGHT = 32,

        /// <summary>
        ///     AV_CH_FRONT_CENTER = 0x00000004
        /// </summary>
        AV_CH_FRONT_CENTER = 4,

        /// <summary>
        ///     AV_CH_FRONT_LEFT = 0x00000001
        /// </summary>
        AV_CH_FRONT_LEFT = 1,

        /// <summary>
        ///     AV_CH_FRONT_LEFT_OF_CENTER = 0x00000040
        /// </summary>
        AV_CH_FRONT_LEFT_OF_CENTER = 64,

        /// <summary>
        ///     AV_CH_FRONT_RIGHT = 0x00000002
        /// </summary>
        AV_CH_FRONT_RIGHT = 2,

        /// <summary>
        ///     AV_CH_FRONT_RIGHT_OF_CENTER = 0x00000080
        /// </summary>
        AV_CH_FRONT_RIGHT_OF_CENTER = 128,

        /// <summary>
        ///     AV_CH_LAYOUT_2_1 = (AV_CH_LAYOUT_STEREO|AV_CH_BACK_CENTER)
        /// </summary>
        AV_CH_LAYOUT_2_1 = 259,

        /// <summary>
        ///     AV_CH_LAYOUT_2_2 = (AV_CH_LAYOUT_STEREO|AV_CH_SIDE_LEFT|AV_CH_SIDE_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_2_2 = 1539,

        /// <summary>
        ///     AV_CH_LAYOUT_2POINT1 = (AV_CH_LAYOUT_STEREO|AV_CH_LOW_FREQUENCY)
        /// </summary>
        AV_CH_LAYOUT_2POINT1 = 11,

        /// <summary>
        ///     AV_CH_LAYOUT_3POINT1 = (AV_CH_LAYOUT_SURROUND|AV_CH_LOW_FREQUENCY)
        /// </summary>
        AV_CH_LAYOUT_3POINT1 = 15,

        /// <summary>
        ///     AV_CH_LAYOUT_4POINT0 = (AV_CH_LAYOUT_SURROUND|AV_CH_BACK_CENTER)
        /// </summary>
        AV_CH_LAYOUT_4POINT0 = 263,

        /// <summary>
        ///     AV_CH_LAYOUT_4POINT1 = (AV_CH_LAYOUT_4POINT0|AV_CH_LOW_FREQUENCY)
        /// </summary>
        AV_CH_LAYOUT_4POINT1 = 271,

        /// <summary>
        ///     AV_CH_LAYOUT_5POINT0 = (AV_CH_LAYOUT_SURROUND|AV_CH_SIDE_LEFT|AV_CH_SIDE_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_5POINT0 = 1543,

        /// <summary>
        ///     AV_CH_LAYOUT_5POINT0_BACK = (AV_CH_LAYOUT_SURROUND|AV_CH_BACK_LEFT|AV_CH_BACK_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_5POINT0_BACK = 55,

        /// <summary>
        ///     AV_CH_LAYOUT_5POINT1 = (AV_CH_LAYOUT_5POINT0|AV_CH_LOW_FREQUENCY)
        /// </summary>
        AV_CH_LAYOUT_5POINT1 = 1551,

        /// <summary>
        ///     AV_CH_LAYOUT_5POINT1_BACK = (AV_CH_LAYOUT_5POINT0_BACK|AV_CH_LOW_FREQUENCY)
        /// </summary>
        AV_CH_LAYOUT_5POINT1_BACK = 63,

        /// <summary>
        ///     AV_CH_LAYOUT_6POINT0 = (AV_CH_LAYOUT_5POINT0|AV_CH_BACK_CENTER)
        /// </summary>
        AV_CH_LAYOUT_6POINT0 = 1799,

        /// <summary>
        ///     AV_CH_LAYOUT_6POINT0_FRONT = (AV_CH_LAYOUT_2_2|AV_CH_FRONT_LEFT_OF_CENTER|AV_CH_FRONT_RIGHT_OF_CENTER)
        /// </summary>
        AV_CH_LAYOUT_6POINT0_FRONT = 1731,

        /// <summary>
        ///     AV_CH_LAYOUT_6POINT1 = (AV_CH_LAYOUT_5POINT1|AV_CH_BACK_CENTER)
        /// </summary>
        AV_CH_LAYOUT_6POINT1 = 1807,

        /// <summary>
        ///     AV_CH_LAYOUT_6POINT1_BACK = (AV_CH_LAYOUT_5POINT1_BACK|AV_CH_BACK_CENTER)
        /// </summary>
        AV_CH_LAYOUT_6POINT1_BACK = 319,

        /// <summary>
        ///     AV_CH_LAYOUT_6POINT1_FRONT = (AV_CH_LAYOUT_6POINT0_FRONT|AV_CH_LOW_FREQUENCY)
        /// </summary>
        AV_CH_LAYOUT_6POINT1_FRONT = 1739,

        /// <summary>
        ///     AV_CH_LAYOUT_7POINT0 = (AV_CH_LAYOUT_5POINT0|AV_CH_BACK_LEFT|AV_CH_BACK_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_7POINT0 = 1591,

        /// <summary>
        ///     AV_CH_LAYOUT_7POINT0_FRONT = (AV_CH_LAYOUT_5POINT0|AV_CH_FRONT_LEFT_OF_CENTER|AV_CH_FRONT_RIGHT_OF_CENTER)
        /// </summary>
        AV_CH_LAYOUT_7POINT0_FRONT = 1735,

        /// <summary>
        ///     AV_CH_LAYOUT_7POINT1 = (AV_CH_LAYOUT_5POINT1|AV_CH_BACK_LEFT|AV_CH_BACK_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_7POINT1 = 1599,

        /// <summary>
        ///     AV_CH_LAYOUT_7POINT1_WIDE = (AV_CH_LAYOUT_5POINT1|AV_CH_FRONT_LEFT_OF_CENTER|AV_CH_FRONT_RIGHT_OF_CENTER)
        /// </summary>
        AV_CH_LAYOUT_7POINT1_WIDE = 1743,

        /// <summary>
        ///     AV_CH_LAYOUT_7POINT1_WIDE_BACK = (AV_CH_LAYOUT_5POINT1_BACK|AV_CH_FRONT_LEFT_OF_CENTER|AV_CH_FRONT_RIGHT_OF_CENTER)
        /// </summary>
        AV_CH_LAYOUT_7POINT1_WIDE_BACK = 255,

        /// <summary>
        ///     AV_CH_LAYOUT_HEXADECAGONAL = (AV_CH_LAYOUT_OCTAGONAL|AV_CH_WIDE_LEFT|AV_CH_WIDE_RIGHT|AV_CH_TOP_BACK_LEFT|AV_CH_TOP_BACK_RIGHT|AV_CH_TOP_BACK_CENTER|AV_CH_TOP_FRONT_CENTER|AV_CH_TOP_FRONT_LEFT|AV_CH_TOP_FRONT_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_HEXADECAGONAL = 6442710839uL,

        /// <summary>
        ///     AV_CH_LAYOUT_HEXAGONAL = (AV_CH_LAYOUT_5POINT0_BACK|AV_CH_BACK_CENTER)
        /// </summary>
        AV_CH_LAYOUT_HEXAGONAL = 311,

        /// <summary>
        ///     AV_CH_LAYOUT_MONO = (AV_CH_FRONT_CENTER)
        /// </summary>
        AV_CH_LAYOUT_MONO = 4,

        /// <summary>
        ///     AV_CH_LAYOUT_NATIVE = 0x8000000000000000ULL
        /// </summary>
        AV_CH_LAYOUT_NATIVE = 9223372036854775808uL,

        /// <summary>
        ///     AV_CH_LAYOUT_OCTAGONAL = (AV_CH_LAYOUT_5POINT0|AV_CH_BACK_LEFT|AV_CH_BACK_CENTER|AV_CH_BACK_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_OCTAGONAL = 1847,

        /// <summary>
        ///     AV_CH_LAYOUT_QUAD = (AV_CH_LAYOUT_STEREO|AV_CH_BACK_LEFT|AV_CH_BACK_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_QUAD = 51,

        /// <summary>
        ///     AV_CH_LAYOUT_STEREO = (AV_CH_FRONT_LEFT|AV_CH_FRONT_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_STEREO = 3,

        /// <summary>
        ///     AV_CH_LAYOUT_STEREO_DOWNMIX = (AV_CH_STEREO_LEFT|AV_CH_STEREO_RIGHT)
        /// </summary>
        AV_CH_LAYOUT_STEREO_DOWNMIX = 1610612736,

        /// <summary>
        ///     AV_CH_LAYOUT_SURROUND = (AV_CH_LAYOUT_STEREO|AV_CH_FRONT_CENTER)
        /// </summary>
        AV_CH_LAYOUT_SURROUND = 7,

        /// <summary>
        ///     AV_CH_LOW_FREQUENCY = 0x00000008
        /// </summary>
        AV_CH_LOW_FREQUENCY = 8,

        /// <summary>
        ///     AV_CH_LOW_FREQUENCY_2 = 0x0000000800000000ULL
        /// </summary>
        AV_CH_LOW_FREQUENCY_2 = 34359738368uL,

        /// <summary>
        ///     AV_CH_SIDE_LEFT = 0x00000200
        /// </summary>
        AV_CH_SIDE_LEFT = 512,

        /// <summary>
        ///     AV_CH_SIDE_RIGHT = 0x00000400
        /// </summary>
        AV_CH_SIDE_RIGHT = 1024,

        /// <summary>
        ///     AV_CH_STEREO_LEFT = 0x20000000
        /// </summary>
        AV_CH_STEREO_LEFT = 536870912,

        /// <summary>
        ///     AV_CH_STEREO_RIGHT = 0x40000000
        /// </summary>
        AV_CH_STEREO_RIGHT = 1073741824,

        /// <summary>
        ///     AV_CH_SURROUND_DIRECT_LEFT = 0x0000000200000000ULL
        /// </summary>
        AV_CH_SURROUND_DIRECT_LEFT = 8589934592uL,

        /// <summary>
        ///     AV_CH_SURROUND_DIRECT_RIGHT = 0x0000000400000000ULL
        /// </summary>
        AV_CH_SURROUND_DIRECT_RIGHT = 17179869184uL,

        /// <summary>
        ///     AV_CH_TOP_BACK_CENTER = 0x00010000
        /// </summary>
        AV_CH_TOP_BACK_CENTER = 65536,

        /// <summary>
        ///     AV_CH_TOP_BACK_LEFT = 0x00008000
        /// </summary>
        AV_CH_TOP_BACK_LEFT = 32768,

        /// <summary>
        ///     AV_CH_TOP_BACK_RIGHT = 0x00020000
        /// </summary>
        AV_CH_TOP_BACK_RIGHT = 131072,

        /// <summary>
        ///     AV_CH_TOP_CENTER = 0x00000800
        /// </summary>
        AV_CH_TOP_CENTER = 2048,

        /// <summary>
        ///     AV_CH_TOP_FRONT_CENTER = 0x00002000
        /// </summary>
        AV_CH_TOP_FRONT_CENTER = 8192,

        /// <summary>
        ///     AV_CH_TOP_FRONT_LEFT = 0x00001000
        /// </summary>
        AV_CH_TOP_FRONT_LEFT = 4096,

        /// <summary>
        ///     AV_CH_TOP_FRONT_RIGHT = 0x00004000
        /// </summary>
        AV_CH_TOP_FRONT_RIGHT = 16384,

        /// <summary>
        ///     AV_CH_WIDE_LEFT = 0x0000000080000000ULL
        /// </summary>
        AV_CH_WIDE_LEFT = 2147483648uL,

        /// <summary>
        ///     AV_CH_WIDE_RIGHT = 0x0000000100000000ULL
        /// </summary>
        AV_CH_WIDE_RIGHT = 4294967296uL,
    }
}