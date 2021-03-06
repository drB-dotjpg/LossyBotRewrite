﻿using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace LossyBotRewrite
{
    public class VoiceService
    {
        private readonly DiscordSocketClient _client;
        private ulong id;

        public IVoiceChannel VoiceChannel { get; private set; }
        public Queue<Video> Queue { get; private set; }
        public int FFmpegId { get; private set; }
        public Video CurrentlyPlaying { get; private set; }

        public VoiceService(DiscordSocketClient client, IVoiceChannel channel, ulong guildId)
        {
            _client = client;
            id = guildId;
            VoiceChannel = channel;
            Queue = new Queue<Video>();
        }

        public async Task PlayAudioAsync()
        {
            var audioClient = await VoiceChannel.ConnectAsync(selfDeaf: true);
            while (Queue.Count != 0)
            {
                CurrentlyPlaying = Queue.Dequeue(); //dequeue the latest video
                await DownloadVideo(CurrentlyPlaying); 
                using (var ffmpeg = CreateStream($"{id}.mp3"))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
                {
                    FFmpegId = ffmpeg.Id;
                    try
                    {
                        await output.CopyToAsync(discord);
                    }
                    finally
                    {
                        await discord.FlushAsync();
                        CurrentlyPlaying = null;
                    }
                }
            }
            await audioClient.StopAsync();
        }

        private async Task<int> DownloadVideo(Video video)
        {
            var tcs = new TaskCompletionSource<int>();
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "youtube-dl",
                    Arguments = $"{video.Id} -x --audio-format mp3 -o {id}.%(ext)s",
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };
            process.Start();

            return await tcs.Task;
            //YoutubeClient youtube = new YoutubeClient();
            //var manifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

            //var streamInfo = manifest.GetAudioOnly().WithHighestBitrate();

            //if(streamInfo != null)
            //{
            //    var stream = youtube.Videos.Streams.GetAsync(streamInfo);

            //    await youtube.Videos.Streams.DownloadAsync(streamInfo, $"{id}.mp3");
            //}
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel warning -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
        public void AddToQueue(Video video)
        {
            Queue.Enqueue(video);
        }

        public void AddPlaylistToQueue(IEnumerable<Video> playlist)
        {
            foreach(Video video in playlist)
            {
                Queue.Enqueue(video);
            }
        }

        public void EmptyQueue()
        {
            Queue.Clear();
        }
    }
}
