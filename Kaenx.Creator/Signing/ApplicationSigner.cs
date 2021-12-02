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
                    bool patchIds = true)
        {
            Assembly asm = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Knx.Ets.XmlSigning.dll"));
            _instance = Activator.CreateInstance(asm.GetType("Knx.Ets.XmlSigning.ApplicationProgramHasher"), applProgFile, mapBaggageIdToFileIntegrity, patchIds);
            _type = asm.GetType("Knx.Ets.XmlSigning.ApplicationProgramHasher");
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