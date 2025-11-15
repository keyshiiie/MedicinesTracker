using CommunityToolkit.Mvvm.ComponentModel;
using MedicinesTracker.Models;
using MedicinesTracker.Services;
using System.Windows.Input;

namespace MedicinesTracker.ViewModels
{
    [QueryProperty(nameof(Medicine), "medicine")]
    public partial class BaseInfoVM : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty]
        private MedicineModel _medicine;
        public BaseInfoVM(MedicineService medicineService)
        {
            _medicine = new MedicineModel();
            _medicineService = medicineService;
        }
    }
}
