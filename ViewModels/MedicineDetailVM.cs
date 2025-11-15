using CommunityToolkit.Mvvm.ComponentModel;
using MedicinesTracker.Models;
using MedicinesTracker.Services;
using System.Diagnostics;
using System.Windows.Input;

namespace MedicinesTracker.ViewModels
{
    [QueryProperty(nameof(Medicine), "medicine")]
    public partial class MedicineDetailVM : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty]
        private MedicineModel _medicine;

        public ICommand BaseInfoTappedCommand { get;}
        public ICommand StockTappedCommand { get; }
        public ICommand NotificationTappedCommand { get; }

        public MedicineDetailVM(MedicineService medicineService)
        {
            _medicine = new MedicineModel();
            _medicineService = medicineService;
            StockTappedCommand = new Command(OpenStockPage);
            BaseInfoTappedCommand = new Command(OpenBaseInfoPage);
            NotificationTappedCommand = new Command(OpenNotificationPage);
        }

        private async void OpenBaseInfoPage()
        {
            if (Medicine == null)
            {
                return;
            }

            var state = new Dictionary<string, object>
            {
                {"medicine", Medicine}
            };

            await Shell.Current.GoToAsync("BaseInfoPage", state);
        }


        private async void OpenStockPage()
        {
            if (Medicine == null)
            {
                return;
            }

            var state = new Dictionary<string, object>
            {
                {"medicine", Medicine}
            };

            await Shell.Current.GoToAsync("StockInfoPage", state);
        }

        private async void OpenNotificationPage()
        {
            if (Medicine == null)
            {
                return;
            }

            var state = new Dictionary<string, object>
            {
                {"medicine", Medicine}
            };

            await Shell.Current.GoToAsync("NotificationInfoPage", state);
        }
    }

}
