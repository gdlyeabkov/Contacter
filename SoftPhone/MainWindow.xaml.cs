using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Speech.Synthesis;
using System.ComponentModel;

namespace SoftPhone
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public SpeechSynthesizer debugger;
        public BackgroundWorker worker;

        public MainWindow()
        {
            InitializeComponent();

            SpeechSynthesizer debugger = new SpeechSynthesizer();
            worker = new BackgroundWorker();
            worker.DoWork += delegate
            {
                ListenSockets();
            };

            worker.RunWorkerAsync();

        }

        private void SendMessageHandler (object sender, RoutedEventArgs e)
        {
            string newMessage = preparedMessage.Text;
            bool isSendMessage = newMessage.Length >= 1;
            if (isSendMessage)
            {
                SendMessage(newMessage);
            }
        }

        private void UploadFileHandler(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            bool? res = ofd.ShowDialog();
            if (res != false)
            {
                Stream myStream;
                if ((myStream = ofd.OpenFile()) != null)
                {

                    string file_name = ofd.FileName;
                }
            }
        }

        public void SendMessage(string newMessage)
        {
            StackPanel newChatMessage = new StackPanel();
            newChatMessage.Background = System.Windows.Media.Brushes.White;
            newChatMessage.Width = 200;
            newChatMessage.Height = 50;
            newChatMessage.Margin = new Thickness(10);
            newChatMessage.HorizontalAlignment = HorizontalAlignment.Left;
            TextBlock newChatMessageLabel = new TextBlock();
            newChatMessageLabel.Text = newMessage;
            newChatMessageLabel.Margin = new Thickness(5);
            newChatMessage.Children.Add(newChatMessageLabel);
            messages.Children.Add(newChatMessage);
            preparedMessage.Text = "";
            chat.ScrollToEnd();
        }

        private void HoverChatHandler(object sender, MouseEventArgs e)
        {

        }

        private void HoutChatHandler(object sender, MouseEventArgs e)
        {

        }

        private void SelectChatHandler(object sender, MouseButtonEventArgs e)
        {
            HideChats();
            StackPanel currentChat = ((StackPanel)(sender));
            SelectChat(currentChat);
            OpenChat(currentChat);
        }

        public void HideChats ()
        {
            foreach (StackPanel hiddenChat in chats.Children)
            {
                hiddenChat.Background = System.Windows.Media.Brushes.White;
            }
        }

        public void SelectChat (StackPanel currentChat)
        {
            currentChat.Background = System.Windows.Media.Brushes.LightBlue;
        }

        public void OpenChat(StackPanel currentChat)
        {
            UpdateChatHeader(currentChat);
            ClearMessages();
        }

        public void UpdateChatHeader(StackPanel currentChat)
        {
            StackPanel chatInfo = ((StackPanel)(currentChat.Children[1]));
            TextBlock friendName = ((TextBlock)(chatInfo.Children[0]));
            StackPanel expandChatInfo = ((StackPanel)(currentChat.Children[1]));
            TextBlock friendWas = ((TextBlock)(expandChatInfo.Children[1]));
            string selectedFriendName = friendName.Text;
            string rawFriendWasInfo = friendWas.Text;
            string computedFriendWasDate = "час назад";
            friendNameChat.Text = selectedFriendName;
            friendWasChat.Text = computedFriendWasDate;
        }

        public void ClearMessages()
        {
            messages.Children.Clear();
        }
        
        private void ChatsFilterHandler(object sender, TextCompositionEventArgs e)
        {
            TextBox chatsFilter = ((TextBox)(sender));
            string keywords = chatsFilter.Text.ToLower();
            List <StackPanel> hiddenChats = new List<StackPanel>();
            foreach (StackPanel currentChat in chats.Children)
            {
                StackPanel chatInfo = ((StackPanel)(currentChat.Children[1]));
                TextBlock rawChatName = ((TextBlock)(chatInfo.Children[0]));
                string chatName = rawChatName.Text.ToLower();
                bool isNotNamesMatch = !chatName.Contains(keywords);
                if (isNotNamesMatch)
                {
                    hiddenChats.Add(currentChat);
                }
            }
            foreach (StackPanel hiddenChat in hiddenChats)
            {
                chats.Children.Remove(hiddenChat);
            }
        }

        public void ListenSockets()
        {
            string ip = "127.0.0.1";
            int port = 80;
            var server = new TcpListener(IPAddress.Parse(ip), port);
            server.Start();
            TcpClient client = server.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            /*Socket socket = server.AcceptSocket();
            socket.Send(Encoding.Unicode.GetBytes("mock data"));*/

            while (true)
            {
                while (!stream.DataAvailable) ;
                while (client.Available < 3) ; // match against "get"

                byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, client.Available);
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                {
                    // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    // 3. Compute SHA-1 and Base64 hash of the new value
                    // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    stream.Write(response, 0, response.Length);
                }
                else
                {
                    bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                    int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                        msglen = bytes[1] - 128, // & 0111 1111
                        offset = 2;

                    if (msglen == 126)
                    {
                        // was ToUInt16(bytes, offset) but the result is incorrect
                        msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                        offset = 4;
                    }
                    else if (msglen == 127)
                    {
                        Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
                        // i don't really know the byte order, please edit this
                        // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                        // offset = 10;
                    }

                    if (msglen == 0)
                        Console.WriteLine("msglen == 0");
                    else if (mask)
                    {
                        byte[] decoded = new byte[msglen];
                        byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                        offset += 4;

                        for (int i = 0; i < msglen; ++i)
                            decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                        string text = Encoding.UTF8.GetString(decoded);
                        debugger.Speak(text);
                    }
                    else
                        Console.WriteLine("mask bit not set");
                }
            }
        }

    }

}
