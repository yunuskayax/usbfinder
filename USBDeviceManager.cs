using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USB_Finder
{
    public class USBDeviceInfo
    {
        public string Name { get; set; }
        public string SerialNumber { get; set; }
        public string DriveLetter { get; set; }
        public DateTime EventDate { get; set; }
        public bool IsNew { get; set; }
        public string Brand { get; set; }
        public long TotalCapacity { get; set; }
        public long FreeSpace { get; set; }

        public USBDeviceInfo() { }

        public USBDeviceInfo(string name, string serialNumber, string driveLetter, bool isNew, string brand, long totalCapacity, long freeSpace)
        {
            Name = name;
            SerialNumber = serialNumber;
            DriveLetter = driveLetter;
            EventDate = DateTime.Now;
            IsNew = isNew;
            Brand = brand;
            TotalCapacity = totalCapacity;
            FreeSpace = freeSpace;
        }

        public int GetUsagePercentage()
        {
            if (TotalCapacity == 0) return 0;
            return (int)(((TotalCapacity - FreeSpace) / (double)TotalCapacity) * 100);
        }
    }


    public class USBDeviceManager
    {
        public List<USBDeviceInfo> USBDevices { get; private set; }

        public USBDeviceManager()
        {
            USBDevices = new List<USBDeviceInfo>();
        }

        public void AddDevice(string name, string serialNumber, string driveLetter, bool isNew, string brand, long totalCapacity, long freeSpace)
        {
            USBDevices.Add(new USBDeviceInfo
            {
                Name = name,
                SerialNumber = serialNumber,
                DriveLetter = driveLetter,
                EventDate = DateTime.Now,
                IsNew = isNew,
                Brand = brand,
                TotalCapacity = totalCapacity,
                FreeSpace = freeSpace
            });
        }

        public void AddDevice(USBDeviceInfo deviceInfo)
        {
            USBDevices.Add(deviceInfo);
        }

        public void RemoveDevice(string driveLetter)
        {
            var device = USBDevices.Find(d => d.DriveLetter == driveLetter);
            if (device != null)
            {
                USBDevices.Remove(device);
            }
        }
    }


}

