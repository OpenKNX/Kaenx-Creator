using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class ParameterView : UserControl
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ParameterView), new PropertyMetadata(OnVersionChangedCallback));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(ParameterView), new PropertyMetadata(OnModuleChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }

        public ParameterView()
		{
            InitializeComponent();
        }

        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterView)?.OnVersionChanged();
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterView)?.OnModuleChanged();
        }

        protected virtual void OnVersionChanged() {
            
        }
        
        protected virtual void OnModuleChanged() {
            if(Module == null) return;
            TextFilter filter = new TextFilter(Module.Parameters, query);
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Models.Parameter para = new Models.Parameter() {
                UId = AutoHelper.GetNextFreeUId(Module.Parameters)
            };
            foreach(Models.Language lang in Version.Languages) {
                para.Text.Add(new Models.Translation(lang, "Dummy"));
            }
            Module.Parameters.Add(para);

            if(Version.IsParameterRefAuto){
                Module.ParameterRefs.Add(new Models.ParameterRef(para) { UId = AutoHelper.GetNextFreeUId(Module.ParameterRefs) });
            }
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Parameter para = ParamList.SelectedItem as Models.Parameter;
            Module.Parameters.Remove(para);

            if(Version.IsParameterRefAuto)
            {
                foreach(ParameterRef pref in Module.ParameterRefs.Where(p => p.ParameterObject == para).ToList())
                    Module.ParameterRefs.Remove(pref);
            }
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.Parameter).Id = -1;
        }

        private void ManuelId(object sender, RoutedEventArgs e)
        {
            PromptDialog diag = new PromptDialog("Neue Parameter ID", "ID Manuell");
            if(diag.ShowDialog() == true)
            {
                int id;
                if(!int.TryParse(diag.Answer, out id))
                {
                    MessageBox.Show("Bitte geben Sie eine Ganzzahl ein.", "Eingabefehler");
                    return;
                }
                Parameter para = Module.Parameters.SingleOrDefault(p => p.Id == id);
                if(para != null)
                {
                    MessageBox.Show($"Die ID {id} wird bereits von Parameter {para.Name} verwendet.", "Doppelte ID");
                    return;
                }
                ((sender as Button).DataContext as Models.Parameter).Id = id;
            }
        }
    
    
        #region DragNDrop

        private Parameter _draggedItem;
        private Parameter _target;

        private void ListMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is ListBox && e.LeftButton == MouseButtonState.Pressed)
            {
                _draggedItem = (Parameter)ParamList.SelectedItem;
                if (_draggedItem != null)
                {
                    DragDropEffects finalDropEffect = DragDrop.DoDragDrop(ParamList, ParamList.SelectedValue, DragDropEffects.Move);
                    //Checking target is not null and item is
                    //dragging(moving) and move drop was accepted
                    
                    if ((finalDropEffect == DragDropEffects.Move) && (_target != null) && (_draggedItem != _target))
                    {
                        //TODO decide to insert above or below by pressing shift?
                        Module.Parameters.Remove(_draggedItem);
                        Module.Parameters.Insert(Module.Parameters.IndexOf(_target), _draggedItem);

                        _target = null;
                        _draggedItem = null;
                    }
                }
            }
        }

        private void ListDragOver(object sender, DragEventArgs e)
        {
            if(sender != ParamList)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            Parameter item = GetNearestContainer(e.OriginalSource);

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
            Parameter TargetItem = GetNearestContainer(e.OriginalSource);
            if (TargetItem != null && _draggedItem != null)
            {
                _target = TargetItem;
                e.Effects = DragDropEffects.Move;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private Parameter GetNearestContainer(object source)
        {
            Parameter item = (source as System.Windows.Documents.Run)?.DataContext as Parameter;

            if(item == null)
                item = (source as System.Windows.Controls.Border)?.DataContext as Parameter;

            if(item == null)
                item = (source as System.Windows.Controls.Image)?.DataContext as Parameter;

            if(item == null)
                item = (source as System.Windows.Controls.TextBlock)?.DataContext as Parameter;
            return item;
        }

        #endregion
    }
}