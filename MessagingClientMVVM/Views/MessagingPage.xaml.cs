﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MessagingClientMVVM
{
    /// <summary>
    /// Interaction logic for MessagingPage.xaml
    /// </summary>
    public partial class MessagingPage : Page
    {
        public MessagingPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                ((MessagingViewModel)DataContext).ns = NavigationService.GetNavigationService(this);
            }
        }
    }
}
