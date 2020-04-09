﻿using MonoGame.Imaging.Attributes.Codec;
using StbSharp;
using static StbSharp.ImageRead;

namespace MonoGame.Imaging.Coding.Decoding
{
    public class PngImageDecoder : StbImageDecoderBase
    {
        public override ImageFormat Format => ImageFormat.Png;

        protected override bool ReadFirst(
            StbImageDecoderState decoderState, ref ReadState readState)
        {
            throw new System.Exception("fix me");
            //return Png.Load(decoderState.ReadContext, ref readState);
        }
    }
}
