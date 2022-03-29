using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Speech.Synthesis;

namespace SoftPhone.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для ContactCreatorDialog.xaml
    /// </summary>
    public partial class ContactCreatorDialog : Window
    {

        public SpeechSynthesizer debugger;

        public ContactCreatorDialog()
        {
            InitializeComponent();

            Initialize();

        }

        public void Initialize()
        {
            debugger = new SpeechSynthesizer();
        }


        private void CreateContactHandler(object sender, RoutedEventArgs e)
        {
            string phoneLabelContent = phoneLabel.Text;
            String uriPath = "https://messengerserv.herokuapp.com/contacts/create?name=" + phoneLabelContent + "&phone=" + phoneLabelContent;
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
                        CreateContactResponse myobj = (CreateContactResponse)js.Deserialize(objText, typeof(CreateContactResponse));
                        string id = myobj.id;
                        string status = myobj.status;
                        bool isStatusOk = status == "OK";
                        if (isStatusOk)
                        {
                            Environment.SpecialFolder localApplicationDataFolder = Environment.SpecialFolder.LocalApplicationData;
                            string localApplicationDataFolderPath = Environment.GetFolderPath(localApplicationDataFolder);
                            string saveDataFilePath = localApplicationDataFolderPath + @"\OfficeWare\Messenger\save-data.txt";
                            using (Stream s = File.Open(saveDataFilePath, FileMode.OpenOrCreate))
                            {
                                using (StreamWriter sw = new StreamWriter(s))
                                {
                                    sw.Write(id);
                                }
                            };
                            MainWindow mainWindow = new MainWindow();
                            mainWindow.Show();
                            this.Close();
                        }
                    }
                }
            }
            catch (WebException)
            {
                debugger.Speak("Ошибка в запросе");
            }
        }

    }

    class CreateContactResponse
    {
        public string id;
        public string status;
    }

}
