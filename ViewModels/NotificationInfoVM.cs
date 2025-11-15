using CommunityToolkit.Mvvm.ComponentModel;
using MedicinesTracker.Models;
using MedicinesTracker.Services;

namespace MedicinesTracker.ViewModels
{
    [QueryProperty(nameof(Medicine), "medicine")]
    public partial class NotificationInfoVM : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty]
        private MedicineModel _medicine;
        public NotificationInfoVM(MedicineService medicineService)
        {
            _medicine = new MedicineModel();
            _medicineService = medicineService;
        }
    }
}
