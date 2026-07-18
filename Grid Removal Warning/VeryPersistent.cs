using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using NLog;

namespace Grid_Removal_Warning
    //Thanks rar61 :))
{
    public class VeryPersistent<T> : IDisposable where T : new()
    {
        public string Path { get; private set; }
        public T Data { get; private set; }
        
        private static Logger Log = LogManager.GetCurrentClassLogger();
        private Timer saveTimer;

        public VeryPersistent(string path, T data)
        {
            Path = path;
            Data = data;
            
            if (data is INotifyPropertyChanged notify) notify.PropertyChanged += OnPropertyChanged;
        }
        
        public void Dispose()
        {
            if (Data is INotifyPropertyChanged notify) notify.PropertyChanged -= OnPropertyChanged;
            saveTimer?.Dispose();
            Save();
            
            GC.SuppressFinalize(this);
        }
        
        ~VeryPersistent()
        {
            Dispose();
        }

        public static VeryPersistent<T> Load(string path, bool saveIfNoFile = true, Func<T> factory = null)
        {
            VeryPersistent<T> persistent;
            
            try
            {
                if (File.Exists(path))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    using (var f = File.OpenText(path)) persistent = new VeryPersistent<T>(path,(T)serializer.Deserialize(f));
                }
                else
                {
                    CreateDefault();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to load config '{path}' Recreating Default.");
                CreateDefault();
            }

            return persistent;

            void CreateDefault()
            {
                persistent = factory != null ? new VeryPersistent<T>(path, factory()) : new VeryPersistent<T>(path, new T());
                if (saveIfNoFile) persistent.Save();
                Log.Info($"Created default config: {path}");
            }
        }
        
        public void Save()
        {
            var serializer = new XmlSerializer(typeof(T));

            try
            {
                using (var f = File.CreateText(Path)) serializer.Serialize(f, Data);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SaveAsync();
        }
        
        private void SaveAsync()
        {
            if (saveTimer == null) saveTimer = new Timer(state => Save());
            saveTimer.Change(1000, Timeout.Infinite);
        }
    }
}