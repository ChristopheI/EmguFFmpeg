﻿using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EmguFFmpeg.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // copy ffmpeg binarys to ./bin
            FFmpegHelper.RegisterBinaries("bin");
            FFmpegHelper.SetupLogging();
            Console.WriteLine("Hello FFmpeg!");

            int a = ffmpeg.AVERROR(ffmpeg.AVERROR_DECODER_NOT_FOUND);
            FFmpegException.GetErrorString(a);

            // No media files provided
            new List<IExample>()
            {
                new DecodeAudio("input.mp3"),
                new CreateVideo("output.mp4"),
                new ReadDevice(),
                new Remuxing("input.mp3"),
                new RtmpPull("rtmp://127.0.0.0/live/stream"),
            }.ForEach(_ =>
            {
                try
                {
                    _.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            });

            Console.ReadKey();
        }
    }
}