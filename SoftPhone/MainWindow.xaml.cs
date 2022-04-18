
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
using System.Web.Script.Serialization;
using MaterialDesignThemes.Wpf;
using SocketIOClient;
using System.Diagnostics;
using System.Text.Json.Serialization;
using SocketIOClient.JsonSerializer;
using System.Text.Json;

namespace SoftPhone
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public SpeechSynthesizer debugger;
        public BackgroundWorker worker;
        public string currentChatId = "-1";
        public bool isAppInit = false;
        public string myContactId = "613f01ca19964e0016370b83";
        public SocketIO client = null;
        public SystemTextJsonSerializer jsonSerializer = null;

        public MainWindow()
        {
            InitializeComponent();

            debugger = new SpeechSynthesizer();
            worker = new BackgroundWorker();
            worker.DoWork += delegate
            {
                ListenSockets();
            };

            worker.RunWorkerAsync();
            
            CheckUser();

            GetChats("");

        }

        public void CheckUser()
        {
            Environment.SpecialFolder localApplicationDataFolder = Environment.SpecialFolder.LocalApplicationData;
            string localApplicationDataFolderPath = Environment.GetFolderPath(localApplicationDataFolder);
            string saveDataFilePath = localApplicationDataFolderPath + @"\OfficeWare\Messenger\save-data.txt";
            /*using (Directory.CreateDirectory(localApplicationDataFolderPath + @"\OfficeWare"))
            {
                using (Directory.CreateDirectory(localApplicationDataFolderPath + @"\OfficeWare\Messenger"))
                {
                    using (File.Create(localApplicationDataFolderPath + @"\OfficeWare\Messenger"))
                    {

                    }
                }
            }*/
            string messengerPath = localApplicationDataFolderPath + @"\OfficeWare\Messenger";
            bool isMessengerFolderExists = Directory.Exists(messengerPath);
            bool isMessengerFolderNotExists = !isMessengerFolderExists;
            if (isMessengerFolderNotExists)
            {
                Directory.CreateDirectory(messengerPath);
                using (Stream s = File.Open(saveDataFilePath, FileMode.OpenOrCreate))
                {
                    using (StreamWriter sw = new StreamWriter(s))
                    {
                        sw.Write("");
                    }
                };
            }
            string contactId = File.ReadAllText(saveDataFilePath);
            int contactIdLength = contactId.Length;
            bool isContactIdNotExists = contactIdLength <= 1;
            if (isContactIdNotExists)
            {
                Dialogs.ContactCreatorDialog dialog = new Dialogs.ContactCreatorDialog();
                dialog.Show();
                this.Close();
            }
            else
            {
                myContactId = contactId;
            }
        }

        public void CreateContactHandler(object sender, EventArgs e)
        {
            Window window = ((Window)(sender));
            object rawContactId = window.DataContext;
            myContactId = rawContactId.ToString();
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

        async public void SendMessage(string newMessage)
        {
            
            AddMessage(newMessage);

            String uriPath = "https://messengerserv.herokuapp.com/contacts/messages/add/?contactid=" + myContactId + "&othercontactid=" + currentChatId + "&message=" + newMessage;
            var webRequest = HttpWebRequest.Create(uriPath);
            webRequest.Method = "GET";
            try
            {
                using (var webResponse = webRequest.GetResponse())
                {
                    using (var reader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        var objText = reader.ReadToEnd();
                        ContactsListResponse myobj = (ContactsListResponse)js.Deserialize(objText, typeof(ContactsListResponse));
                        await client.EmitAsync("SendMessage", new ContactMessage
                        {
                            id = myContactId,
                            chatId = currentChatId,
                            msg = newMessage
                        });
                        Debugger.Log(0, "debug", "client socket receive message");
                    }
                }
            }
            catch (WebException)
            {
                debugger.Speak("Ошибка в запросе");
            }
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
            object rawCurrentChatId = currentChat.DataContext;
            currentChatId = ((string)(rawCurrentChatId));
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
            OutputMessages();
            chatBlock.Visibility = Visibility.Visible;
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
        
        /*private void ChatsFilterHandler(object sender, TextCompositionEventArgs e)
        {
            
        }*/

        public void AddMessage (string newMessage)
        {
            this.Dispatcher.Invoke(() =>
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
            });
        }

        async public void ListenSockets ()
        {
            try
            {
                // client = new SocketIO("http://localhost:4000/");
                client = new SocketIO("https://messengerservsockets.herokuapp.com/");
                client.OnConnected += async (sender, e) =>
                {
                    Debugger.Log(0, "debug", "client socket conntected");
                };
                client.On("ReceiveMessage", async response => {
                    ContactMessage contactMessage = response.GetValue<ContactMessage>();
                    string id = contactMessage.id;
                    string chatId = contactMessage.chatId;
                    string message = contactMessage.msg;
                    Debugger.Log(0, "debug", "client socket receive message: ");
                    Debugger.Log(0, "debug", Environment.NewLine + "id: " + id + Environment.NewLine);
                    Debugger.Log(0, "debug", Environment.NewLine + "chatId: " + chatId + Environment.NewLine);
                    Debugger.Log(0, "debug", Environment.NewLine + "message: " + message + Environment.NewLine);
                    if (id == currentChatId)
                    {
                        if (chatId == myContactId)
                        {
                            AddMessage(message);
                        }
                    }
                });
                jsonSerializer = client.JsonSerializer as SystemTextJsonSerializer;
                jsonSerializer.OptionsProvider = () => new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                await client.ConnectAsync();
            }
            catch (System.Net.WebSockets.WebSocketException)
            {

            }
        }

        public void GetChats(string search)
        {
            if (isAppInit)
            {
                String uriPath = "https://messengerserv.herokuapp.com/contacts/list";
                var webRequest = HttpWebRequest.Create(uriPath);
                webRequest.Method = "GET";
                try
                {
                    using (var webResponse = webRequest.GetResponse())
                    {
                        using (var reader = new StreamReader(webResponse.GetResponseStream()))
                        {
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            var objText = reader.ReadToEnd();
                            ContactsListResponse myobj = (ContactsListResponse)js.Deserialize(objText, typeof(ContactsListResponse));
                            List<ContactResponse> contacts = myobj.contacts;
                            chats.Children.Clear();
                            foreach (ContactResponse contact in contacts)
                            {
                                String id = contact._id;
                                String name = contact.name;
                                String phone = contact.phone;
                                String avatar = contact.avatar;
                                bool isMyContactId = id == myContactId;
                                bool isNotMyContactId = !isMyContactId;
                                int searchLength = search.Length;
                                bool isEmptySearchLength = searchLength <= 0;
                                bool isNotEmptySearchLength = !isEmptySearchLength;
                                string insensitiveCaseContactName = name.ToLower();
                                bool isContactNameIncludedSearch = insensitiveCaseContactName.Contains(search);
                                bool isMatchBySearch = isNotEmptySearchLength && isContactNameIncludedSearch;
                                bool isChatSearched = isEmptySearchLength || isMatchBySearch;
                                bool isPrintChat = isNotMyContactId && isChatSearched;
                                if (isPrintChat)
                                {
                                    StackPanel chat = new StackPanel();
                                    chat.Orientation = Orientation.Horizontal;
                                    chat.Background = System.Windows.Media.Brushes.White;
                                    chat.Height = 65;
                                    chat.DataContext = id;
                                    chat.MouseEnter += HoverChatHandler;
                                    chat.MouseLeave += HoverChatHandler;
                                    chat.MouseUp += SelectChatHandler;
                                    Image chatAvatar = new Image();
                                    chatAvatar.Width = 50;
                                    chatAvatar.Height = 50;
                                    chatAvatar.BeginInit();
                                    bool isAvatarUriNotEmpty = avatar.Length >= 1;
                                    bool isAvatarUriReaded = Uri.IsWellFormedUriString(avatar, UriKind.RelativeOrAbsolute);
                                    bool isAvatarLoaded = isAvatarUriNotEmpty && isAvatarUriReaded;
                                    if (isAvatarLoaded)
                                    {
                                        try
                                        {
                                            chatAvatar.Source = new BitmapImage(new Uri(avatar));
                                        }
                                        catch (UriFormatException)
                                        {
                                            LoadDefaultAvatar(chatAvatar);
                                        }
                                    }
                                    else
                                    {
                                        LoadDefaultAvatar(chatAvatar);
                                    }
                                    chatAvatar.ImageFailed += AvatarNotLoadedHandler;
                                    chatAvatar.EndInit();
                                    chat.Children.Add(chatAvatar);
                                    StackPanel chatCenter = new StackPanel();
                                    chatCenter.VerticalAlignment = VerticalAlignment.Center;
                                    chatCenter.Margin = new Thickness(25, 0, 25, 0);
                                    TextBlock chatCenterFriendNameLabel = new TextBlock();
                                    chatCenterFriendNameLabel.Text = name;
                                    chatCenterFriendNameLabel.Margin = new Thickness(0, 5, 0, 5);
                                    chatCenter.Children.Add(chatCenterFriendNameLabel);
                                    TextBlock chatCenterLastMessageLabel = new TextBlock();
                                    chatCenterLastMessageLabel.Text = "Последнее сообщение";
                                    chatCenter.Children.Add(chatCenterLastMessageLabel);
                                    chat.Children.Add(chatCenter);
                                    StackPanel chatAside = new StackPanel();
                                    chatAside.VerticalAlignment = VerticalAlignment.Center;
                                    chatAside.Orientation = Orientation.Horizontal;
                                    PackIcon chatAsideCheckIcon = new PackIcon();
                                    chatAsideCheckIcon.Kind = PackIconKind.CheckAll;
                                    chatAsideCheckIcon.Foreground = System.Windows.Media.Brushes.Green;
                                    chatAsideCheckIcon.Margin = new Thickness(5);
                                    chatAside.Children.Add(chatAsideCheckIcon);
                                    TextBlock chatAsideLastMessageDateTimeLabel = new TextBlock();
                                    chatAsideLastMessageDateTimeLabel.Text = "00:00";
                                    chatAsideLastMessageDateTimeLabel.Margin = new Thickness(5);
                                    chatAside.Children.Add(chatAsideLastMessageDateTimeLabel);
                                    chat.Children.Add(chatAside);
                                    chats.Children.Add(chat);
                                }
                            }
                        }
                    }
                }
                catch (WebException)
                {
                    debugger.Speak("Ошибка в запросе");
                }
            }
            else
            {
                isAppInit = true;
                GetChats("");
            }
        }

        public void AvatarNotLoadedHandler (object sender, RoutedEventArgs e)
        {
            Image chatAvatar = ((Image)(sender));
            LoadDefaultAvatar(chatAvatar);
        }

        public void LoadDefaultAvatar(Image chatAvatar)
        {
            chatAvatar.Source = new BitmapImage(new Uri("https://cdn3.iconfinder.com/data/icons/remixicon-system/24/checkbox-blank-circle-line-256.png"));
        }

        public void OutputMessages()
        {
            String uriPath = "https://messengerserv.herokuapp.com/contacts/list";
            var webRequest = HttpWebRequest.Create(uriPath);
            webRequest.Method = "GET";
            try
            {
                using (var webResponse = webRequest.GetResponse())
                {
                    using (var reader = new StreamReader(webResponse.GetResponseStream()))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        var objText = reader.ReadToEnd();
                        ContactsListResponse myobj = (ContactsListResponse)js.Deserialize(objText, typeof(ContactsListResponse));
                        debugger.Speak(myobj.contacts.Count.ToString());
                        List<ContactResponse> contacts = myobj.contacts;
                        foreach (ContactResponse contact in contacts)
                        {
                            String id = contact._id;
                            List<MessageResponse> contactMessages = contact.messages;
                            foreach (MessageResponse contactMessage in contactMessages)
                            {
                                String contactMessageContent = contactMessage.message;
                                string contactMessageId = contactMessage.id;
                                string contactOtherMessageId = contactMessage.otherMessageId;
                                bool isMyMessage = contactMessageId == currentChatId;
                                bool isOtherMessage = contactOtherMessageId == currentChatId;
                                bool isPrintMessage = isMyMessage || isOtherMessage;
                                if (isPrintMessage)
                                {
                                    StackPanel message = new StackPanel();
                                    message.Margin = new Thickness(10);
                                    message.Width = 200;
                                    message.Height = 50;
                                    TextBlock messageContent = new TextBlock();
                                    messageContent.Margin = new Thickness(5);
                                    messageContent.Text = contactMessageContent;
                                    if (isMyMessage)
                                    {
                                        message.Background = System.Windows.Media.Brushes.Blue;
                                        messageContent.Foreground = System.Windows.Media.Brushes.White;
                                        message.HorizontalAlignment = HorizontalAlignment.Right;
                                    }
                                    else
                                    {
                                        message.Background = System.Windows.Media.Brushes.White;
                                        messageContent.Foreground = System.Windows.Media.Brushes.Black;
                                        message.HorizontalAlignment = HorizontalAlignment.Left;
                                    }
                                    message.Children.Add(messageContent);
                                    messages.Children.Add(message);
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException)
            {
                debugger.Speak("Ошибка в запросе");
            }
        }

        private void ChatsFilterHandler(object sender, TextChangedEventArgs e)
        {
            TextBox chatsFilter = ((TextBox)(sender));
            string keywords = chatsFilter.Text.ToLower();
            /*List <StackPanel> hiddenChats = new List<StackPanel>();
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
            }*/
            GetChats(keywords);
        }

        private void ToggleBurgerMenuHandler(object sender, MouseButtonEventArgs e)
        {
            ToggleBurgerMenu();
        }

        public void ToggleBurgerMenu()
        {

        }

        private void ActivateSearchBoxHandler(object sender, MouseButtonEventArgs e)
        {
            ActivateSearchBox();
        }

        public void ActivateSearchBox()
        {
            searchBox.Focus();
        }

    }

    public class ContactsListResponse
    {
        public List<ContactResponse> contacts;
    }

    public class ContactResponse
    {
        public string _id;
        public string name;
        public string phone;
        public string avatar;
        public List<MessageResponse> messages;
    }

    public class MessageResponse
    {
        public string id;
        public string otherMessageId;
        public string message;
    }

    public class ContactMessage
    {
        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("chatId")]
        public string chatId { get; set; }

        [JsonPropertyName("msg")]
        public string msg { get; set; }

    }

}
