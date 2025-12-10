using Dapper;
using MedicinesTracker.Interface;
using MedicinesTracker.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Repository
{
    public class MethodAdmissionRepository : IMethodAdmissionRepository
    {
        private readonly DBHandler _dbHandler;
        public MethodAdmissionRepository(DBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        public async Task<IEnumerable<MethodAdmissionModel>> GetAllMethodsAdmissionAsync()
        {
            var query = @"SELECT * FROM MethodAdmission";
            return await _dbHandler.QueryAsync<MethodAdmissionModel>(query);
        }
    }
}
