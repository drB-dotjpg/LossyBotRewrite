﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using System.Linq;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Reflection;

namespace LossyBotRewrite
{
    [Group("image")]
    public class ImageModule : ModuleBase<SocketCommandContext>
    {
        [Command]
        public async Task ImageCommand(params string[] args)
        {
            string url = args.Last();
            Array.Resize(ref args, args.Length - 1); //remove the last element

            Stopwatch watch = new Stopwatch();
            watch.Start();
            IImageWrapper img = await ProcessImageAsync(url, args);
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);
            using (var stream = new MemoryStream())
            {
                img.Write(stream);
                var im = new MagickImage();
                stream.Position = 0;
                await Context.Channel.SendFileAsync(stream, "lossyimage." + img.GetFormat().ToString().ToLower());
            }
        }

        private async Task<byte[]> DownloadImageAsync(string url)
        {
            byte[] data = await Globals.httpClient.GetByteArrayAsync(url);
            return data;
        }

        public async Task<IImageWrapper> ProcessImageAsync(string url, string[] args)
        {
            IImageWrapper img;

            if (url.Contains(".gif"))
                img = new GifWrapper(await DownloadImageAsync(url));
            else
                img = new ImageWrapper(await DownloadImageAsync(url));

            foreach (var effect in args)
            {
                switch (effect.ToLower())
                {
                    case "magik":
                        img.Magik();
                        break;
                    case "edge":
                        img.Edge();
                        break;
                    case "wave":
                        img.Wave();
                        break;
                    case "deepfry":
                        img.Deepfry();
                        break;
                    case "jpgify":
                        img.Jpgify();
                        break;
                    case "waaw":
                        img.Waaw();
                        break;
                    case "haah":
                        img.Haah();
                        break;
                    case "contrast":
                        img.Contrast();
                        break;
                    case "negate":
                        img.Negate();
                        break;
                    case "bulge":
                        img.Bulge();
                        break;
                    case "implode":
                        img = img.Implode();
                        break;
                    case "drift":
                        img = img.Drift();
                        break;
                    case "expand":
                        img = img.Expand();
                        break;
                    case "explode":
                        img = img.Explode();
                        break;
                    case "dance":
                        img = img.Dance();
                        break;
                    case "angry":
                        img = img.Angry();
                        break;
                    case "spectrum":
                        img = img.Spectrum();
                        break;
                    case "lsd":
                        img = img.Lsd();
                        break;
                    default:
                        await ReplyAsync($"`Invalid effect '{effect}'.`");
                        img.Dispose();
                        throw new Exception("Invalid effect");
                }
            }

            return img;
        }
    }
}
