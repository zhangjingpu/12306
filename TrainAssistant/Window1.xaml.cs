using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TrainAssistant
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            gridTest.Children.Clear();
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 3; i++)
                {
                    CheckBox chkContact = new CheckBox()
                    {
                        Content = "第"+(j+1)+"行第"+(i+1)+"列",
                        Height = 15
                    };
                    gridTest.Children.Add(chkContact);
                    chkContact.SetValue(Grid.RowProperty, j);
                    chkContact.SetValue(Grid.ColumnProperty, i);
                }
            }
        }
    }
}
