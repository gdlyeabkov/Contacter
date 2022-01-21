using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace SoftPhone
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

    }

}
