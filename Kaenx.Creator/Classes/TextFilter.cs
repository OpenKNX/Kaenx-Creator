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

        public TextFilter(object list, TextBox query)
        {
            ICollectionView view = System.Windows.Data.CollectionViewSource.GetDefaultView(list);

            view.Filter = (object item) => {
                string value = item.GetType().GetProperty("Name").GetValue(item, null).ToString();
                return value.ToLower().Contains(Query);
            };

            query.TextChanged += (object sender, TextChangedEventArgs e) => {
                Query = query.Text.ToLower();
                view.Refresh();
            };
        }
    }
}