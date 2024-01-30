using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace karaoke
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            Console.WriteLine("מיקרופונים מחוברים:");
            comboboxDevices.Items.Clear();
            foreach (var device in devices)
            {
                Console.WriteLine($"- {device.DeviceFriendlyName}");
                comboboxDevices.Items.Add(device);
            }
            comboboxDevices.SelectedItem = comboboxDevices.Items[0];    
            var enumerato1r = new MMDeviceEnumerator();
            var devices1 = enumerato1r.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            Console.WriteLine("רמקולים והתקני פלט אודיו אחרים:");
            comboboxRamkol.Items.Clear();
            foreach (var device in devices1)
            {
                Console.WriteLine($"- {device.DeviceFriendlyName}");
                comboboxRamkol.Items.Add(device);
            }
            comboboxRamkol.SelectedItem = comboboxRamkol.Items[0];

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            Console.WriteLine("מיקרופונים מחוברים:");
            comboboxDevices.Items.Clear();
            foreach (var device in devices)
            {
                Console.WriteLine($"- {device.DeviceFriendlyName}");
                comboboxDevices.Items.Add(device);
            }

           
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MMDevice mMDevice = comboboxDevices.SelectedItem as MMDevice;
            MMDevice deviceRameol = comboboxRamkol.SelectedItem as MMDevice;   
            StartListening(mMDevice, deviceRameol);
        }
        WasapiCapture capture = null;
        WaveOutEvent waveOut = null;
        private void StartListening(MMDevice microphoneDevice, MMDevice speakerDevice)
        {

            capture = new WasapiCapture(microphoneDevice, true, (int)sliderAudioBufferMillisecondsLength.Value); // השתמש במצב Exclusive עם אפס עיכוב
            //capture = new WasapiCapture(microphoneDevice);  
           
            

            waveOut = new WaveOutEvent() { DeviceNumber = GetWaveOutDeviceNumber(speakerDevice) };
            var bufferedWaveProvider = new BufferedWaveProvider(capture.WaveFormat);

            capture.DataAvailable += (s, a) =>
            {
                bufferedWaveProvider.AddSamples(a.Buffer, 0, a.BytesRecorded);
            };
            capture.RecordingStopped += (s, a) =>
            {
                capture.Dispose();
                waveOut.Dispose();
            };
            capture.DataAvailable += capture_DataAvailable;

            waveOut.Init(bufferedWaveProvider);
            waveOut.Play();
            capture.StartRecording();
            Console.WriteLine("לחץ על כל מקש כדי להפסיק...");
    
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        { 

            waveOut.Stop();
            capture.StopRecording();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            Console.WriteLine("רמקולים והתקני פלט אודיו אחרים:");
            comboboxRamkol.Items.Clear();
            foreach (var device in devices)
            {
                Console.WriteLine($"- {device.DeviceFriendlyName}");
                comboboxRamkol.Items.Add(device);
            }
        }

        private static int GetWaveOutDeviceNumber(MMDevice speakerDevice)
        {
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                if (capabilities.ProductName == speakerDevice.DeviceFriendlyName)
                {
                    return i;
                }
            }
            return -1; // אם לא נמצא התקן מתאים
        }

        private void capture_DataAvailable(object sender, WaveInEventArgs e)
        {
            // חישוב עוצמת הקול או התנודות מנתוני האודיו
            // זה יכול להיות כל חישוב שרק תרצה, כאן זו רק דוגמה
            double sumLevel = 0;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                double sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                sumLevel -= Math.Abs(sample / 32768d);
            }
            double avgLevel = sumLevel / (e.BytesRecorded / 2);

            // עדכון הגרף
            UpdateGraph(avgLevel);
        }
        private void UpdateGraph(double level)
        {
            Dispatcher.Invoke(() =>
            {
                // הוספת נקודה חדשה לקו הגל
                PointCollection points = waveform.Points;
                if (points == null)
                    points = new PointCollection();

                // הזזת הנקודות הקיימות לצד
                for (int i = 0; i < points.Count; i++)
                {
                    Point p = points[i];
                    p.X -= 2; // הזזת כל נקודה לשמאל
                    points[i] = p;
                }

                // הוספת נקודה חדשה לסוף הקו
                points.Add(new Point(canvas.Width, level * canvas.Height));

                // הגבלת מספר הנקודות
                if (points.Count > canvas.Width / 2)
                    points.RemoveAt(0);

                waveform.Points = points;
            });
        }

    }
}