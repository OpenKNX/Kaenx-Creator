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
            IDictionary<string, string> hardware2ProgramIdMapping,
            string basePath,
            int nsVersion)
        {
            Assembly asm = Assembly.LoadFrom(Path.Combine(basePath, "Knx.Ets.XmlSigning.dll"));
            
            if(asm.GetName().Version.ToString().StartsWith("6.")) {
                Assembly objm = Assembly.LoadFrom(Path.Combine(basePath, "Knx.Ets.Xml.ObjectModel.dll"));
                object knxSchemaVersion = Enum.ToObject(objm.GetType("Knx.Ets.Xml.ObjectModel.KnxXmlSchemaVersion"), nsVersion);
                _type = asm.GetType("Knx.Ets.XmlSigning.Signer.CatalogIdPatcher");
                if (asm.GetName().Version.ToString().StartsWith("6.0"))
                    _type = asm.GetType("Knx.Ets.XmlSigning.CatalogIdPatcher");
                _instance = Activator.CreateInstance(_type, catalogFile, hardware2ProgramIdMapping, knxSchemaVersion);
            } else {
                _type = asm.GetType("Knx.Ets.XmlSigning.CatalogIdPatcher");
                _instance = Activator.CreateInstance(_type, catalogFile, hardware2ProgramIdMapping);
            }
        }

        public void Patch()
        {
            _type.GetMethod("Patch", BindingFlags.Instance | BindingFlags.Public).Invoke(_instance, null);
        }

        private readonly object _instance;
        private readonly Type _type;
    }
}