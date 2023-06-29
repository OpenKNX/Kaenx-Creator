using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Kaenx.Creator.Signing
{
    class ApplicationProgramHasher
    {
        public ApplicationProgramHasher(
                    FileInfo applProgFile,
                    IDictionary<string, string> mapBaggageIdToFileIntegrity,
                    string basePath,
                    int nsVersion,
                    bool patchIds = true)
        {
            Assembly asm = Assembly.LoadFrom(Path.Combine(basePath, "Knx.Ets.XmlSigning.dll"));
            
            if(asm.GetName().Version.ToString().StartsWith("6.")) { //ab ETS6
                Assembly objm = Assembly.LoadFrom(Path.Combine(basePath, "Knx.Ets.Xml.ObjectModel.dll"));
                object knxSchemaVersion = Enum.ToObject(objm.GetType("Knx.Ets.Xml.ObjectModel.KnxXmlSchemaVersion"), nsVersion);
                _type = asm.GetType("Knx.Ets.XmlSigning.Signer.ApplicationProgramHasher");
                if(asm.GetName().Version.ToString().StartsWith("6.0"))
                    _type = asm.GetType("Knx.Ets.XmlSigning.ApplicationProgramHasher");
                _instance = Activator.CreateInstance(_type, applProgFile, mapBaggageIdToFileIntegrity, patchIds, knxSchemaVersion);
            } else { //für ETS5 und früher
                _type = asm.GetType("Knx.Ets.XmlSigning.ApplicationProgramHasher");
                _instance = Activator.CreateInstance(_type, applProgFile, mapBaggageIdToFileIntegrity, patchIds);
            }
        }

        public void HashFile()
        {
            _type.GetMethod("HashFile", BindingFlags.Instance | BindingFlags.Public).Invoke(_instance, null);
        }

        public string OldApplProgId
        {
            get
            {
                return _type.GetProperty("OldApplProgId", BindingFlags.Public | BindingFlags.Instance).GetValue(_instance).ToString();
            }
        }

        public string NewApplProgId
        {
            get
            {
                return _type.GetProperty("NewApplProgId", BindingFlags.Public | BindingFlags.Instance).GetValue(_instance).ToString();
            }
        }

        public string GeneratedHashString
        {
            get
            {
                return _type.GetProperty("GeneratedHashString", BindingFlags.Public | BindingFlags.Instance).GetValue(_instance).ToString();
            }
        }

        private readonly object _instance;
        private readonly Type _type;
    }
}