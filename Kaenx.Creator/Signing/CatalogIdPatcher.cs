using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Kaenx.Creator.Signing
{
    class CatalogIdPatcher
    {
        public CatalogIdPatcher(
            FileInfo catalogFile,
            IDictionary<string, string> hardware2ProgramIdMapping)
        {
            Assembly asm = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Knx.Ets.XmlSigning.dll"));
            _instance = Activator.CreateInstance(asm.GetType("Knx.Ets.XmlSigning.CatalogIdPatcher"), catalogFile, hardware2ProgramIdMapping);
            _type = asm.GetType("Knx.Ets.XmlSigning.CatalogIdPatcher");
        }

        public void Patch()
        {
            _type.GetMethod("Patch", BindingFlags.Instance | BindingFlags.Public).Invoke(_instance, null);
        }

        private readonly object _instance;
        private readonly Type _type;
    }
}