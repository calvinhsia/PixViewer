using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PixViewer
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MemWatsonKusto"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MemWatsonKusto;assembly=MemWatsonKusto"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:MyComboBox/>
    ///
    /// </summary>
    public class MyComboBox : ComboBox
    {
        private PropertyInfo typSettings;

        //static MyComboBox()
        //{   
        //    //https://stackoverflow.com/questions/6695888/wpf-custom-controls-are-invisible
        //    //[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly))]
        //    //DefaultStyleKeyProperty.OverrideMetadata(typeof(MyComboBox), new FrameworkPropertyMetadata(typeof(MyComboBox)));
        //}
        public void Initialize(string ValueSettingName)
        {
            typSettings = Properties.Settings.Default.GetType().GetProperty(ValueSettingName);
            var initValues = (StringCollection)typSettings.GetValue(Properties.Settings.Default);
            this.Items.Clear();
            if (initValues?.Count > 0)
            {
                foreach (var itm in initValues)
                {
                    this.Items.Add(new ComboBoxItem() { Content = itm });
                }
                this.Text = initValues[0];
            }
            this.SelectedIndex = 0;
        }
        /// <summary>
        /// If the text is not in the list, then add it at beginning of the list and return true. Persist settings
        /// </summary>
        public bool CommitData()
        {
            var isNewItem = true;
            foreach (ComboBoxItem itm in this.Items)
            {
                if ((string)itm.Content == this.Text)
                {
                    isNewItem = false;
                    break;
                }
            }
            if (isNewItem)
            {
                this.Items.Insert(0, new ComboBoxItem() { Content = this.Text });
                var col = new StringCollection();
                foreach (ComboBoxItem itm in this.Items)
                {
                    col.Add((string)itm.Content);
                }
                typSettings.SetValue(Properties.Settings.Default, col);
                Properties.Settings.Default.Save();
            }
            return isNewItem;
        }
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (this.IsDropDownOpen)
            {
                if (e.Key == Key.Delete)
                {
                    if (Items.Count > 0 && this.SelectedIndex >= 0)
                    {
                        Items.RemoveAt(SelectedIndex);
                        if (Items.Count > 0)
                        {
                            SelectedIndex = 0;
                        }
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
