using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MedicinesTracker.Dto
{
    public class GroupedHistory : ObservableObject
    {
        public string Date { get; private set; }
        public ObservableCollection<HistoryDto> Medicines { get; private set; }

        public GroupedHistory(string date, IEnumerable<HistoryDto> medicines)
        {
            Date = date;
            Medicines = new ObservableCollection<HistoryDto>(medicines);
        }
    }
}
