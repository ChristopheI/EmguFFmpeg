﻿using System;

using FFmpeg.AutoGen;

namespace EmguFFmpeg
{
    public unsafe class MediaStream
    {

        public static MediaStream FromNative(IntPtr pAVStream)
        {
            return FromNative((AVStream*)pAVStream);
        }

        public static MediaStream FromNative(AVStream* stream)
        {
            return new MediaStream(stream);
        }

        internal MediaStream(AVStream* stream)
        {
            pStream = stream;
        }

        public AVRational TimeBase
        {
            get => pStream->time_base;
            set => pStream->time_base = value;
        }

        public long StartTime
        {
            get => pStream->start_time;
            set => pStream->start_time = value;
        }

        public long Duration
        {
            get => pStream->duration;
            set => pStream->duration = value;
        }

        public long FirstDts
        {
            get => pStream->first_dts;
            set => pStream->first_dts = value;
        }

        public long CurDts
        {
            get => pStream->cur_dts;
            set => pStream->cur_dts = value;
        }

        public AVStream Stream => *pStream;

        /// <summary>
        /// stream index in AVFormatContext
        /// </summary>
        public int Index => pStream->index;

  

        /// <summary>
        /// Convert to TimeSpan use <see cref="TimeBase"/>.
        /// </summary>
        /// <remarks>
        /// throw exception when <paramref name="pts"/> &lt; 0.
        /// </remarks>
        /// <param name="pts"></param>
        /// <exception cref="FFmpegException"/>
        /// <returns></returns>
        public TimeSpan ToTimeSpan(long pts)
        {
            if (pts < 0)
                throw new FFmpegException(FFmpegException.PtsOutOfRange);
            return TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(TimeBase));
        }

        /// <summary>
        /// Convert to TimeSpan use <see cref="TimeBase"/>
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public bool TryToTimeSpan(long pts, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;
            if (pts < 0)
                return false;
            timeSpan = TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(TimeBase));
            return true;
        }

        /// <summary>
        /// [Unsafe]
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator AVStream*(MediaStream value)
        {
            if (value == null) return null;
            return value.pStream;
        }

        private AVStream* pStream = null;
    }
}
