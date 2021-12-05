using System;
using System.Reflection;
using System.IO;

namespace Kaenx.Creator.Signing
{
    class XmlSigning
    {
        public static void SignDirectory(
            string path,
            bool useCasingOfBaggagesXml = false,
            string[] excludeFileEndings = null)
        {
            Assembly asm = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Knx.Ets.XmlSigning.dll"));

            Type ds = asm.GetType("Knx.Ets.XmlSigning.XmlSigning");

            ds.GetMethod("SignDirectory", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { path, useCasingOfBaggagesXml, excludeFileEndings });
        }
    }
}