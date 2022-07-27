using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class ComObjectView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ComObjectView), new PropertyMetadata(OnVersionChangedCallback));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(ComObjectView), new PropertyMetadata(OnModuleChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }
        
        public ComObjectView()
		{
            InitializeComponent();
        }
        
        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ComObjectView)?.OnVersionChanged();
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ComObjectView)?.OnModuleChanged();
        }

        protected virtual void OnModuleChanged()
        {
            if(Module == null) return;
            TextFilter filter = new TextFilter(Module.ComObjects, query);
        }
        
        protected virtual void OnVersionChanged() {
            //InType.ItemsSource = Version?.ParameterTypes;
            //InMemory.ItemsSource = Version?.Memories;
        }
        
        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Models.ComObject com = new Models.ComObject() { UId = AutoHelper.GetNextFreeUId(Module.ComObjects) };
            foreach(Models.Language lang in Version.Languages) {
                com.Text.Add(new Models.Translation(lang, "Dummy"));
                com.FunctionText.Add(new Models.Translation(lang, "Dummy"));
            }
            Module.ComObjects.Add(com);

            if(Version.IsComObjectRefAuto){
                Models.ComObjectRef cref = new Models.ComObjectRef(com) { UId = AutoHelper.GetNextFreeUId(Module.ComObjectRefs) };
                foreach(Models.Language lang in Version.Languages) {
                    cref.Text.Add(new Models.Translation(lang, ""));
                    cref.FunctionText.Add(new Models.Translation(lang, ""));
                }
                Module.ComObjectRefs.Add(cref);
            }
        }

        private void ClickClone(object sender, RoutedEventArgs e)
        {
            Models.ComObject com = ComobjectList.SelectedItem as Models.ComObject;
            Models.ComObject clonedCom = com.Copy();
            clonedCom.Id = -1;
            clonedCom.UId = AutoHelper.GetNextFreeUId(Module.ComObjects);

            Module.ComObjects.Add(clonedCom);

            if (Version.IsComObjectRefAuto)
            {
                Models.ComObjectRef cref = new Models.ComObjectRef(clonedCom) { UId = AutoHelper.GetNextFreeUId(Module.ComObjectRefs) };
                foreach (Models.Language lang in Version.Languages)
                {
                    cref.Text.Add(new Models.Translation(lang, ""));
                    cref.FunctionText.Add(new Models.Translation(lang, ""));
                }
                Module.ComObjectRefs.Add(cref);
            }
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Models.ComObject com = ComobjectList.SelectedItem as Models.ComObject;
            Module.ComObjects.Remove(com);

            if(Version.IsComObjectRefAuto){
                foreach(ComObjectRef cref in Module.ComObjectRefs.Where(c => c.ComObjectObject == com).ToList())
                    Module.ComObjectRefs.Remove(cref);
            }
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.ComObject).Id = -1;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

                #region DragNDrop

        private ComObject _draggedItem;
        private ComObject _target;

        private void ListMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is ListBox && e.LeftButton == MouseButtonState.Pressed)
            {
                _draggedItem = (ComObject)ComobjectList.SelectedItem;
                if (_draggedItem != null)
                {
                    DragDropEffects finalDropEffect = DragDrop.DoDragDrop(ComobjectList, ComobjectList.SelectedValue, DragDropEffects.Move);
                    //Checking target is not null and item is
                    //dragging(moving) and move drop was accepted
                    
                    if ((finalDropEffect == DragDropEffects.Move) && (_target != null) && (_draggedItem != _target))
                    {
                        //TODO decide to insert above or below by pressing shift?
                        Module.ComObjects.Remove(_draggedItem);
                        Module.ComObjects.Insert(Module.ComObjects.IndexOf(_target), _draggedItem);

                        _target = null;
                        _draggedItem = null;
                    }
                }
            }
        }

        private void ListDragOver(object sender, DragEventArgs e)
        {
            if(sender != ComobjectList)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            ComObject item = GetNearestContainer(e.OriginalSource);

            if(item == null)
            {
                System.Diagnostics.Debug.WriteLine(e.OriginalSource.GetType().ToString());
                e.Effects = DragDropEffects.None;
            } else {
                e.Effects = DragDropEffects.Move;
            }
            
            e.Handled = true;
        }

        private void ListDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            // Verify that this is a valid drop and then store the drop target
            ComObject TargetItem = GetNearestContainer(e.OriginalSource);
            if (TargetItem != null && _draggedItem != null)
            {
                _target = TargetItem;
                e.Effects = DragDropEffects.Move;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private ComObject GetNearestContainer(object source)
        {
            ComObject item = (source as System.Windows.Documents.Run)?.DataContext as ComObject;

            if(item == null)
                item = (source as System.Windows.Controls.Border)?.DataContext as ComObject;

            if(item == null)
                item = (source as System.Windows.Controls.Image)?.DataContext as ComObject;

            if(item == null)
                item = (source as System.Windows.Controls.TextBlock)?.DataContext as ComObject;
            return item;
        }

        #endregion
    }
}