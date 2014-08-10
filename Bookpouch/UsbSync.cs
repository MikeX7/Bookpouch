using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Serialization.Formatters.Binary;

namespace Bookpouch
{
    static class UsbSync
    {

        static private string _deviceDir;
        static public bool ManuaSync;

        public static void Sync()
        {
            var dirs = Directory.GetDirectories("books");
            var extensions = Properties.Settings.Default.FileExtensions.Split(';');
            var localBookList = new List<string>();

            foreach (var dir in dirs)
            {
                if (!File.Exists(dir + "\\info.dat")) 
                    continue;

                using (var infoFile = new FileStream(dir + "\\info.dat", FileMode.Open))
                {
                    var bf = new BinaryFormatter();
                    var bookInfo = (Dictionary<string, object>) bf.Deserialize(infoFile);
                    
                    if ((bool) bookInfo["sync"])
                    {
                        localBookList.Add(
                            
                                Directory.EnumerateFiles(dir)
                                    .FirstOrDefault(
                                        f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))));
                    }
                }
            }

            var fileList = GetFileList();

            if (fileList.Length == 0)
                return;
            
            foreach (var file in fileList) //Remove any files from the reader which aren't books marked for sync in the Bookpouch
            {
                var fileName = Path.GetFileName(file);

                if (localBookList.Select(Path.GetFileName).Contains(fileName))                
                    continue;

                DebugConsole.WriteLine("Deleting " + file);

                File.Delete(file);                
            }

            foreach (var file in localBookList) //Copy all books marked for sync from the local storage to the reader device, skip books which already exist on the reader
            {
                if (fileList.Select(Path.GetFileName).Contains(Path.GetFileName(file)))
                    continue;

                DebugConsole.WriteLine("Copying " + file);                
                
                File.Copy(file, _deviceDir + "/" + Path.GetFileName(file));
                
            }

            MainWindow.Info(UiLang.Get("SyncFinished"));
        }

        /// <summary>
        /// Get a list of book files from the reader device
        /// </summary>        
        private static String[] GetFileList()
        {            
            var drive = GetDriveLetter();

            if (drive == "") //Reader is not connected to the pc or wasn't found, so there is no point to go on                            
                return new String[0];

            _deviceDir = drive + Properties.Settings.Default.DeviceRootDir;

            if (!Directory.Exists(_deviceDir)) //Specified directory on the reader which should contain ebook files doesn't exist
            {                
                MainWindow.Info(String.Format(UiLang.Get("SyncReaderDirNotFound"), Properties.Settings.Default.DeviceRootDir, Properties.Settings.Default.DeviceModel), 1);                

                return new String[0];            
            }

            var extensions = Properties.Settings.Default.FileExtensions.Split(';');

            var files = Directory.EnumerateFiles(_deviceDir).Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToArray();            

            if (files.Length == 0)
                MainWindow.Info(UiLang.Get("SyncNoBooks"));

            return files;
        }
        
        /// <summary>
        /// Get the drive letter for the reader device
        /// </summary>        
        private static String GetDriveLetter() 
        {
            try
            {                                
                var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskDrive");

                DebugConsole.WriteLine("Disk list:\n--------------------");

                foreach (var queryObj in searcher.Get().Cast<ManagementObject>())
                {
                    DebugConsole.WriteLine("Model: " + queryObj["Model"] + "; PnP ID: " + queryObj["PNPDeviceID"]);

                    if (!queryObj["PNPDeviceID"].ToString().Contains(Properties.Settings.Default.DevicePnpId) && !queryObj["Model"].ToString().Contains(Properties.Settings.Default.DeviceModel))                    
                        continue;

                    foreach (var partition in queryObj.GetRelated("Win32_DiskPartition").Cast<ManagementObject>())
                    {
                        foreach (var disk in partition.GetRelated("Win32_LogicalDisk"))
                        {
                            DebugConsole.WriteLine("--------------------");
                            return (string) disk["name"]; 
                        }
                    }
                }

                DebugConsole.WriteLine("--------------------");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Houston, we have a problem with getting the reader disk letter:\n" + e.Message);
            }

            if(ManuaSync)
                MainWindow.Info(UiLang.Get("SyncNoReadersFound"), 1);

            ManuaSync = false;

            return "";
        }
        
      
    }
}
