﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LogicAndTrick.Oy;
using Newtonsoft.Json;
using CBRE.Common.Shell;
using CBRE.Common.Shell.Hooks;
using CBRE.Common.Shell.Settings;
using CBRE.Common.Translations;

namespace CBRE.Shell.Translations
{
    [Export(typeof(IStartupHook))]
    [Export(typeof(ISettingsContainer))]
    [Export(typeof(ITranslationStringProvider))]
    public class TranslationsProvider : IStartupHook, ISettingsContainer, ITranslationStringProvider
    {
        private readonly TranslationStringsCatalog _catalog;
        private readonly IEnumerable<Lazy<object>> _autoTranslate;
        private readonly IApplicationInfo _appInfo;

        private string Language { get; set; } = "en";

        [ImportingConstructor]
        public TranslationsProvider(
            [Import] TranslationStringsCatalog catalog,
            [ImportMany("AutoTranslate")] IEnumerable<Lazy<object>> autoTranslate,
            [Import(AllowDefault = true)] IApplicationInfo appInfo)
        {
            _catalog = catalog;
            _autoTranslate = autoTranslate;
            _appInfo = appInfo;
        }

        public Task OnStartup()
        {
            // Load language setting early, as most settings are loaded on initialise
            string path = _appInfo?.GetApplicationSettingsFolder("Shell");
            if (path == null) return Task.CompletedTask;

            string file = Path.Combine(path, Name + ".json");

            Dictionary<string, string> data = null;
            if (File.Exists(file))
            {
                try
                {
                    data = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                }
                catch
                {
                    data = null;
                }
            }
            if (data != null && data.ContainsKey("Language"))
            {
                Language = data["Language"] ?? "en";
            }

            foreach (Lazy<object> at in _autoTranslate)
            {
                try
                {
                    Translate(at.Value);
                }
                catch (Exception e)
                {
                    Oy.Publish("Shell:UnhandledException", e);
                }
            }

            return Task.FromResult(0);
        }

        public void Translate(object target)
        {
            if (target == null) return;

            Type ty = target.GetType();
            _catalog.Load(ty);

            if (target is IManualTranslate mt) mt.Translate(this);
            else Inject(ty, target);
        }

        private void Inject(Type type, object target)
        {
            IEnumerable<System.Reflection.PropertyInfo> props = type.GetProperties().Where(x => x.PropertyType == typeof(string) && x.CanWrite);
            foreach (System.Reflection.PropertyInfo prop in props)
            {
                string path = type.FullName + '.' + prop.Name;
                string val = _catalog.GetString(Language, path);
                if (val != null) prop.SetValue(target, val);
            }
        }

        public string GetString(params string[] path)
        {
            string key = String.Join(".", path);
            return _catalog.GetString(Language, key);
        }

        public string GetSetting(params string[] path)
        {
            string key = String.Join(".", path);
            return _catalog.GetSetting(Language, key);
        }

        // Settings container

        public string Name => "CBRE.Shell.Translations";

        public IEnumerable<SettingKey> GetKeys()
        {
            yield return new SettingKey("Interface", "Language", typeof(string)) { EditorType = "LanguageSelectionEditor" };
        }

        public void LoadValues(ISettingsStore store)
        {
            Language = store.Get("Language", Language) ?? "en";
        }

        public void StoreValues(ISettingsStore store)
        {
            store.Set("Language", Language ?? "en");
        }
    }
}
