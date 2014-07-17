using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace Libropouch
{
    class UsbSync
    {                
        public UsbSync()
        {
            var fileList = GetFileList();

            if (fileList.Length == 0)
                return;

            foreach (var file in fileList)
            {
                Debug.WriteLine(file);
            }            
        }

        private static String[] GetFileList()
        {            
            var drive = GetDriveLetter();

            if (drive == "") //Reader is not connected to the pc or wasn't found, so there is no point to go on                            
                return new String[0];            

            if (!Directory.Exists(@drive + Properties.Settings.Default.DeviceRootDir)) //Specified directory on the reader which should contain ebook files doesn't exist
            {                
                MainWindow.Info(String.Format("Specified directory \"{0}\" wasn't found on the connected reader: {1}.", Properties.Settings.Default.DeviceRootDir, Properties.Settings.Default.DeviceModel), 1);                

                return new String[0];            
            }

            var extensions = Properties.Settings.Default.FileExtensions.Split(';');

            var files = Directory.EnumerateFiles(@drive + Properties.Settings.Default.DeviceRootDir).Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToArray();

            if (files.Length == 0)
                MainWindow.Info("No books found on the reader.");

            return files;
        }
        
        private static String GetDriveLetter() //Get the drive letter for the reader device
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
                Debug.WriteLine("Houston, we have a problem with getting the reader disk letter:\n" + e);
            }

            MainWindow.Info("I wasn't able to find any connected readers.", 1);
            return "";
        }
        
      
    }
}
