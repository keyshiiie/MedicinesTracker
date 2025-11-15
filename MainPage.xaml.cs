using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace MedicinesTracker
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }
        private void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is Exception ex)
            {
                LogException(ex);
            }
        }

        private void LogException(Exception ex)
        {
            string logsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_checks.log");
            try
            {
                StackFrame? sf = new StackFrame(1);
                MethodBase? method = sf?.GetMethod();
                int? line = sf?.GetFileLineNumber();

                if (method == null || line == null)
                {
                    return;
                }

                string declaringType = method?.DeclaringType?.FullName ?? "UnknownType";
                string lineStr = line?.ToString() ?? "UnknownLine";

                string logMessage = $"{Environment.CurrentManagedThreadId} ~ {DateTime.Now:yyyy-MM-dd HH:mm:ss} " +
                                   $"{declaringType}::{lineStr} - {GetErrorMessage(ex)}";


                File.AppendAllText(logsFilePath, logMessage + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
                // Если ошибка при логировании — игнорируем
            }
        }

        private string GetErrorMessage(Exception? ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            string message = ex.StackTrace + "\n" + ex.Message;
            Exception? innerException = ex.InnerException;

            if (innerException != null)
            {
                message += "\nCaused By: " + GetErrorMessage(innerException);
            }
            return message;
        }
    }
}
