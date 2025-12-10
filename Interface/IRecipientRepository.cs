using MedicinesTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Interface
{
    public interface IRecipientRepository
    {
        Task<IEnumerable<RecipientModel>> GetAllRecipientsAsync();
    }
}
