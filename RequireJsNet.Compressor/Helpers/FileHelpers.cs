﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequireJsNet.Compressor.Helpers
{
    internal static class FileHelpers
    {
        public static Encoding ParseEncoding(string encoding)
        {
            if (string.IsNullOrEmpty(encoding))
            {
                return Encoding.Default;
            }

            switch (encoding.ToLowerInvariant())
            {
                case "ascii":
                    return Encoding.ASCII;
                case "bigendianunicode":
                    return Encoding.BigEndianUnicode;
                case "unicode":
                    return Encoding.Unicode;
                case "utf32":
                case "utf-32":
                    return Encoding.UTF32;
                case "utf7":
                case "utf-7":
                    return Encoding.UTF7;
                case "utf8":
                case "utf-8":
                    return Encoding.UTF8;
                case "default":
                    return Encoding.Default;
                default:
                    throw new ArgumentException("Encoding: " + encoding + " is invalid.", "EncodingType");
            }
        }
    }
}
