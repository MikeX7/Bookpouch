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
            bool skipInfo;

            var fileList = GetFileList(out skipInfo);

            if (fileList.Length == 0)
            {
                if(!skipInfo)
                    MainWindow.Info("File list is empty.");

                return;
            }

            foreach (var file in fileList)
            {
                Debug.WriteLine(file);
            }
            
        }



        private static String[] GetFileList(out bool skipInfo)
        {
            skipInfo = false; 
            var drive = GetDriveLetter();            

            if (drive == "") //Reader is not connected to the pc or wasn't found, so there is no point to go on
                return new String[0];            

            if (!Directory.Exists(@drive + Properties.Settings.Default.RootDir)) //Specified directory on the reader which should contain ebook files doesn't exist
            {
                skipInfo = true; //Don't display empty file list info to the user, since there is no directory to scan
                MainWindow.Info(String.Format("Specified directory \"{0}\" wasn't found on the connected reader: {1}.", Properties.Settings.Default.RootDir, Properties.Settings.Default.UsbModel));                

                return new String[0];            
            }

            var files = Directory.GetFiles(@drive + Properties.Settings.Default.RootDir);

            return files;
        }
        
        private static String GetDriveLetter() //Get the drive letter for the reader device
        {
            try
            {                                
                var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskDrive");
              
                foreach (var queryObj in searcher.Get().Cast<ManagementObject>())
                {
                    if (!queryObj["PNPDeviceID"].ToString().Contains(Properties.Settings.Default.UsbPnpDeviceId) &&
                        !queryObj["Model"].ToString().Contains(Properties.Settings.Default.UsbModel))
                        continue;

                    foreach (var partition in queryObj.GetRelated("Win32_DiskPartition").Cast<ManagementObject>())
                    {
                        foreach (var disk in partition.GetRelated("Win32_LogicalDisk"))
                        {                            
                            return (string) disk["name"];
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Houston, we have a problem with getting the reader disk letter:\n" + e);
            }

            MainWindow.Info("I wasn't able to find any connected readers.");
            return "";
        }
        
      
    }
}
