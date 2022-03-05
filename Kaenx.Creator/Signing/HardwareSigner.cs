using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Kaenx.Creator.Signing
{
    class HardwareSigner
    {
        public HardwareSigner(
                FileInfo hardwareFile,
                IDictionary<string, string> applProgIdMappings,
                IDictionary<string, string> applProgHashes,
                string basePath,
                int nsVersion,
                bool patchIds)
        {
            Assembly asm = Assembly.LoadFrom(Path.Combine(basePath, "Knx.Ets.XmlSigning.dll"));
            Assembly objm = Assembly.LoadFrom(Path.Combine(basePath, "Knx.Ets.Xml.ObjectModel.dll"));

            Type RegistrationKeyEnum = objm.GetType("Knx.Ets.Xml.ObjectModel.RegistrationKey");
            object registrationKey = Enum.Parse(RegistrationKeyEnum, "knxconv");

            if(asm.GetName().Version.ToString().StartsWith("6.0")) {
                object knxSchemaVersion = Enum.ToObject(objm.GetType("Knx.Ets.Xml.ObjectModel.KnxXmlSchemaVersion"), nsVersion);
                _instance = Activator.CreateInstance(asm.GetType("Knx.Ets.XmlSigning.HardwareSigner"), hardwareFile, applProgIdMappings, applProgHashes, patchIds, registrationKey, knxSchemaVersion);
                _type = asm.GetType("Knx.Ets.XmlSigning.HardwareSigner");
            } else {
                _instance = Activator.CreateInstance(asm.GetType("Knx.Ets.XmlSigning.HardwareSigner"), hardwareFile, applProgIdMappings, applProgHashes, patchIds, registrationKey);
                _type = asm.GetType("Knx.Ets.XmlSigning.HardwareSigner");
            }
        }

        public void SignFile()
        {
            _type.GetMethod("SignFile", BindingFlags.Instance | BindingFlags.Public).Invoke(_instance, null);
        }

        private readonly object _instance;
        private readonly Type _type;

        public IDictionary<string, string> OldNewIdMappings
        {
            get
            {
                return (IDictionary<string, string>)_type.GetProperty("OldNewIdMappings", BindingFlags.Public | BindingFlags.Instance).GetValue(_instance);
            }
        }
    }
}