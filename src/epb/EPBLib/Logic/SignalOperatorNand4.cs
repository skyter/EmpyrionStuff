﻿namespace EPBLib.Logic
{
    public class SignalOperatorNand4 : SignalOperator
    {
        public string InSig0 => Tags.ContainsKey("InSig0") && Tags["InSig0"].BlockTagType == BlockTag.TagType.String ? ((BlockTagString)Tags["InSig0"]).Value : "";
        public string InSig1 => Tags.ContainsKey("InSig1") && Tags["InSig1"].BlockTagType == BlockTag.TagType.String ? ((BlockTagString)Tags["InSig1"]).Value : "";
        public string InSig2 => Tags.ContainsKey("InSig2") && Tags["InSig2"].BlockTagType == BlockTag.TagType.String ? ((BlockTagString)Tags["InSig2"]).Value : "";
        public string InSig3 => Tags.ContainsKey("InSig3") && Tags["InSig3"].BlockTagType == BlockTag.TagType.String ? ((BlockTagString)Tags["InSig3"]).Value : "";
    }
}
