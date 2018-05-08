﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace EPBLib.Helpers
{
    public static class BinaryReaderExtensions
    {
        #region EpBlueprint
        public static readonly UInt32 EpbIdentifier = 0x78945245;
        public static readonly byte[] ZipDataStartPattern = new byte[] { 0x00, 0x00, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00, 0x08, 0x00 };


        public static EpBlueprint ReadEpBlueprint(this BinaryReader reader, ref long bytesLeft)
        {

            UInt32 identifier = reader.ReadUInt32();
            if (identifier != EpbIdentifier)
            {
                throw new Exception($"Unknown file identifier. 0x{identifier:x4}");
            }
            UInt32 version = reader.ReadUInt32();
            EpBlueprint.EpbType type = (EpBlueprint.EpbType)reader.ReadByte();
            UInt32 width = reader.ReadUInt32();
            UInt32 height = reader.ReadUInt32();
            UInt32 depth = reader.ReadUInt32();
            bytesLeft -= 4 + 4 + 1 + 4 + 4 + 4;
            Console.WriteLine($"Version:  {version}");
            Console.WriteLine($"Type:     {type}");
            Console.WriteLine($"Width:    {width}");
            Console.WriteLine($"Height:   {height}");
            Console.WriteLine($"Depth:    {depth}");

            EpBlueprint epb = new EpBlueprint(type, width, height, depth);

            UInt16 unknown01 = reader.ReadUInt16();
            bytesLeft -= 2;

            Console.WriteLine($"Unkown01: {unknown01}");

            epb.MetaTags = reader.ReadEpMetaTagDictionary(ref bytesLeft);
            foreach (EpMetaTag tag in epb.MetaTags.Values)
            {
                Console.WriteLine(tag.ToString());
            }


            // TODO: Funky
            int nUnknown02;
            if (version <= 4)
            {
                nUnknown02 = 33;
            }
            else if (version <= 12)
            {
                nUnknown02 = 36;
            }
            else if (version <= 17)
            {
                nUnknown02 = 26;
            }
            else
            {
                nUnknown02 = 30;
            }
            byte[] unknown02 = reader.ReadBytes(nUnknown02);
            bytesLeft -= nUnknown02;
            Console.WriteLine($"Unknown02: {BitConverter.ToString(unknown02).Replace("-", "")}");

            if (version > 12)
            {
                UInt16 nUnknown04 = reader.ReadUInt16();
                bytesLeft -= 2;
                int bytesToRead = nUnknown04 * 6 - 4; // First value is 2 bytes, the rest are 6 bytes each
                byte[] unknown04 = reader.ReadBytes(bytesToRead);
                bytesLeft -= bytesToRead;
                Console.WriteLine($"Unknown04: {nUnknown04:x4} {BitConverter.ToString(unknown04).Replace("-", "")}");
            }

            byte[] unknown05 = reader.ReadBytes(5);
            bytesLeft -= 5;
            Console.WriteLine($"Unknown05: {BitConverter.ToString(unknown05).Replace("-", "")}");

            epb.DeviceGroups = reader.ReadEpbDeviceGroups(ref bytesLeft);
            Console.WriteLine($"DeviceGroups ({epb.DeviceGroups.Count}):");
            foreach (EpbDeviceGroup group in epb.DeviceGroups)
            {
                Console.WriteLine($"    {group.Name} (Flags=0x{group.Flags:x4})");
                foreach (EpbDevice device in group.Devices)
                {
                    Console.WriteLine($"        {device.Unknown:x8} {device.Name}");
                }
            }

            //UInt16 nGroups = reader.ReadUInt16();
            //bytesLeft -= 2;
            //Console.WriteLine($"Groups ({nGroups})");
            //for (int g = 0; g < nGroups; g++)
            //{
            //    string groupName = reader.ReadEpString(ref bytesLeft);
            //    UInt16 groupUnknown01 = reader.ReadUInt16();
            //    UInt16 nDevicesInGroup = reader.ReadUInt16();
            //    bytesLeft -= 2 + 2;
            //    Console.WriteLine($"    {groupName} ({groupUnknown01:x4})");
            //    for (int d = 0; d < nDevicesInGroup; d++)
            //    {
            //        UInt32 deviceUnknown01 = reader.ReadUInt32();
            //        bytesLeft -= 4;
            //        string deviceName = reader.ReadEpString(ref bytesLeft);
            //        Console.WriteLine($"        device: {deviceUnknown01:x8} \"{deviceName}\"");
            //    }
            //}

            // There might be a number of unparsed bytes remaining at this point, so read the rest and search for the PKZip header:
            byte[] buf = reader.ReadBytes((int)bytesLeft);
            int dataStart = buf.IndexOf(ZipDataStartPattern);
            if (dataStart == -1)
            {
                throw new Exception("ReadHeader: Unable to locate ZipDataStart.");
            }
            byte[] unknown8 = buf.Take(dataStart).ToArray();
            bytesLeft -= dataStart;
            Console.WriteLine($"BeforeZIP: {BitConverter.ToString(unknown8).Replace("-", "")}");

            byte[] zippedData = buf.Skip(dataStart).Take((int)bytesLeft).ToArray();
            zippedData[0] = 0x50;
            zippedData[1] = 0x4b;
            using (ZipFile zf = new ZipFile(new MemoryStream(zippedData)))
            {
                zf.IsStreamOwner = true;
                foreach (ZipEntry entry in zf)
                {
                    if (!entry.IsFile || entry.Name != "0")
                    {
                        Console.WriteLine($"Skipping ZIP entry: {entry.Name} ({entry.Size} bytes)");
                        continue;
                    }

                    byte[] zipBuffer = new byte[4096];
                    Stream zipStream = zf.GetInputStream(entry);

                    using (BinaryReader zipReader = new BinaryReader(zipStream))
                    {
                        zipReader.ReadEpbBlocks(epb, version, entry.Size);
                    }
                }
            }

            return epb;
        }
        #endregion EpBlueprint

        #region EpbBlocks
        public static void ReadEpbBlocks(this BinaryReader reader, EpBlueprint epb, UInt32 version, long length)
        {
            long bytesLeft = length;
            int blockCount = 0;
            bytesLeft = reader.ReadEpbMatrix(epb, "Blocks", length, (r, e, x, y, z, b) =>
            {
                byte type = r.ReadByte();
                byte rotation = r.ReadByte();
                byte unknown2 = r.ReadByte();
                byte variant = r.ReadByte();
                blockCount++;
                Console.WriteLine($"    {blockCount} ({x}, {y}, {z}): Type=0x{type:x2} Rot=0x{rotation:x2} Unknown2=0x{unknown2:x2} Variant=0x{variant:x2}");
                return b - 4;
            });

            int unknown3Count = 0;
            bytesLeft = reader.ReadEpbMatrix(epb, "Unknown3", length, (r, e, x, y, z, b) =>
            {
                byte unknown31 = r.ReadByte();
                byte unknown32 = r.ReadByte();
                byte unknown33 = r.ReadByte();
                byte unknown34 = r.ReadByte();
                unknown3Count++;
                Console.WriteLine($"    {unknown3Count} ({x}, {y}, {z}): 0x{unknown31:x2} 0x{unknown32:x2} 0x{unknown33:x2} 0x{unknown34:x2}");
                return b - 4;
            });

            int nUnknown4 = reader.ReadByte();
            bytesLeft -= 1;
            if (nUnknown4 == 0)
            {
                nUnknown4 = (int)(epb.Width * epb.Height * epb.Depth); // blockCount;
            }
            byte[] unknown4 = reader.ReadBytes(nUnknown4);
            bytesLeft -= nUnknown4;
            Console.WriteLine($"Unknown4: {BitConverter.ToString(unknown4).Replace("-", "")}");

            int colourCount = 0;
            bytesLeft = reader.ReadEpbMatrix(epb, "Colour", length, (r, e, x, y, z, b) =>
            {
                UInt32 bits = r.ReadUInt32();
                byte[] colours = new byte[6];
                for (int i = 0; i < 6; i++)
                {
                    colours[i] = (byte)(bits & 0x1f);
                    bits = bits >> 5;
                }
                colourCount++;
                Console.WriteLine($"    {colourCount} ({x}, {y}, {z}): {string.Join(", ", colours)}");
                return b - 4;
            });

            int textureCount = 0;
            bytesLeft = reader.ReadEpbMatrix(epb, "Texture", length, (r, e, x, y, z, b) =>
            {
                UInt64 bits = r.ReadUInt64();
                byte[] textures = new byte[6];
                for (int i = 0; i < 6; i++)
                {
                    textures[i] = (byte)(bits & 0x3f);
                    bits = bits >> 6;
                }
                textureCount++;
                Console.WriteLine($"    {textureCount} ({x}, {y}, {z}): {string.Join(", ", textures)}");
                return b - 8;
            });

            if (version >= 20)
            {
                int unknown7Count = 0;
                bytesLeft = reader.ReadEpbMatrix(epb, "Unknown7", length, (r, e, x, y, z, b) =>
                {
                    byte unknown71 = r.ReadByte();
                    unknown7Count++;
                    Console.WriteLine($"    {unknown7Count} ({x}, {y}, {z}): 0x{unknown71:x2}");
                    return b - 1;
                });
            }
            else
            {
                int unknown7Count = 0;
                bytesLeft = reader.ReadEpbMatrix(epb, "Unknown7", length, (r, e, x, y, z, b) =>
                {
                    UInt32 unknown71 = r.ReadUInt32();
                    unknown7Count++;
                    Console.WriteLine($"    {unknown7Count} ({x}, {y}, {z}): 0x{unknown71:x8}");
                    return b - 4;
                });
            }

            int symbolCount = 0;
            bytesLeft = reader.ReadEpbMatrix(epb, "Symbol", length, (r, e, x, y, z, b) =>
            {
                UInt32 bits = r.ReadUInt32();
                byte[] symbols = new byte[6];
                for (int i = 0; i < 6; i++)
                {
                    symbols[i] = (byte)(bits & 0x1f);
                    bits = bits >> 5;
                }
                byte symbolPage = (byte)bits;
                symbolCount++;
                Console.WriteLine($"    {symbolCount} ({x}, {y}, {z}): Page={symbolPage} {string.Join(", ", symbols)}");
                return b - 4;
            });

            if (version >= 20) // TODO: I have no idea when this appeared
            {
                int unknown9Count = 0;
                bytesLeft = reader.ReadEpbMatrix(epb, "Unknown9", length, (r, e, x, y, z, b) =>
                {
                    byte unknown91 = r.ReadByte();
                    byte unknown92 = r.ReadByte();
                    byte unknown93 = r.ReadByte();
                    byte unknown94 = r.ReadByte();
                    unknown9Count++;
                    Console.WriteLine($"    {unknown9Count} ({x}, {y}, {z}): 0x{unknown91:x2} 0x{unknown92:x2} 0x{unknown93:x2} 0x{unknown94:x2}");
                    return b - 4;
                });
            }

            byte[] remainingData = reader.ReadBytes((int)(bytesLeft));
            Console.WriteLine($"Remaining data: {BitConverter.ToString(remainingData).Replace("-", "")}");
        }

        public static long ReadEpbMatrix(this BinaryReader reader, EpBlueprint epb, string name, long bytesLeft, Func<BinaryReader, EpBlueprint, int, int, int, long, long> func)
        {
            UInt32 matrixSize = reader.ReadUInt32();
            byte[] matrix = reader.ReadBytes((int)matrixSize);
            bytesLeft -= 4;
            Console.WriteLine($"{name} Matrix: {BitConverter.ToString(matrix).Replace("-", "")}");
            if (func == null)
            {
                return bytesLeft;
            }

            bool[] m = matrix.ToBoolArray();
            for (int z = 0; z < epb.Depth; z++)
            {
                for (int y = 0; y < epb.Height; y++)
                {
                    for (int x = 0; x < epb.Width; x++)
                    {
                        if (m[z * epb.Width * epb.Height + y * epb.Width + x])
                        {
                            bytesLeft = func(reader, epb, x, y, z, bytesLeft);
                        }
                    }
                }
            }
            return bytesLeft;
        }
        #endregion EpbBlocks

        #region EpMetaTags

        public static Dictionary<EpMetaTagKey, EpMetaTag> ReadEpMetaTagDictionary(this BinaryReader reader, ref long bytesLeft)
        {
            Dictionary<EpMetaTagKey, EpMetaTag> dictionary = new Dictionary<EpMetaTagKey, EpMetaTag>();
            UInt16 nTags = reader.ReadUInt16();
            bytesLeft -= 2;
            for (int i = 0; i < nTags; i++)
            {
                EpMetaTag tag = reader.ReadEpMetaTag(ref bytesLeft);
                dictionary.Add(tag.Key, tag);
            }
            return dictionary;
        }


        public static EpMetaTag ReadEpMetaTag(this BinaryReader reader, ref long bytesLeft)
        {
            EpMetaTagKey key   = (EpMetaTagKey)reader.ReadInt32();
            EpMetaTagType type = (EpMetaTagType)reader.ReadInt32();
            bytesLeft -= 8;

            EpMetaTag tag;
            switch (type)
            {
                case EpMetaTagType.String:
                    EpMetaTagString tagString = new EpMetaTagString(key);
                    tagString.Value = reader.ReadEpString(ref bytesLeft);
                    tag = tagString;
                    break;
                case EpMetaTagType.Unknownx01:
                    EpMetaTag01 tag01 = new EpMetaTag01(key);
                    tag01.Value = reader.ReadUInt16();
                    bytesLeft -= 2;
                    tag = tag01;
                    break;
                case EpMetaTagType.Unknownx02:
                    EpMetaTag02 tag02 = new EpMetaTag02(key);
                    tag02.Value = reader.ReadBytes(5);
                    bytesLeft -= 5;
                    tag = tag02;
                    break;
                case EpMetaTagType.Unknownx03:
                    EpMetaTag03 tag03 = new EpMetaTag03(key);
                    tag03.Value = reader.ReadBytes(5);
                    bytesLeft -= 5;
                    tag = tag03;
                    break;
                case EpMetaTagType.Unknownx04:
                    EpMetaTag04 tag04 = new EpMetaTag04(key);
                    tag04.Value = reader.ReadBytes(13);
                    bytesLeft -= 13;
                    tag = tag04;
                    break;
                case EpMetaTagType.Unknownx05:
                    EpMetaTag05 tag05 = new EpMetaTag05(key);
                    tag05.Value = reader.ReadBytes(9);
                    bytesLeft -= 9;
                    tag = tag05;
                    break;
                default:
                    tag = null;
                    break;
            }
            return tag;
        }
        #endregion EpMetaTags

        #region EpbDevices

        public static List<EpbDeviceGroup> ReadEpbDeviceGroups(this BinaryReader reader, ref long bytesLeft)
        {
            List<EpbDeviceGroup> groups = new List<EpbDeviceGroup>();
            UInt16 nGroups = reader.ReadUInt16();
            bytesLeft -= 2;
            for (int i = 0; i < nGroups; i++)
            {
                groups.Add(reader.ReadEpbDeviceGroup(ref bytesLeft));
            }
            return groups;
        }
        public static EpbDeviceGroup ReadEpbDeviceGroup(this BinaryReader reader, ref long bytesLeft)
        {
            EpbDeviceGroup group = new EpbDeviceGroup();
            group.Name = reader.ReadEpString(ref bytesLeft);
            group.Flags = reader.ReadUInt16();
            bytesLeft -= 2;
            UInt16 nDevices = reader.ReadUInt16();
            bytesLeft -= 2;
            for (int i = 0; i < nDevices; i++)
            {
                group.Devices.Add(reader.ReadEpbDevice(ref bytesLeft));
            }
            return group;
        }
        public static EpbDevice ReadEpbDevice(this BinaryReader reader, ref long bytesLeft)
        {
            EpbDevice device = new EpbDevice();
            device.Unknown = reader.ReadUInt32();
            bytesLeft -= 4;
            device.Name = reader.ReadEpString(ref bytesLeft);
            return device;
        }

        #endregion EpbDevices

        public static string ReadEpString(this BinaryReader reader, ref long bytesLeft)
        {
            byte len = reader.ReadByte();
            string s = (len == 0) ? "" : System.Text.Encoding.ASCII.GetString(reader.ReadBytes(len));
            bytesLeft -= 1 + len;
            return s;
        }


    }
}
