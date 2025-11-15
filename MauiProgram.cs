using MedicinesTracker.DataAccess;
using MedicinesTracker.Services;
using MedicinesTracker.ViewModels;
using MedicinesTracker.Views;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MedicinesTracker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Light.ttf", "OpenSansLight");
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Регистрируем сервисы
            builder.Services.AddSingleton<DBConnection>();
            builder.Services.AddSingleton<MedicineService>();
            builder.Services.AddSingleton<MedicineListVM>();
            builder.Services.AddSingleton<TodayMedicineVM>();
            builder.Services.AddSingleton<MedicineDetailVM>();
            builder.Services.AddSingleton<BaseInfoVM>();
            builder.Services.AddSingleton<NotificationInfoVM>();
            builder.Services.AddSingleton<StockInfoVM>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }

}
