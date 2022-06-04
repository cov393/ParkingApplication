using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using App1.View;
using App1.Services;

namespace App1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();

            //if (!string.IsNullOrEmpty(Preferences.Get("MyFirebaseRefreshToken", "")))
            //{
            //    //disable main page
            //    MainPage = new NavigationPage(new MainPage());
            //    //MainPage = new NavigationPage(new Home());
            //}
            //else
            //{
            //    MainPage = new NavigationPage(new Home());
            //}
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
