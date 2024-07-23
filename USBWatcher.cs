using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USB_Finder
{
    public class USBWatcher
    {
        private ManagementEventWatcher insertWatcher;
        private ManagementEventWatcher removeWatcher;

        public event EventHandler<USBEventArgs> USBInserted;
        public event EventHandler<USBEventArgs> USBRemoved;

        public USBWatcher()
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3");

            insertWatcher = new ManagementEventWatcher(insertQuery);
            removeWatcher = new ManagementEventWatcher(removeQuery);

            insertWatcher.EventArrived += new EventArrivedEventHandler(OnUSBInserted);
            removeWatcher.EventArrived += new EventArrivedEventHandler(OnUSBRemoved);
        }

        private void OnUSBInserted(object sender, EventArrivedEventArgs e)
        {
            string driveLetter = e.NewEvent.Properties["DriveName"].Value.ToString();
            var deviceInfo = GetUSBDeviceInfo(driveLetter);
            deviceInfo.IsNew = true; // Yeni cihazları işaretle
            USBInserted?.Invoke(this, new USBEventArgs
            {
                DriveLetter = driveLetter,
                DeviceName = deviceInfo.Name,
                SerialNumber = deviceInfo.SerialNumber,
                Brand = deviceInfo.Brand,
                TotalCapacity = deviceInfo.TotalCapacity,
                FreeSpace = deviceInfo.FreeSpace,
                EventType = "Inserted"
            });
        }

        private void OnUSBRemoved(object sender, EventArrivedEventArgs e)
        {
            string driveLetter = e.NewEvent.Properties["DriveName"].Value.ToString();
            USBRemoved?.Invoke(this, new USBEventArgs { DriveLetter = driveLetter, EventType = "Removed" });
        }

        public void Start()
        {
            insertWatcher.Start();
            removeWatcher.Start();
        }

        public void Stop()
        {
            insertWatcher.Stop();
            removeWatcher.Stop();
        }

        public List<USBDeviceInfo> EnumerateUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();
            string query = "SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject drive in searcher.Get())
                {
                    string deviceID = drive["DeviceID"].ToString();
                    string diskPartitionQuery = $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceID}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition";
                    using (ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher(diskPartitionQuery))
                    {
                        foreach (ManagementObject partition in partitionSearcher.Get())
                        {
                            string logicalDiskQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition";
                            using (ManagementObjectSearcher logicalDiskSearcher = new ManagementObjectSearcher(logicalDiskQuery))
                            {
                                foreach (ManagementObject logicalDisk in logicalDiskSearcher.Get())
                                {
                                    string driveLetter = logicalDisk["DeviceID"].ToString();
                                    USBDeviceInfo deviceInfo = GetUSBDeviceInfo(driveLetter);
                                    deviceInfo.IsNew = false; // Mevcut cihazları işaretle
                                    devices.Add(deviceInfo);
                                }
                            }
                        }
                    }
                }
            }
            return devices;
        }

        private USBDeviceInfo GetUSBDeviceInfo(string driveLetter)
        {
            USBDeviceInfo deviceInfo = new USBDeviceInfo();
            string partitionQuery = $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{driveLetter}'}} WHERE AssocClass=Win32_LogicalDiskToPartition";

            using (ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher(partitionQuery))
            {
                foreach (ManagementObject partition in partitionSearcher.Get())
                {
                    string diskQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition";
                    using (ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher(diskQuery))
                    {
                        foreach (ManagementObject disk in diskSearcher.Get())
                        {
                            deviceInfo.Name = disk["Model"]?.ToString();
                            deviceInfo.SerialNumber = CleanSerialNumber(disk["SerialNumber"]?.ToString() ?? "Unknown");
                            deviceInfo.DriveLetter = driveLetter;
                            deviceInfo.EventDate = DateTime.Now;
                            deviceInfo.Brand = GetUSBBrand(disk);

                            string logicalDiskQuery = $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID='{driveLetter}'";
                            using (ManagementObjectSearcher logicalDiskSearcher = new ManagementObjectSearcher(logicalDiskQuery))
                            {
                                foreach (ManagementObject logicalDisk in logicalDiskSearcher.Get())
                                {
                                    deviceInfo.TotalCapacity = Convert.ToInt64(logicalDisk["Size"]);
                                    deviceInfo.FreeSpace = Convert.ToInt64(logicalDisk["FreeSpace"]);
                                }
                            }
                        }
                    }
                }
            }
            return deviceInfo;
        }

        private string GetUSBBrand(ManagementObject disk)
        {
            string[] properties = { "Manufacturer", "Vendor", "PNPDeviceID" };
            foreach (var property in properties)
            {
                if (disk[property] != null && disk[property].ToString() != "Standard Disk Drives")
                {
                    return disk[property].ToString();
                }
            }
            return "Unknown";
        }

        private string CleanSerialNumber(string serialNumber)
        {
            // Sadece ASCII karakterleri tutarak ve boşlukları kaldırarak seri numarasını temizler
            var cleanSerial = new System.Text.StringBuilder();
            foreach (var ch in serialNumber)
            {
                if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                {
                    cleanSerial.Append(ch);
                }
            }
            return cleanSerial.ToString().Trim();
        }
    }

    public class USBEventArgs : EventArgs
    {
        public string DriveLetter { get; set; }
        public string DeviceName { get; set; }
        public string SerialNumber { get; set; }
        public string EventType { get; set; }
        public string Brand { get; set; }
        public long TotalCapacity { get; set; }
        public long FreeSpace { get; set; }

        public int GetUsagePercentage()
        {
            if (TotalCapacity == 0) return 0;
            return (int)(((TotalCapacity - FreeSpace) / (double)TotalCapacity) * 100);
        }
    }
}