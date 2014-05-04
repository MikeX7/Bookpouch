using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace Libropouch
{
    class UsbSync
    {                
        public UsbSync()
        {     
            GetFileList(); 
            
        }



        private static void GetFileList()
        {
            var drive = GetDriveLetter();

            if (drive == "")
            {
                //MessageBox.Show("I wasn't able to detect any connected reader device, sorry.");
                Debug.WriteLine("I wasn't able to detect any connected reader device, sorry.");
                return;
            }
                
            Debug.WriteLine("Kindle found." + drive);
            
            
            return;
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

                        foreach (var partition in queryObj.GetRelated("Win32_DiskPartition"))
                        {
                            var partitionO = (ManagementObject) partition;

                            foreach (var disk in partitionO.GetRelated("Win32_LogicalDisk"))
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

            return "";
        }
        
      
    }
}
