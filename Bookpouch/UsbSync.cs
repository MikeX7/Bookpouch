using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace Bookpouch
{
    static class UsbSync
    {

        static private string _deviceDir;
        static public bool ManualSync = false;

        /// <summary>
        /// Starts the synchronization process, possibly in a new thread.
        /// </summary>
        public static void Sync()
        {
            MainWindow.Busy(true);
            Task.Factory.StartNew(SyncBookFiles);
        }

        /// <summary>
        /// Take all books files marked for sync and copy them onto the reader device, if one is found.
        /// If any book files are found on the reader device, which are not marked for sync in the local library, they will be deleted from the device.
        /// </summary>
        private static void SyncBookFiles()
        {            
            var localBookList = LibraryStructure.List();
            var localBookListForSync = (from bookData in localBookList where bookData.Sync select BookKeeper.GetAbsoluteBookFilePath(bookData.Path)).ToList();
            var bookList = GetFileList();

            foreach (var file in from file in bookList let fileName = Path.GetFileName(file) where !localBookListForSync.Select(Path.GetFileName).Contains(fileName) select file)
            {
                DebugConsole.WriteLine("Usb sync: Deleting " + file);

                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    DebugConsole.WriteLine("Usb sync: Failed to delete " + file + ": " + e);
                }
            }

            foreach (
                var file in
                    localBookListForSync.Where(
                        file => File.Exists(file) && !bookList.Select(Path.GetFileName).Contains(Path.GetFileName(file)))
                        .Where(file => !File.Exists(Path.Combine(_deviceDir, Path.GetFileName(file)))))
            {
                DebugConsole.WriteLine("Copying " + file);

                try
                {
                    File.Copy(file, Path.Combine(_deviceDir, Path.GetFileName(file)));
                }
                catch (Exception e)
                {
                    MainWindow.Info(String.Format(UiLang.Get("SyncFileCopyFailed"), file));
                    DebugConsole.WriteLine("Usb sync: Error while copying " + file + ": " + e);
                }
            }

            MainWindow.Info(UiLang.Get("SyncFinished"));
            MainWindow.Busy(false);
        }

        /// <summary>
        /// Get a list of book files from the reader device
        /// </summary>        
        private static String[] GetFileList()
        {            
            var drive = GetDriveLetter();

            if (drive == "") //Reader is not connected to the pc or wasn't found, so there is no point to go on                            
                return new String[0];

            _deviceDir = drive + Properties.Settings.Default.DeviceBooksDir;

            if (!Directory.Exists(_deviceDir)) //Specified directory on the reader which should contain ebook files doesn't exist
            {                
                MainWindow.Info(String.Format(UiLang.Get("SyncReaderDirNotFound"), Properties.Settings.Default.DeviceBooksDir, Properties.Settings.Default.DeviceModel), 1);                

                return new String[0];            
            }

            var extensions = Properties.Settings.Default.FileExtensions.Split(';');
            var files = Directory.EnumerateFiles(_deviceDir).Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToArray();                        

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

            if(ManualSync)
                MainWindow.Info(UiLang.Get("SyncNoReadersFound"), 1);

            ManualSync = false;

            return "";
        }
        
      
    }
}
