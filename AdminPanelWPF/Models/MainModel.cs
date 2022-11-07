using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AdminPanelWPF.Models
{
    internal class MainModel
    {
        public enum EditMenu { Bold, Italic, Underlined, Paragraph, Left, Right, Center, Justify, Br, Hr };
        FtpWebRequest reqFTP;
        static string[] index = File.ReadAllLines("Config.ini");
        private static string FTPserver = index[0].ToString().Split('-')[1].Trim();
        private static string Login = index[1].ToString().Split('-')[1].Trim();
        private static string Password = index[2].ToString().Split('-')[1].Trim();

        public string FileName { get; set; }
        public string Page { get; set; }
        public string FileContent { get; set; }
        public int Progress { get; set; } = 0;
        public string Console { get; set; }
        public bool OpenFile(string filter)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = filter
            };
            if (ofd.ShowDialog() == true)
            {
                FTPMain(ofd.FileName);
                return true;
            }
            else return false;
        }
        public List<string> ListPages()
        {
            Connect("content/");
            reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
            reqFTP.UseBinary = true;
            FtpWebResponse resp = (FtpWebResponse)reqFTP.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            List<string> listPages = new List<string>();

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.Contains(".inc.php") && line != "feedback.inc.php")
                {
                    listPages.Add(line);
                }
            }
            reader.Close();
            resp.Close();
            return listPages;
        }
        public string ReadPage()
        {
            Connect($"content/{Page}");
            reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
            reqFTP.UseBinary = true;
            FtpWebResponse resp = (FtpWebResponse)reqFTP.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string text = reader.ReadToEnd();
            reader.Close();
            resp.Close();
            return text;
        }
        public string ReadAllPages()/// Чтение всех страниц, нахождение ссылок на файлы
        {
            string links = string.Empty;
            List<string> pages = ListPages();
            foreach (var item in pages)
            {
                Connect($"content/{item}");
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                reqFTP.UseBinary = true;
                FtpWebResponse resp = (FtpWebResponse)reqFTP.GetResponse();
                Stream stream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(stream);

                string line;
                while (reader.Peek() >= 0)
                {
                    line = reader.ReadLine().Trim().Replace("\t", "");
                    if (!line.Contains("Files"))
                        continue;
                    links += line;
                }
                reader.Close();
                resp.Close();
            }
            return links;
        }
        public List<string> CollectionFile()/// Собрать файлы
        {
            Connect("Files/");
            reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
            FtpWebResponse resp = (FtpWebResponse)reqFTP.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader reader = new StreamReader(stream);

            List<string> files = new List<string>();
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                files.Add(line);
            }
            reader.Close();
            resp.Close();
            return files;
        }
        public void DeleteFiles()
        {
            string links = ReadAllPages();
            List<string> files = CollectionFile();
            int count = 0;
            foreach (string item in files)
            {
                if (!links.Contains(item))/// Если файл отсутствует на всех страницах, удалить
                {
                    Connect("Files/" + item);
                    reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;
                    FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                    response.Close();
                    count++;
                }
            }
            Console = $"Удалено - {count} файлов!";
        }
        public void SavePage()
        {
            string path = @$"C:\Temp\{Page}";
            if (!Directory.Exists("C:\\Temp\\"))
            {
                Directory.CreateDirectory("C:\\Temp\\");
            }
            File.WriteAllText(path, FileContent);
            string uri = Connect($"content/");
            FTPUploadFile(uri, path, Page);/// Загружаем измененный файл
            Console = $"Страница успешно сохранена!";
        }
        private void PostfixName()/// Если название файлов совпадает, добавляем постфикс 
        {
            List<string> files = CollectionFile();
            int i = 0;
            foreach (string item in files)
            {
                i++;
                if (FileName == item)
                {
                    string extension = FileName.Substring(FileName.IndexOf('.'));
                    string name = FileName.Substring(0, FileName.IndexOf('.')) + i;
                    FileName = name + extension;
                }
            }
        }
        #region FTP
        private string Connect(string str = null)
        {
            string uri = "ftp://" + FTPserver + "/httpdocs/" + str;
            reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(uri));
            reqFTP.Credentials = new NetworkCredential(Login, Password);
            reqFTP.KeepAlive = false;
            return uri;
        }
        public void FTPMain(string path)
        {
            string uri = Connect("Files/");
            FileInfo info = new FileInfo(path);/// Получить информацию о файле
            FileName = info.Name;

            if (FTPCheckFolder("Files"))/// Проверяем, есть ли папка на фтп-сервере
            {
                PostfixName();
                FTPUploadFile(uri, path, FileName);/// Загружаем файл
            }
            else
            {
                FTPCreateFolder("Files");/// Создаем папку на фтп-сервере
                PostfixName();
                FTPUploadFile(uri, path, FileName);
            }
        }
        protected bool FTPCheckFolder(string nameFolder)
        {
            Connect();
            reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;/// Выбираем метод, который возвращает подробный список файлов на фтп-сервере
            reqFTP.UseBinary = true;
            FtpWebResponse resp = (FtpWebResponse)reqFTP.GetResponse();/// Получаем ответ от фтп-сервера           
            Stream stream = resp.GetResponseStream();/// Получаем поток данных
            StreamReader reader = new StreamReader(stream);
            var contents = reader.ReadToEnd();/// Считываем данные из потока
            reader.Close();
            resp.Close();
            //Разбиваем полученную строку на массив строк, проверяем есть ли там папка с именем nameFolder
            if (contents.Replace("\r\n", " ").Split(' ').Any(x => x == nameFolder))
                return true;
            return false;
        }
        protected void FTPUploadFile(string uri, string filePath, string fileName)
        {
            reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(uri + fileName));
            reqFTP.Credentials = new NetworkCredential(Login, Password);
            reqFTP.KeepAlive = false;
            FileInfo info = new FileInfo(filePath);
            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;/// Выбираем метод загрузки файла            
            reqFTP.UseBinary = true;/// Тип передачи файла          
            reqFTP.ContentLength = info.Length;/// Указываем размер файла

            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;
            //Открываем файловый поток
            FileStream fileStream = info.OpenRead();
            try
            {
                Stream stream = reqFTP.GetRequestStream();
                contentLen = fileStream.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    stream.Write(buff, 0, contentLen);
                    contentLen = fileStream.Read(buff, 0, buffLength);
                }
                stream.Close();
                fileStream.Close();
                Console = $"Загрузка выполнена успешно!";
            }
            catch (Exception ex)
            {
                Console = ex.Message;
            }
        }
        protected void FTPCreateFolder(string folder)
        {
            Connect(folder);
            reqFTP.Method = WebRequestMethods.Ftp.MakeDirectory;/// Выбираем метод создания папки
            reqFTP.UseBinary = true;
            FtpWebResponse resp = (FtpWebResponse)reqFTP.GetResponse();/// Получаем ответ от фтп-сервера
            resp.Close();
        }
        #endregion
    }
}
