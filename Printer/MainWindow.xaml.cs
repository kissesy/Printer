using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Threading;
using System.ComponentModel;
using System.Net;
using System.Management;
using Microsoft.Win32;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Printer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<DeviceItem> items = new ObservableCollection<DeviceItem>();
        //bool Scanning = false;
        Thread SearchDevice_Thread = null;
        private string subnet;
        private int Default_Port = 9332;

        private string SafeFileName;
        private FileStream fs;
        private byte[] FileBuf;

        //Thread SearchDevice_Thread = null;

        public MainWindow()
        {
            InitializeComponent();
            //Control.CheckForIllegalCrossThreadCalls = false;
            DeviceList.ItemsSource = items;
            
        }

        private byte[] ObjectToBytes(object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            //FIle Search한것에대해서도 체크 . IP와 port, NumberOf가 올바른 값인지 체크하기
            if ((IP_Text.Text == string.Empty)
                || (Port_Text.Text == string.Empty)
                || (NumberOf_Text.Text == string.Empty)
                || (FileName.Text == string.Empty))
            {
                MessageBox.Show("Input Blank!", "Caption");
            }
            else
            {
                //통신 되는지 아이피랑 포트로 통신 테스트 하기
                Packet packet = new Packet(SafeFileName, FileBuf.Length, FileBuf);
                TcpClient client = new TcpClient();
                client.Connect(IPAddress.Parse(IP_Text.Text), int.Parse(Port_Text.Text));
                if (client.Connected)
                {
                    NetworkStream ns = client.GetStream();
                    BinaryWriter writer = new BinaryWriter(ns);
                    byte[] data = System.Text.Encoding.ASCII.GetBytes("hello world");
                    //byte[] data = ObjectToBytes(packet);
                    //서버측은 length 받고, data 길이만큼 받기
                    
                    //writer.Write(data.Length);
                    writer.Write(data);
                }
            }
        }

        private void SearchFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                string extension = System.IO.Path.GetExtension(openFileDialog.FileName);
                if(extension != ".pdf"
                    && extension != ".txt")
                {
                    MessageBox.Show("Please Select Txt File or Pdf File!", "Caption");
                    return;
                }
                else
                {
                    FileName.Text = openFileDialog.FileName;
                    SafeFileName = openFileDialog.SafeFileName;
                    fs = new FileStream(FileName.Text, FileMode.Open, FileAccess.Read);
                    FileBuf = new byte[fs.Length];
                    fs.Read(FileBuf, 0, FileBuf.Length);
                    fs.Close();
                    //파일 버퍼 모으기  File.ReadAllText(openFileDialog.FileName);
                }
            }
        }



        private void SearchDevice_Click(object sender, RoutedEventArgs e)
        {
            if(SubNet_Text.Text == string.Empty)
            {
                MessageBox.Show("Input your Subnet", "Caption");
                Keyboard.Focus(SubNet_Text);
            }
            else
            {
                subnet = SubNet_Text.Text;
                items.Clear();

                SubNet_Text.IsEnabled = false;
                Search_Button.IsEnabled = false;
                StopSearch_Button.IsEnabled = true;

                SearchDevice_Thread = new Thread(DeviceScan);
                SearchDevice_Thread.IsBackground = true;
                //Thread SearchDevice_Thread = new Thread(() => DeviceStartScan(SubNet_Text.Text));
                SearchDevice_Thread.Start();
                if(SearchDevice_Thread.IsAlive == true)
                {
                    Search_Button.IsEnabled = false;
                    StopSearch_Button.IsEnabled = true;
                }
            }
            
        }

        public void DeviceStartScan()
        {
            
            for(int i = 1; i < 100000; i++)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    CurrentScanning.Text = "Scanning " + i.ToString();
                }));
            }
        }

        public void DeviceScan()
        {
            //MessageBox.Show("OK");
            int index = 0;
            Ping myPing;
            PingReply reply;
            IPAddress addr;
            IPHostEntry host;

            //
            this.Dispatcher.Invoke(new Action(() =>
            {
                Progress_Bar.Value = 0;
            }));

            for (int i = 1; i <255; i++)
            {
                string subnetn = "." + i.ToString();
                myPing = new Ping();
                reply = myPing.Send(subnet + subnetn, 900);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    CurrentScanning.Text = "Scanning " + subnet + subnetn + "....";
                }));
                
                if (reply.Status == IPStatus.Success)
                {
                    try
                    {
                        addr = IPAddress.Parse(subnet + subnetn);
                        host = Dns.GetHostEntry(addr);
                        //MessageBox.Show(subnet+subnetn);
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            items.Add(new DeviceItem() { IPAddress = subnet + subnetn, DeviceName = host.HostName });
                        }));
                        
                    }
                    catch
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            items.Add(new DeviceItem() { IPAddress = subnet + subnetn, DeviceName = "Could not Found" });
                        }));        
                        //MessageBox.Show("Couldn't retrieve hostname...","Caption");
                    }
                }
                this.Dispatcher.Invoke(new Action(() =>
                {
                    if(index > 46)
                    {
                        Progress_Bar.Value += 0.11;
                    }
                    else
                    {
                        Progress_Bar.Value += 5;
                    }
                    
                    int value = (int)((float)Progress_Bar.Value / 254.0f * 100);
                    index++;
                    Percent.Text = value.ToString()+"%";

                }));
                
            }
            this.Dispatcher.Invoke(new Action(() =>
            {
                Search_Button.IsEnabled = true;
                StopSearch_Button.IsEnabled = false;
                SubNet_Text.IsEnabled = true;
            }));

        }

        private void StopSearchDevice_Click(object sender, RoutedEventArgs e)
        {
            
            if(SearchDevice_Thread.IsAlive == true)
            {
                SearchDevice_Thread.Suspend();
            }

            Search_Button.IsEnabled = true;
            StopSearch_Button.IsEnabled = false;
            CurrentScanning.Text = "IDLE";
            SubNet_Text.IsEnabled = true;
            
        }

        private void DeviceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object item = DeviceList.SelectedItem;
            if(item != null)
            {
                IP_Text.Text = (item as DeviceItem).IPAddress;
                Port_Text.Text = Default_Port.ToString();
                NumberOf_Text.Text = "1";
            }
        }
    }

    public class Packet
    {
        public byte[] Data { get; private set; }
        public string FileName
        {
            get;
            private set;
        }
        public int Size
        {
            get;
            private set;
        }
        public Packet(string filename, int fileLength, byte[] buf)
        {
            FileName = filename;
            Data = buf;
            Size = fileLength;
        }
    }

    public class DeviceItem : INotifyPropertyChanged
    { 
        private string ipaddress;
        public string IPAddress { get { return this.ipaddress; }
            set {
                if(this.ipaddress != value)
                {
                    this.ipaddress = value;
                    this.NotifyPropertyChanged("IPAddress"); 
                }
            } 
        }
        private string devicename;
        public string DeviceName
        {
            get { return this.devicename; }
            set
            {
                if (this.devicename != value)
                {
                    this.devicename = value;
                    this.NotifyPropertyChanged("DeviceName");
                }
            }
        }


        //public string DeviceName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
