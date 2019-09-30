﻿using MonoGame.Framework;
using MonoGame.Utilities.Memory;
using StbSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using MonoGame.Utilities;

namespace MonoGame.Imaging.Tests
{
    unsafe class Program
    {
        public const string DataZip = "testdata.zip";

        static void Main(string[] args)
        {
            var archive = new ZipArchive(File.OpenRead(DataZip), ZipArchiveMode.Read, false);
            //var stream = archive.GetEntry("png/32bit.png").Open();
            //var stream = archive.GetEntry("bmp/32bit.bmp").Open();

            var encoded = new MemoryStream(1024 * 1024 * 8);
            using(var stream = new FileStream("big img.png", FileMode.Open))
                stream.CopyTo(encoded);

            //var encoded = new FileStream("big img.png", FileMode.Open);

            //var req = WebRequest.CreateHttp("https://cdn.discordapp.com/attachments/290820360335523840/551171768090492938/unknown.png");
            //var encoded = req.GetResponse().GetResponseStream();
            //encoded = RecyclableMemoryManager.Default.GetBufferedStream(encoded, false);

            int readRepeats = 1;
            int writeRepeats = 1;

            bool OnReadProgress(
                int frameIndex, FrameCollection<Color> frames, double progress, Rectangle? rectangle)
            {
                Console.WriteLine("Read: " + Math.Round(progress * 100, 2) + "%");
                return false;
            }
            
            var watch = new Stopwatch();
            Image<Color> image = null;
            for (int i = 0; i < readRepeats; i++)
            {
                encoded.Seek(0, SeekOrigin.Begin);
                if (readRepeats == 1 || i != 0)
                    watch.Start();

                image?.Dispose();
                image = Image.Load<Color>(encoded, OnReadProgress);
                watch.Stop();
            }
            Console.WriteLine(image.Width + "x" + image.Height + " # " + image.GetBitDepth());
            Console.WriteLine("Buffer Read Average: " +
                Math.Round(watch.Elapsed.TotalMilliseconds / (readRepeats == 1 ? 1 : readRepeats - 1), 3) + "ms");

            Thread.Sleep(500);

            watch.Reset();

            bool OnWriteProgress(
                int frameIndex, ReadOnlyFrameCollection<Color> frames, double progress)
            {
                //Console.WriteLine("Write: " + Math.Round(progress * 100, 2) + "%");
                return true;
            }

            var result = new MemoryStream(1024 * 1024 * 85);
            var writeFormat = ImageFormat.Png;
            for (int i = 0; i < writeRepeats; i++)
            {
                result.Seek(0, SeekOrigin.Begin);
                if (writeRepeats == 1 || i != 0)
                    watch.Start();

                image.Save(result, ImageFormat.Png, null, OnWriteProgress);
                watch.Stop();
            }
            image.Dispose();

            using (var fs = new FileStream("recoded" + writeFormat.Extension, FileMode.Create))
            {
                result.Seek(0, SeekOrigin.Begin);
                result.CopyTo(fs);
            }
            Console.WriteLine("Write Average: " +
                Math.Round(watch.Elapsed.TotalMilliseconds / (writeRepeats == 1 ? 1 : writeRepeats - 1), 3) + "ms");

            GC.Collect();

            Console.ReadKey();

            // terraria party: "https://preview.redd.it/2ws638caxh421.png?auto=webp&s=07157d5f791ebec998d2793ff384aa6f8c67a638"
            // nyoom dog: "https://cdn.discordapp.com/attachments/290820360335523840/551171768090492938/unknown.png"

            //string request = "https://cdn.discordapp.com/attachments/290820360335523840/551171768090492938/unknown.png";
            //var req = WebRequest.CreateHttp(request);
            //
            //Console.WriteLine("Requested " + request);
            //Console.WriteLine();
            //
            //using (var rep = req.GetResponse())
            //{
            //    Console.WriteLine("Got response");
            //
            //    using (var repStream = rep.GetResponseStream())
            //    using (var img1 = new Image(repStream, ImagePixelFormat.Rgb))
            //    {
            //        Console.Write("Writing... ");
            //        using (var fs = File.OpenWrite("im done netted.png"))
            //            img1.Save(fs, ImageSaveFormat.Png);
            //        Console.WriteLine("Done");
            //    }
            //}
            //Console.WriteLine();
            //
            //Console.WriteLine("Copying net response");
            //using (var img1 = new Image(File.OpenRead("im done netted.png"), ImagePixelFormat.Rgb))
            //{
            //    Console.Write("Writing... ");
            //    using (var fs = File.OpenWrite("im done.png"))
            //        img1.Save(fs, ImageSaveFormat.Png);
            //    Console.WriteLine("Done");
            //}
            //
            //Console.ReadKey();
            //
            //return;

            //var d = new SaveConfiguration(true, 0, RecyclableMemoryManager.Instance);
            //var nonD = new SaveConfiguration(false, 0, RecyclableMemoryManager.Instance);
            //var ms = new MemoryStream();
            //
            //TestEntry(ms, d, archive, "bmp/8bit.bmp");
            //TestEntry(ms, d, archive, "bmp/24bit.bmp");
            //
            //TestEntry(ms, d, archive, "jpg/quality_0.jpg");
            //TestEntry(ms, d, archive, "jpg/quality_25.jpg");
            //TestEntry(ms, d, archive, "jpg/quality_50.jpg");
            //TestEntry(ms, d, archive, "jpg/quality_75.jpg");
            //TestEntry(ms, d, archive, "jpg/quality_100.jpg");
            //
            //TestEntry(ms, d, archive, "png/32bit.png");
            //TestEntry(ms, d, archive, "png/24bit.png");
            //TestEntry(ms, d, archive, "png/8bit.png");
            //
            //TestEntry(ms, nonD, archive, "tga/32bit.tga");
            //TestEntry(ms, d, archive, "tga/32bit_compressed.tga");
            //TestEntry(ms, nonD, archive, "tga/24bit.tga");
            //TestEntry(ms, d, archive, "tga/24bit_compressed.tga");
            //
            //Console.WriteLine(RecyclableMemoryManager.Instance.SmallBlocksFree);

            /*
            var watch = new Stopwatch();
            var fs = new FileStream("test.png", FileMode.Open);
            using(var img = new Image(fs, false, manager, true))
            {
                watch.Restart();
                img.GetImageInfo();
                watch.Stop();
                Console.WriteLine("Info: " + watch.Elapsed.TotalMilliseconds + "ms");

                watch.Restart();
                img.GetDataPointer();
                watch.Stop();
                Console.WriteLine("Pointer: " + watch.Elapsed.TotalMilliseconds + "ms");

                using (var outFs = new FileStream("out.png", FileMode.Create))
                {
                    watch.Restart();
                    img.Save(outFs);
                    watch.Stop();
                    Console.WriteLine("Saving: " + watch.Elapsed.TotalMilliseconds + "ms");
                }
            }
            */

            //TestEntry(manager, archive, "32bit.gif");

            archive.Dispose();

            Console.ReadKey();
        }

        //static void TestEntry(MemoryStream ms,
        //    SaveConfiguration config, ZipArchive archive, string name)
        //{
        //    var watch = new Stopwatch();
        //    int tries = 1; //3000;
        //
        //    try
        //    {
        //        var entry = archive.GetEntry(name);
        //        MemoryStream dataStream = new MemoryStream((int)entry.Length);
        //        using (var es = entry.Open())
        //            es.CopyTo(dataStream);
        //
        //        double infoReadTime = 0;
        //        double pointerReadTime = 0;
        //        double imageSaveTime = 0;
        //
        //        for (int i = 0; i < tries; i++)
        //        {
        //            dataStream.Position = 0;
        //            using (var img = new Image(dataStream, true))
        //            {
        //                watch.Restart();
        //                ImageInfo imageInfo = img.Info;
        //                watch.Stop();
        //                if (tries > 0)
        //                    infoReadTime += watch.Elapsed.TotalMilliseconds;
        //
        //                //Console.WriteLine(name + ": " + (img.LastGetInfoFailed ? "Failed to read info" : "Retrieved info successfully"));
        //
        //                //Console.WriteLine($"Loading ({imageInfo}) data...");
        //
        //                watch.Restart();
        //                IntPtr data = img.GetPointer();
        //                watch.Stop();
        //                if (tries > 0)
        //                    pointerReadTime += watch.Elapsed.TotalMilliseconds;
        //
        //                if (data == null)
        //                    Console.WriteLine("Data Pointer NULL: " + img.Errors);
        //                else
        //                {
        //                    //Console.WriteLine("Saving " + img.PointerLength + " bytes...");
        //
        //                    watch.Restart();
        //
        //                    ms.Position = 0;
        //                    ms.SetLength(0);
        //                    img.Save(ms, imageInfo.SourceFormat.ToSaveFormat(), config);
        //
        //                    watch.Stop();
        //                    if (tries > 0)
        //                        imageSaveTime += watch.Elapsed.TotalMilliseconds;
        //                }
        //            }
        //        }
        //
        //        FileInfo outputInfo = new FileInfo("testoutput/" + name);
        //        outputInfo.Directory.Create();
        //        using (var fs = new FileStream(outputInfo.FullName, FileMode.Create))
        //        {
        //            ms.Position = 0;
        //            ms.CopyTo(fs);
        //        }
        //
        //        Console.WriteLine();
        //
        //        Console.WriteLine(name);
        //        Console.WriteLine("Info Read Avg: " + Math.Round(infoReadTime / tries, 2) + "ms");
        //        Console.WriteLine("Pointer Read Avg: " + Math.Round(pointerReadTime / tries, 2) + "ms");
        //        Console.WriteLine("Saving Time Avg: " + Math.Round(imageSaveTime / tries, 2) + "ms");
        //    }
        //    catch (Exception exc)
        //    {
        //        Console.WriteLine(exc);
        //    }
        //
        //    //Console.WriteLine($"Memory Allocated (Arrays: {manager.AllocatedArrays}): " + manager.AllocatedBytes + " bytes");
        //    //Console.WriteLine($"Lifetime Allocated (Arrays: {manager.LifetimeAllocatedArrays}): " + manager.AllocatedBytes + " bytes");
        //    Console.WriteLine("----------------------------------------------------");
        //}
    }
}