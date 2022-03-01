using System;
using System.Reflection;
using System.IO;

namespace Kaenx.Creator.Signing
{
    class XmlSigning
    {
        public static void SignDirectory(
            string path,
            string basePath,
            bool useCasingOfBaggagesXml = false,
            string[] excludeFileEndings = null)
        {
            Assembly asm = Assembly.LoadFrom(Path.Combine(basePath, "Knx.Ets.XmlSigning.dll"));

            Type ds = asm.GetType("Knx.Ets.XmlSigning.XmlSigning");

            ds.GetMethod("SignDirectory", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { path, useCasingOfBaggagesXml, excludeFileEndings });
        }
    }
}