﻿using System.Threading.Tasks;
using MonoGame.Imaging.Codecs.Detection;
using StbSharp;

namespace MonoGame.Imaging.Codecs.Formats.Bmp
{
    public class BmpImageInfoDetector : StbImageInfoDetectorBase
    {
        public override ImageFormat Format => ImageFormat.Bmp;

        protected override async Task<InfoResult> GetInfo(
            IImagingConfig config, ImageRead.BinReader reader)
        {
            var bmpInfo = await ImageRead.Bmp.Info(reader, out var readState);

            return new InfoResult(readState);
        }
    }
}
