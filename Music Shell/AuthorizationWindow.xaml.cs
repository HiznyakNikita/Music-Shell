using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VkNet;

namespace Music_Shell
{
    /// <summary>
    /// Interaction logic for AuthorizationWindow.xaml
    /// </summary>
    public partial class AuthorizationWindow : Window
    {
        public AuthorizationWindow()
        {
            InitializeComponent();
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void authButton_Click(object sender, RoutedEventArgs e)
        {
            if (tbLogin.Text == "" || tbPassword.Password == "")
                MessageBox.Show("Please, fill all the fields!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                var vkApi = new VkApi();
                try
                {
                    vkApi.Authorize(Convert.ToInt32(Properties.Settings.Default.appId), tbLogin.Text, tbPassword.Password.ToString(), VkNet.Enums.Filters.Settings.All);
                    Properties.Settings.Default.token = vkApi.AccessToken;
                    Properties.Settings.Default.id = vkApi.UserId.ToString();
                    Properties.Settings.Default.auth = true;
                    Properties.Settings.Default.Save();
                    this.Close();
                }

                catch (Exception ex) 
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
    }
}
