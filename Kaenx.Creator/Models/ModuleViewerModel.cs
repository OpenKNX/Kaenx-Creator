using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kaenx.Creator.Models;

public class ModuleViewerModel
{
    public string Name { get; set; }
    public ObservableCollection<Module> Modules { get; set; }


    public ModuleViewerModel(string name, ObservableCollection<Module> modules)
    {
        Name = name;
        Modules = modules;
    }
}