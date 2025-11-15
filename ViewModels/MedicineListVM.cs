using CommunityToolkit.Mvvm.ComponentModel;
using MedicinesTracker.Models;
using MedicinesTracker.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace MedicinesTracker.ViewModels
{
    public class MedicineListVM : ObservableObject
    {
        private readonly MedicineService _medicineService;
        private ObservableCollection<MedicineModel> _medicines = new ObservableCollection<MedicineModel>();
        public ICommand MedicineTappedCommand { get; }

        public ObservableCollection<MedicineModel> Medicines
        {
            get => _medicines;
            private set
            {
                if (_medicines != value)
                {
                    _medicines = value;
                    OnPropertyChanged(nameof(Medicines));
                }
            }
        }

        public MedicineListVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
            LoadMedicineList();
            MedicineTappedCommand = new Command<MedicineModel>(OpenDetailPage);
        }

        private async void OpenDetailPage(MedicineModel medicine)
        {
            var state = new Dictionary<string, object>
            {
                {"medicine", medicine}
            };
            await Shell.Current.GoToAsync("MedicineDetailPage", state);
        }




        private void LoadMedicineList()
        {
            try
            {
                var medicines = _medicineService.GetAllMedicines();
                foreach (var medicine in medicines)
                {
                    Medicines.Add(medicine);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка загрузки данных.", ex);
            }
        }
    }
}
