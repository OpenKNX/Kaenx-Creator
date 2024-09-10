using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
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

namespace Kaenx.Creator.Classes
{
    public class TextFilter
    {
        private string Query { get; set; } = "";
        private string _query { get; set; } = "";
        private ICollectionView view;

        public TextFilter(TextBox query)
        {
            query.TextChanged += (object sender, TextChangedEventArgs e) => {
                Query = query.Text.ToLower();
                view.Refresh();
            };
        }
        
        public void ChangeView(object list)
        {
            view = System.Windows.Data.CollectionViewSource.GetDefaultView(list);

            view.Filter = (object item) => {
                string value = item.GetType().GetProperty("Name").GetValue(item, null).ToString();
                if(item.GetType().GetProperty("Id") != null)
                {
                    string id = item.GetType().GetProperty("Id").GetValue(item, null).ToString();
                    if(id == Query) return true;
                }
                return value.ToLower().Contains(Query);
            };
        }

        public void Hide()
        {
            _query = Query;
            Query = "";
            view.Refresh();
        }

        public void Show()
        {
            Query = _query;
            if(view != null)
                view.Refresh();
        }
    }
}