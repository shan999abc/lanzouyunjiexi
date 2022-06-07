using System;
using System.Windows;

namespace TEST
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        public MainWindow()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                textbox1.Text = await Download.关键字解析(textbox2.Text);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233079)
                {
                    textbox1.Text = "密码不正确...";
                }
                else
                {
                    textbox1.Text = ex.Message;
                }
                
            }
        }
    }
}
