﻿
using EPBLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace EPBLib
{
    public class EpBlueprint
    {
        #region Types
        public enum EpbType
        {
            Voxel         = 0x00,
            Base          = 0x02,
            SmallVessel   = 0x04,
            CapitalVessel = 0x08,
            HoverVessel   = 0x10
        }
        #endregion Types

        #region Properties
        public UInt32 Version { get; protected set; }
        public EpbType Type { get; protected set; }
        public UInt32 Width { get; protected set; }
        public UInt32 Height { get; protected set; }
        public UInt32 Depth { get; protected set; }

        public Dictionary<EpMetaTagKey, EpMetaTag> MetaTags;

        #endregion Properties

        public EpBlueprint (EpbType type, UInt32 width, UInt32 height, UInt32 depth)
        {
            Version     = 20;
            Type        = type;
            Width       = width;
            Height      = height;
            Depth       = depth;
            MetaTags    = new Dictionary<EpMetaTagKey, EpMetaTag>();
        }

    }
}
