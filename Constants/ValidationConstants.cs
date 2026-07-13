using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Constants
{
    public static class ValidationConstants
    {
        public const int MaxMedicineNameLength = 255;
        public const int MaxQuantity = 1000;
        public const int MinDosage = 1;
        public const int MaxDosage = 100;
        public const int MaxRecipientNameLength = 255;
        public const int MaxTimesCount = 10;
    }
}
