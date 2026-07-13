using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MedicinesTracker.Entities;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;

namespace MedicinesTracker.Modules.Notifications.ViewModels
{
    public partial class AcquaintanceVM : ObservableObject
    {
        private readonly IRecipientRepository _recipientRepository;
        private readonly IValidatorService _validatorService;
        private readonly IPreferencesService _preferencesService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppSettingsService _appSettingsService; 
        private Page? _page;

        [ObservableProperty]
        private Recipient _recipient = new Recipient();

        public AcquaintanceVM(
            IRecipientRepository recipientRepository,
            IValidatorService validatorService,
            IPreferencesService preferencesService,
            IServiceProvider serviceProvider,
            IAppSettingsService appSettingsService)  
        {
            _recipientRepository = recipientRepository;
            _validatorService = validatorService;
            _preferencesService = preferencesService;
            _serviceProvider = serviceProvider;
            _appSettingsService = appSettingsService; 
        }

        // Метод для установки ссылки на страницу
        public void SetPage(Page page)
        {
            _page = page;
        }

        [RelayCommand]
        private async Task SaveAndContinue()
        {
            try
            {
                var errors = _validatorService.GetRecipientValidationErrors(Recipient);

                if (errors.Any())
                {
                    if (_page != null)
                    {
                        await _page.DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
                    }
                    return;
                }

                int rowsAffected = await _recipientRepository.AddRecipientAsync(Recipient);

                if (rowsAffected > 0)
                {
                    await _appSettingsService.MarkFirstLaunchCompletedAsync();

                    // ✅ Правильный способ перехода к главному приложению
                    var appShell = new AppShell();

                    // Получаем текущее окно и устанавливаем новую страницу
                    var window = Application.Current?.Windows.FirstOrDefault();
                    if (window != null)
                    {
                        window.Page = appShell;  // window.Page существует, а SetPage - нет!
                    }
                }
                else
                {
                    if (_page != null)
                    {
                        await _page.DisplayAlertAsync(
                            "Предупреждение!",
                            "Получатель не было обновлен",
                            "ОК");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_page != null)
                {
                    await _page.DisplayAlertAsync(
                        "Ошибка!",
                        $"Не удалось сохранить: {ex.Message}",
                        "ОК");
                }
            }
        }
    }
}