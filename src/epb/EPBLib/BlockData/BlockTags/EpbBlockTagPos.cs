﻿
namespace EPBLib
{
    public class EpbBlockTagPos : EpbBlockTag
    {
        public EpbBlockTagPos()
        {
            BlockTagType = TagType.UInt32;
            Name = "Pos";
        }

        public EpbBlockTagPos(EpbBlockPos value) : this()
        {
            Value = value;
        }

        public EpbBlockPos Value { get; set; }

        public override string ToString()
        {
            return $"{Name,-14}: {Value}";
        }
    }
}
