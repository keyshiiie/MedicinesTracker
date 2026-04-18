using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MedicinesTracker.Dto
{
    public class GroupedTodayMedicine : ObservableObject
    {
        public string RecipientName { get; private set; }
        public ObservableCollection<TodayMedicineDto> Medicines { get; private set; }

        public GroupedTodayMedicine(string recipientName, IEnumerable<TodayMedicineDto> medicines)
        {
            RecipientName = recipientName;
            Medicines = new ObservableCollection<TodayMedicineDto>(medicines);
        }
    }
}