namespace MedicinesTracker.Services
{
    public interface IPreferencesService
    {
        bool Get(string key, bool defaultValue);
        void Set(string key, bool value);
        string Get(string key, string defaultValue);
        void Set(string key, string value);
    }

    public class PreferencesService : IPreferencesService
    {
        public bool Get(string key, bool defaultValue)
        {
            return Preferences.Default.Get(key, defaultValue);
        }

        public void Set(string key, bool value)
        {
            Preferences.Default.Set(key, value);
        }

        public string Get(string key, string defaultValue)
        {
            return Preferences.Default.Get(key, defaultValue);
        }

        public void Set(string key, string value)
        {
            Preferences.Default.Set(key, value);
        }
    }
}