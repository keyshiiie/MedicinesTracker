using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Message
{
    public class RecipientIdMessage
    {
        public int Id { get; }

        public RecipientIdMessage(int id)
        {
            Id = id;
        }
    }


}
