﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    /// <summary>
    /// <see cref="SwrContext"/> wapper, include a <see cref="AVAudioFifo"/>.
    /// </summary>
    public unsafe class SampleConverter : IFrameConverter, IDisposable
    {
        protected SwrContext* pSwrContext = null;
        public readonly AudioFifo AudioFifo;
        public readonly AVSampleFormat DstFormat;
        public readonly AVChannelLayout DstChannelLayout;
        public readonly int DstNbSamples;
        public readonly int DstSampleRate;
        public MediaFrame DstFrame { get; set; }
        private bool disposedValue;



        protected SampleConverter() { }
        /// <summary>
        /// NOTE: must set <see cref="DstFrame"/> before use.
        /// </summary>
        /// <param name="pSwrContext"></param>
        /// <param name="isDisposeByOwner"></param>
        /// <returns></returns>
        public SampleConverter FromNative(IntPtr pSwrContext, bool isDisposeByOwner = true)
        {
            return new SampleConverter { pSwrContext = (SwrContext*)pSwrContext, disposedValue = !isDisposeByOwner };
        }

        /// <summary>
        /// create audio converter by dst output parames
        /// </summary>
        /// <param name="dstFormat"></param>
        /// <param name="dstChannelLayout">see <see cref="AVChannelLayout"/></param>
        /// <param name="dstNbSamples"></param>
        /// <param name="dstSampleRate"></param>
        public SampleConverter(AVSampleFormat dstFormat, AVChannelLayout dstChannelLayout, int dstNbSamples, int dstSampleRate)
        {
            DstFormat = dstFormat;
            DstChannelLayout = dstChannelLayout;
            DstNbSamples = dstNbSamples;
            DstSampleRate = dstSampleRate;
            DstFrame = MediaFrame.CreateAudioFrame(dstChannelLayout.nb_channels, DstNbSamples, DstFormat, DstSampleRate);
            AudioFifo = new AudioFifo(DstFormat, dstChannelLayout.nb_channels, 1);
        }

        /// <summary>
        /// create audio converter by dst output parames
        /// </summary>
        /// <param name="dstFormat"></param>
        /// <param name="dstChannels"></param>
        /// <param name="dstNbSamples"></param>
        /// <param name="dstSampleRate"></param>
        public SampleConverter(AVSampleFormat dstFormat, int dstChannels, int dstNbSamples, int dstSampleRate)
        {
            DstFormat = dstFormat;
            fixed (AVChannelLayout* p = &DstChannelLayout)
            {
                ffmpeg.av_channel_layout_default(p, dstChannels);

            }
            DstNbSamples = dstNbSamples;
            DstSampleRate = dstSampleRate;
            DstFrame = MediaFrame.CreateAudioFrame(dstChannels, DstNbSamples, DstFormat, DstSampleRate);
            AudioFifo = new AudioFifo(DstFormat, dstChannels);
        }

        /// <summary>
        /// create audio converter by dst codec
        /// </summary>
        /// <param name="dstCodec"></param>
        public static SampleConverter CreateByCodeContext(MediaCodecContext dstCodec)
        {
            if (dstCodec.AVCodecContext.codec_type != AVMediaType.AVMEDIA_TYPE_AUDIO)
                throw new FFmpegException(ffmpeg.AVERROR_INVALIDDATA);
            var DstFormat = dstCodec.AVCodecContext.sample_fmt;
            var DstNbSamples = dstCodec.AVCodecContext.frame_size;
            var DstSampleRate = dstCodec.AVCodecContext.sample_rate;
            var DstChannelLayout = dstCodec.AVCodecContext.ch_layout;
            return new SampleConverter(DstFormat, DstChannelLayout, DstNbSamples, DstSampleRate);
        }

        /// <summary>
        /// create audio converter by dst frame
        /// </summary>
        /// <param name="dstFrame"></param>
        public static SampleConverter CreateByDstFrame(MediaFrame dstFrame)
        {
            var DstFormat = (AVSampleFormat)dstFrame.AVFrame.format;
            var DstChannelLayout = dstFrame.AVFrame.ch_layout;
            var DstNbSamples = dstFrame.AVFrame.nb_samples;
            var DstSampleRate = dstFrame.AVFrame.sample_rate;
            return new SampleConverter(DstFormat, DstChannelLayout, DstNbSamples, DstSampleRate);
        }

        #region safe wapper for IEnumerable

        private void SwrCheckInit(MediaFrame srcFrame)
        {
            if (pSwrContext == null)
            {
                AVFrame* src = srcFrame;
                AVFrame* dst = DstFrame;
       
                fixed(AVChannelLayout* pDst = &DstChannelLayout)
                fixed(SwrContext** ppSwrContext = &pSwrContext)
                {
                     ffmpeg.swr_alloc_set_opts2(ppSwrContext, pDst, DstFormat, DstSampleRate, &src->ch_layout, (AVSampleFormat)src->format, src->sample_rate, 0, null);
                } 
                ffmpeg.swr_init(pSwrContext).ThrowIfError();
            }
        }

        private int FifoPush(MediaFrame srcFrame)
        {
            AVFrame* src = srcFrame;
            AVFrame* dst = DstFrame;
            for (int i = 0, ret = DstNbSamples; ret == DstNbSamples && src != null; i++)
            {
                if (i == 0 && src != null)
                    ret = ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, src->extended_data, src->nb_samples).ThrowIfError();
                else
                    ret = ffmpeg.swr_convert(pSwrContext, dst->extended_data, dst->nb_samples, null, 0).ThrowIfError();
                AudioFifo.Add((void**)dst->extended_data, ret);
            }
            return AudioFifo.Size;
        }

        private MediaFrame FifoPop()
        {
            AVFrame* dst = DstFrame;
            AudioFifo.Read((void**)dst->extended_data, DstNbSamples);
            return DstFrame;
        }

        #endregion safe wapper for IEnumerable

        /// <summary>
        /// Convert <paramref name="srcFrame"/>.
        /// <para>
        /// sometimes audio inputs and outputs are used at different
        /// frequencies and need to be resampled using fifo,
        /// so use <see cref="IEnumerable{T}"/>.
        /// </para>
        /// </summary>
        /// <param name="srcFrame"></param>
        /// <returns></returns>
        public IEnumerable<MediaFrame> Convert(MediaFrame srcFrame)
        {
            SwrCheckInit(srcFrame);
            FifoPush(srcFrame);
            while (AudioFifo.Size >= DstNbSamples)
            {
                yield return FifoPop();
            }
        }

        /// <summary>
        /// convert input audio frame to output frame
        /// </summary>
        /// <param name="srcFrame">input audio frame</param>
        /// <param name="outSamples">number of samples actually output</param>
        /// <param name="cacheSamples">number of samples in the internal cache</param>
        /// <returns></returns>
        public MediaFrame ConvertFrame(MediaFrame srcFrame, out int outSamples, out int cacheSamples)
        {
            SwrCheckInit(srcFrame);
            int curSamples = FifoPush(srcFrame);
            var dstframe = FifoPop();
            cacheSamples = AudioFifo.Size;
            outSamples = curSamples - cacheSamples;
            return dstframe;
        }

        public static implicit operator SwrContext*(SampleConverter value)
        {
            if (value is null) return null;
            return value.pSwrContext;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                fixed (SwrContext** ppSwrContext = &pSwrContext)
                {
                    ffmpeg.swr_free(ppSwrContext);
                }
                AudioFifo.Dispose();
                disposedValue = true;
            }
        }

        ~SampleConverter()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
