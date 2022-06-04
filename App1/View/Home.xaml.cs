using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
//using Xamarin.Forms.GoogleMaps;
using Xamarin.Forms.Xaml;

namespace App1.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Home : ContentPage
    {
        public Home()
        {
            InitializeComponent();

            GeneratePins();
        }
        private void GeneratePins()
        {
            Pin pin_West_Midlands = new Pin()
            {
                Type = PinType.Place,
                Label = "Parking lot",
                Address = "115 Far Gosford St, Coventry CV1 5EA",
                Position = new Position(52.407834021213674d, -1.4947492916852505d),
                Rotation = 33.3f,
                Tag = "id_coventry",

            };


            map.Pins.Add(pin_West_Midlands);
            map.MoveToRegion(MapSpan.FromCenterAndRadius(pin_West_Midlands.Position, Distance.FromMeters(1000)));

        }
    }
}