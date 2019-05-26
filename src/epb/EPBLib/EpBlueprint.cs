﻿
using EPBLib.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EPBLib.BlockData;
using EPBLib.Logic;
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
        public EpbType Type { get; set; }
        public UInt32 Width { get; protected set; }
        public UInt32 Height { get; protected set; }
        public UInt32 Depth { get; protected set; }
        public UInt16 Unknown01 { get; set; }

        public Dictionary<EpMetaTagKey, EpMetaTag> MetaTags;

        public UInt16 Unknown02 { get; set; }
        public UInt32 LightCount { get; set; }
        public UInt32 UnknownCount01 { get; set; }
        public UInt32 DeviceCount { get; set; }
        public UInt32 UnknownCount02 { get; set; }
        public UInt32 UnknownCount03 { get; set; }
        public UInt32 TriangleCount { get; set; }

        public List<EpbDeviceGroup> DeviceGroups;
        public EpbBlockList Blocks { get; set; }

        public byte[] Unknown07 { get; set; }

        public List<EpbSignalSource> SignalSources = new List<EpbSignalSource>();
        public List<EpbSignalTarget> SignalTargets = new List<EpbSignalTarget>();
        public List<EpbSignalOperator> SignalOperators = new List<EpbSignalOperator>();
        public EpbPalette Palette = new EpbPalette();
        #endregion Properties

        public EpBlueprint (EpbType type = EpbType.Base, UInt32 width = 0, UInt32 height = 0, UInt32 depth = 0)
        {
            Version        = 20;
            Type           = type;
            Width          = width;
            Height         = height;
            Depth          = depth;
            Unknown01      = 1;
            MetaTags       = new Dictionary<EpMetaTagKey, EpMetaTag>();
            Unknown02      = 0;
            LightCount     = 0;
            UnknownCount01 = 0;
            DeviceCount    = 0;
            UnknownCount02 = 0;
            UnknownCount03 = 0;
            TriangleCount  = 0;
            DeviceGroups = new List<EpbDeviceGroup>();

            MetaTags[EpMetaTagKey.UnknownMetax11] = new EpMetaTag03(EpMetaTagKey.UnknownMetax11)     { Value = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }};
            MetaTags[EpMetaTagKey.UnknownMetax01] = new EpMetaTagUInt16(EpMetaTagKey.UnknownMetax01) { Value = 0x0000};
            MetaTags[EpMetaTagKey.UnknownMetax0E] = new EpMetaTag03(EpMetaTagKey.UnknownMetax0E)     { Value = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }};
            MetaTags[EpMetaTagKey.UnknownMetax0F] = new EpMetaTag03(EpMetaTagKey.UnknownMetax0F)     { Value = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }};
            MetaTags[EpMetaTagKey.UnknownMetax05] = new EpMetaTagUInt16(EpMetaTagKey.UnknownMetax05) { Value = 0x0000 };
            MetaTags[EpMetaTagKey.UnknownMetax04] = new EpMetaTag02(EpMetaTagKey.UnknownMetax04)     { Value = 0, Unknown = 0 };
            MetaTags[EpMetaTagKey.UnknownMetax06] = new EpMetaTag04(EpMetaTagKey.UnknownMetax06)     { Value = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }};
            MetaTags[EpMetaTagKey.BlueprintName]  = new EpMetaTagString(EpMetaTagKey.BlueprintName)  { Value = "" };
            MetaTags[EpMetaTagKey.CreationTime]   = new EpMetaTag05(EpMetaTagKey.CreationTime)       { Value = DateTime.Now, Unknown = 0 };
            MetaTags[EpMetaTagKey.BuildVersion]   = new EpMetaTag02(EpMetaTagKey.BuildVersion)       { Value = 1838, Unknown = 0 };
            MetaTags[EpMetaTagKey.CreatorId]      = new EpMetaTagString(EpMetaTagKey.CreatorId)      { Value = "" };
            MetaTags[EpMetaTagKey.CreatorName]    = new EpMetaTagString(EpMetaTagKey.CreatorName)    { Value = "" };
            MetaTags[EpMetaTagKey.OwnerId]        = new EpMetaTagString(EpMetaTagKey.OwnerId)        { Value = "" };
            MetaTags[EpMetaTagKey.OwnerName]      = new EpMetaTagString(EpMetaTagKey.OwnerName)      { Value = "" };
            MetaTags[EpMetaTagKey.DisplayName]    = new EpMetaTagString(EpMetaTagKey.DisplayName)    { Value = "" };
            MetaTags[EpMetaTagKey.UnknownMetax12] = new EpMetaTag05(EpMetaTagKey.UnknownMetax12)     { Value = DateTime.MinValue, Unknown = 0 };
        }

        public void SetBlock(EpbBlock block)
        {
            if (Blocks == null)
            {
                Blocks = new EpbBlockList();
            }

            // TODO: Update blockCounts
            Blocks[block.Position] = block;
        }

        /// <summary>
        /// Sets the dimensions of this blueprint to the minimum that encloses all its blocks.
        /// </summary>
        public void UpdateDimensions()
        {
            UInt32 width = 0;
            UInt32 height = 0;
            UInt32 depth = 0;
            foreach (EpbBlock block in Blocks)
            {
                EpbBlockPos pos = block.Position;
                if (pos.X >= width)
                {
                    width = (UInt32)pos.X + 1;
                }
                if (pos.Y >= height)
                {
                    height = (UInt32)pos.Y + 1;
                }
                if (pos.Z >= depth)
                {
                    depth = (UInt32)pos.Z + 1;
                }
            }
            Width = width;
            Height = height;
            Depth = depth;
        }
    }
}
