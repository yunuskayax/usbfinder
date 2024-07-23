using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USB_Finder
{
    public partial class FrmAnasayfa : Form
    {
        private USBWatcher usbWatcher;
        private USBDeviceManager usbDeviceManager;
        public FrmAnasayfa()
        {
            InitializeComponent();
            usbDeviceManager = new USBDeviceManager();
            usbWatcher = new USBWatcher();
            usbWatcher.USBInserted += OnUSBInserted;
            usbWatcher.USBRemoved += OnUSBRemoved;

            // Mevcut USB cihazlarını listele
            List<USBDeviceInfo> existingDevices = usbWatcher.EnumerateUSBDevices();
            foreach (var device in existingDevices)
            {
                usbDeviceManager.AddDevice(device.Name, device.SerialNumber, device.DriveLetter, device.IsNew, device.Brand, device.TotalCapacity, device.FreeSpace);
            }

            usbWatcher.Start();

            // Cihaz bilgilerini GroupBox içinde göster
            DisplayUSBInfo();

        }
        private void DisplayUSBInfo()
        {
            if (InvokeRequired)
            {
                // Ana iş parçacığında çalıştırmak için Invoke kullan
                Invoke(new Action(DisplayUSBInfo));
                return;
            }

            // Önceki Panel'leri temizle
            flowLayoutPanel1.Controls.Clear();

            foreach (var device in usbDeviceManager.USBDevices)
            {
                // Panel oluştur
                Panel panel = new Panel
                {
                    Size = new Size(300, 200), // Panel boyutunu artırdık
                    Margin = new Padding(10),
                    BackColor = device.IsNew ? Color.DarkGreen : Color.DarkRed, // Renk ayarlaması
                    BorderStyle = BorderStyle.FixedSingle // 3D görünüm için sınır
                };

                // Yazıların beyaz renkte olmasını sağla
                panel.ForeColor = Color.White;

                // Etiketler ve diğer bileşenler için uygun konumlar ayarla
                Label lblDriveName = new Label
                {
                    Text = "Drive Name: " + device.Name,
                    Location = new Point(10, 10),
                    AutoSize = true,
                    ForeColor = Color.White // Yazı rengini beyaz yap
                };

                Label lblBrand = new Label
                {
                    Text = "Brand: " + device.Brand,
                    Location = new Point(10, 30),
                    AutoSize = true,
                    ForeColor = Color.White // Yazı rengini beyaz yap
                };

                Label lblSerialNumber = new Label
                {
                    Text = "Serial Number: " + device.SerialNumber,
                    Location = new Point(10, 50),
                    AutoSize = true,
                    ForeColor = Color.White // Yazı rengini beyaz yap
                };
                lblSerialNumber.MouseClick += (s, e) => Clipboard.SetText(device.SerialNumber); // Seri numarayı kopyala

                Label lblDriveLetter = new Label
                {
                    Text = "Drive Letter: " + device.DriveLetter,
                    Location = new Point(10, 70),
                    AutoSize = true,
                    ForeColor = Color.White // Yazı rengini beyaz yap
                };

                // Bayt cinsinden kapasiteyi GB cinsine çevir
                double totalCapacityGB = device.TotalCapacity / (1024.0 * 1024 * 1024);
                double freeSpaceGB = device.FreeSpace / (1024.0 * 1024 * 1024);
                double usedSpaceGB = totalCapacityGB - freeSpaceGB;

                Label lblCapacity = new Label
                {
                    Text = "Total Capacity: " + totalCapacityGB.ToString("F2") + " GB",
                    Location = new Point(10, 90),
                    AutoSize = true,
                    ForeColor = Color.White // Yazı rengini beyaz yap
                };

                Label lblFreeSpace = new Label
                {
                    Text = "Free Space: " + freeSpaceGB.ToString("F2") + " GB",
                    Location = new Point(10, 110),
                    AutoSize = true,
                    ForeColor = Color.White // Yazı rengini beyaz yap
                };

                ProgressBar progressBar = new ProgressBar
                {
                    Location = new Point(10, 130),
                    Size = new Size(260, 20),
                    Maximum = (int)totalCapacityGB, // Kapasiteyi GB cinsinden ayarlayın
                    Value = (int)usedSpaceGB, // Kullanılan alanı gösterin
                    Style = ProgressBarStyle.Continuous
                };

                Button copyButton = new Button
                {
                    Text = "Kopyala",
                    Location = new Point(10, 160),
                    Size = new Size(260, 30),
                    ForeColor = Color.White, // Yazı rengini beyaz yap
                    BackColor = Color.DarkGray // Butonun arka plan rengini ayarla
                };
                copyButton.Click += (s, e) =>
                {
                    string textToCopy = $"{device.Name} {totalCapacityGB.ToString("F2")} GB S.N.: {device.SerialNumber}";
                    Clipboard.SetText(textToCopy);
                    MessageBox.Show("Bilgiler panoya kopyalandı: " + textToCopy);
                };

                // Panel'e kontrolleri ekle
                panel.Controls.Add(lblDriveName);
                panel.Controls.Add(lblBrand);
                panel.Controls.Add(lblSerialNumber);
                panel.Controls.Add(lblDriveLetter);
                panel.Controls.Add(lblCapacity);
                panel.Controls.Add(lblFreeSpace);
                panel.Controls.Add(progressBar);
                panel.Controls.Add(copyButton);

                // FlowLayoutPanel'a Panel'i ekle
                flowLayoutPanel1.Controls.Add(panel);
            }
        }

        private void OnUSBInserted(object sender, USBEventArgs e)
        {
            usbDeviceManager.AddDevice(e.DeviceName, e.SerialNumber, e.DriveLetter, true, e.Brand, e.TotalCapacity, e.FreeSpace);
            DisplayUSBInfo(); // Bu çağrı, InvokeRequired kontrolü ile ana iş parçacığı üzerinden yapılır
        }

        private void OnUSBRemoved(object sender, USBEventArgs e)
        {
            usbDeviceManager.RemoveDevice(e.DriveLetter);
            DisplayUSBInfo(); // Bu çağrı, InvokeRequired kontrolü ile ana iş parçacığı üzerinden yapılır
        }
        private void FrmAnasayfa_Load(object sender, EventArgs e)
        {

        }

       





        private void FrmAnasayfa_FormClosed(object sender, FormClosedEventArgs e)
        {
            usbWatcher.Stop();
            base.OnFormClosed(e);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            usbWatcher.Stop();
            Application.Exit();
        }

        private void ınfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmInfo frmInfo = new FrmInfo();
            frmInfo.ShowDialog();
        }
    }
}
