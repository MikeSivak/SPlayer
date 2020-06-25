using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NAudio.Wave;
using System.Net;
using System.IO;
using VideoLibrary;
using MediaToolkit.Model;
using MediaToolkit;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Threading;
using System.Linq;
using System.Windows.Media.Imaging;

namespace SPlayer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<musicList> items = new List<musicList>();
        private MediaElement media_Element = new MediaElement();
        bool Repeat = false; //variable for repeat of music;
        TextAccords textAccords = new TextAccords();
        
        public MainWindow()
        {
            InitializeComponent();
            HiddenForm.Click += (s, e) => WindowState = WindowState.Minimized;
            PowerOff.Click += (s, e) => Close();
        }

        public void db_Managment()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                string str_MusicName = Track_name.Text.ToString(); //Full name of current music
                string save_MusicName = str_MusicName; //Saving full name of music
                str_MusicName = new string(str_MusicName.TakeWhile(x => x != '-').ToArray()); //Cut full name for a search in db
                string search = str_MusicName.Replace(" ", "");
                string save_TextAccords = "";
                string save_Cover = "";
                string new_save = save_MusicName.Replace(" ", "");
                string path_Parent = Path.GetFullPath(@"..\..\");

                try
                {
                    using (DataModel.MusicContext db = new DataModel.MusicContext())
                    {
                        var music = db.Musics;
                        foreach (DataModel.Music m in music)
                        {
                            if (m.MusicId == search)
                            {
                                save_TextAccords = m.TextAccords_Path;
                                save_Cover = m.Cover_Path;
                            }
                        }
                    }
                }
                catch
                {
                    textAccords.txt.Text = "The Lyrics is not in the database :(";
                }

                try
                {
                    using (StreamReader sr = new StreamReader(path_Parent +
                       save_TextAccords + "/" + new_save + ".txt", System.Text.Encoding.UTF8))
                    {
                        textAccords.txt.Text = sr.ReadToEnd();
                    }

                }
                catch
                {
                    textAccords.txt.Text = "Lyrics is not found";
                }

                try
                {
                    cover_Circle.ImageSource = BitmapFrame.Create(new Uri(path_Parent +
                    save_Cover + "/" + search + ".jpg"));
                    
                }
                catch
                {
                    cover_Circle.ImageSource = BitmapFrame.Create(new Uri(path_Parent + "Covers/NotFound.jpg"));
                }
            }
            ));
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Volum.Value = media_Element.Position.TotalSeconds;
            Current_Duration.Text = media_Element.Position.ToString(@"mm\:ss");

            textAccords.Track.Text = Track_name.Text.ToString();
            textAccords.ToolTip = null;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            media_Element.Pause();
            Stop.Visibility = Visibility.Hidden;
            Play.Visibility = Visibility.Visible;
        }

        private void ListViewItem_Selected(object sender, RoutedEventArgs e)
        {
            media_Element.Source = new Uri((sender as musicList).Path, UriKind.RelativeOrAbsolute);
            media_Element.LoadedBehavior = MediaState.Manual;
            media_Element.UnloadedBehavior = MediaState.Manual;
            media_Element.Play();
            Play.Visibility = Visibility.Hidden;
            Stop.Visibility = Visibility.Visible;
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (media_Element.Source != null)
            {
                media_Element.Play();
                Play.Visibility = Visibility.Hidden;
                Stop.Visibility = Visibility.Visible;
            }
        }

        private void SwapItemsInList(int i, int itemsIndex)
        {
            var temp = items[i];
            items[i] = items[itemsIndex];
            items[itemsIndex] = temp;
        }

        private void Shaker_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            for (int i = 0; i < items.Count; i++)
            {
                int itemsIndex = rnd.Next(items.Count);
                SwapItemsInList(i, itemsIndex);
            }
            ListMusic.Items.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                items[i].NumberInLIst = i;
                ListMusic.Items.Add(items[i]);
            }
        }

        private void Choose_Track(object sender, RoutedEventArgs e)
        {
            if (media_Element.Source != null)
                media_Element.Stop();
            media_Element.Volume = Volume_Slider.Value;
            string MyPath = (sender as musicList).Path;
            Track_name.Text = Path.GetFileNameWithoutExtension(MyPath);
            media_Element.Source = new Uri(MyPath, UriKind.RelativeOrAbsolute);
            media_Element.LoadedBehavior = MediaState.Manual;
            media_Element.UnloadedBehavior = MediaState.Manual;
            media_Element.MediaOpened += GetDuration_MediaOpened;
            media_Element.Play();

            Volum.Value = media_Element.Position.TotalSeconds;
            Current_Duration.Text = media_Element.Position.ToString(@"mm\:ss");

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
            Play.Visibility = Visibility.Hidden;
            Stop.Visibility = Visibility.Visible;

            db_Managment();
        }

        void GetDuration_MediaOpened(object sender, RoutedEventArgs e)
        {
            Volum.Maximum = media_Element.NaturalDuration.TimeSpan.TotalSeconds;
            TimeSpan Ts = TimeSpan.FromSeconds(Math.Round(media_Element.NaturalDuration.TimeSpan.TotalSeconds));
            Full_Duration.Text = Ts.ToString(@"mm\:ss");

        }

        private void rollupSPlayer(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void dropSPlayer(object sender, MouseButtonEventArgs e)
        { this.Dispatcher.BeginInvoke(new Action(() =>
             {
                 DragMove();
             }     
          ));
        }

        private void musicDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] DropPath = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                foreach (string dropfilepath in DropPath)
                    AddMusic(dropfilepath);
            }
        }

        private void AddMusic(string dropfilepath)
        {
            if ((Path.GetExtension(dropfilepath).Contains(".mp3")) || (Path.GetExtension(dropfilepath).Contains(".wav")))
            {
                Mp3FileReader reader = new Mp3FileReader(Path.GetFullPath(dropfilepath));
                TimeSpan Ts = reader.TotalTime;
                musicList pi = new musicList(Path.GetFileNameWithoutExtension(dropfilepath), items.Count + 1, Ts);
                pi.Path = Path.GetFullPath(dropfilepath);
                bool IsFind = Find_in_List(pi);
                if (!IsFind)
                {
                    pi.musicItem.MouseDown += Choose_Track;
                    items.Add(pi);
                    ListMusic.Items.Add(pi);
                }
            }
        }

        private bool Find_in_List(musicList item)
        {
            bool isFind = false;
            foreach (var allitems in items)
            {
                if (allitems.Path == item.Path)
                    isFind = true;
            }
            return isFind;
        }

        private void Next_Track_Click(object sender, RoutedEventArgs e)
        {
            if (media_Element.Source != null)
            {
                bool IsFind = false;
                int i = 0;
                if (items.Count > 1)
                {
                    do
                    {
                        if (items[i].Path == media_Element.Source.LocalPath.ToString())
                            IsFind = true;

                        i++;
                    } while ((i < items.Count - 1) && (!IsFind));
                    if (i < items.Count)
                        media_Element.Source = new Uri(items[i].Path, UriKind.RelativeOrAbsolute);
                    else
                        media_Element.Stop();
                }
                else
                    media_Element.Stop();
                media_Element.LoadedBehavior = MediaState.Manual;
                media_Element.UnloadedBehavior = MediaState.Manual;

                Track_name.Text = System.IO.Path.GetFileNameWithoutExtension(items[i].Path);
                Volum.Value = media_Element.Position.TotalSeconds;
                Current_Duration.Text = media_Element.Position.ToString(@"mm\:ss");

                media_Element.Play();

                db_Managment();
            }
        }

        private void Pred_Track_Click(object sender, RoutedEventArgs e)
        {
            if (media_Element.Source != null)
            {
                bool IsFind = false;
                int i = items.Count - 1;
                if (i > 0)
                {

                    do
                    {
                        if (items[i].Path == media_Element.Source.LocalPath.ToString())
                            IsFind = true;

                        i--;
                    } while ((i > 0) && (!IsFind));
                    if (i >= 0)
                        media_Element.Source = new Uri(items[i].Path, UriKind.RelativeOrAbsolute);
                    else
                        media_Element.Stop();
                }
                else
                    media_Element.Stop();
                media_Element.LoadedBehavior = MediaState.Manual;
                media_Element.UnloadedBehavior = MediaState.Manual;

                Track_name.Text = System.IO.Path.GetFileNameWithoutExtension(items[i].Path);
                Volum.Value = media_Element.Position.TotalSeconds;
                Current_Duration.Text = media_Element.Position.ToString(@"mm\:ss");

                media_Element.Play();

                db_Managment();
            }
        }

        private void Volume_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (media_Element.Source != null)
            {
                media_Element.Volume = Volume_Slider.Value;
                if (media_Element.Volume == 0)
                {
                    Volume.Visibility = Visibility.Hidden;
                    Mute.Visibility = Visibility.Visible;
                }
                else
                {
                    Volume.Visibility = Visibility.Visible;
                    Mute.Visibility = Visibility.Hidden;
                }
            }
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            if (!Mute.IsChecked.Value)
                Volume_Slider.Visibility = Visibility.Hidden;
            else
                Volume_Slider.Visibility = Visibility.Visible;
        }

        private void Volume_Click(object sender, RoutedEventArgs e)
        {
            if (Volume.IsChecked.Value)
                Volume_Slider.Visibility = Visibility.Visible;
            else
                Volume_Slider.Visibility = Visibility.Hidden;
        }

        private void buttonQuestion(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Program name: SPlayer\nVersion: 1.0.4\nAuthor: Mike Sivak\nCopyright 2019\n");
        }

        private void Volum_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (media_Element.Source != null)
            {
                TimeSpan ts = TimeSpan.FromSeconds(e.NewValue);
                media_Element.Position = ts;

                if ((Volum.Value == Volum.Maximum) && (Repeat))
                {
                    media_Element.Stop();
                    media_Element.Play();
                }
                if ((Current_Duration.Text == Full_Duration.Text) && (!Repeat))
                    Next_Track_Click(sender, e);

                Volum.Value = media_Element.Position.TotalSeconds;
                Current_Duration.Text = media_Element.Position.ToString(@"mm\:ss");

            }
        }

        private void Replay_Click(object sender, RoutedEventArgs e)
        {
            if (media_Element.Source != null)
            {
                if (Replay.IsChecked.Value)
                {
                    Repeat = true;
                    Replay.Background = MainGrid.Background;
                }
                else
                {
                    Repeat = false;
                    Replay.Background = TopGrid.Background;
                }
            }
        }

        private void Show_TextAccords(object sender, RoutedEventArgs e)
        {
            Show.Visibility = Visibility.Hidden;
            Hide.Visibility = Visibility.Visible;
            if (media_Element.Source == null)
            {
                textAccords.Track.Text = "-No results-";
                textAccords.ToolTip = "Please add to music";
            }

            try
            {
                textAccords.Left = Left + Width;
                textAccords.Top = Top;
                textAccords.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Hide_TextAccords(object sender, RoutedEventArgs e)
        {
            textAccords.Hide();
            Hide.Visibility = Visibility.Hidden;
            Show.Visibility = Visibility.Visible;
        }

        private void drop_TextAccords(object sender, EventArgs e)
        {
            textAccords.Left = Left + Width;
            textAccords.Top = Top;
        }

        private void set_Child_Window(object sender, RoutedEventArgs e)
        {
            textAccords.Owner = this;
            Load_DB();
        }

        public void Load_DB()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    using (DataModel.MusicContext db = new DataModel.MusicContext())
                    {
                        DataModel.Music music_Nirvana = new DataModel.Music { MusicId = "Nirvana", TextAccords_Path = "TextAccords/Nirvana", Cover_Path = "Covers/Nirvana" };
                        DataModel.Music music_Rainbow = new DataModel.Music { MusicId = "Rainbow", TextAccords_Path = "TextAccords/Rainbow", Cover_Path = "Covers/Rainbow" };
                        DataModel.Music music_Scorpions = new DataModel.Music { MusicId = "Scorpions", TextAccords_Path = "TextAccords/Scorpions", Cover_Path = "Covers/Scorpions" };
                        DataModel.Music music_EdSheeran = new DataModel.Music { MusicId = "EdSheeran", TextAccords_Path = "TextAccords/EdSheeran", Cover_Path = "Covers/EdSheeran" };
                        db.Musics.Add(music_Nirvana);
                        db.Musics.Add(music_Rainbow);
                        db.Musics.Add(music_Scorpions);
                        db.Musics.Add(music_EdSheeran);

                        db.SaveChanges();
                    }
                }
                catch
                {
                }
            }));
        }

        private void SearchField_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchField.Text == "search for music on the Internet")
            {
                SearchField.Text = "";
                SearchField.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Purple"));
            }
        }

        private void SearchField_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchField.Text = "search for music on the Internet";
            SearchField.Foreground = Brushes.White;
        }

        private string MyParse(string songname)
        {
            string result = "";
            for (int i = 0; i < songname.Length; i++)
            {
                if (songname[i] == ' ')
                {
                    result += "+";
                }
                else
                    result += songname[i];
            }
            return result;
        }

        private void SearchField_KeyDown(object sender, KeyEventArgs e)
        {
                string answer = "";
                string SongName = SearchField.Text;

                if ((e.Key == Key.Enter) && (SongName != "search for music on the Internet") && (SongName != ""))
                {
                    var MyRequest = MyParse(SongName);
                    string url = $"http://ws.audioscrobbler.com/2.0/?method=track.search&track={MyRequest}&api_key=57ee3318536b23ee81d6b27e36997cde&format=json&limit=1";

                    try
                    {
                        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                        HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                        string response;
                        try
                        {
                            var streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                            response = streamReader.ReadToEnd();
                            string subString = "https://www.last.fm/music/";
                            answer = GetUrlFromResponse(response, subString);

                            httpWebRequest = (HttpWebRequest)WebRequest.Create(answer);
                            httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                            streamReader = new StreamReader(httpWebResponse.GetResponseStream());

                            response = streamReader.ReadToEnd();
                            subString = "https://www.youtube.com/watch";
                            answer = GetUrlFromResponse(response, subString);

                            Thread thread_SaveMP3 = new Thread(new ParameterizedThreadStart(SaveMP3));
                            thread_SaveMP3.Start(answer);
                        }
                        catch
                        {
                            MessageBox.Show($"\"{SongName}\" not found");
                        }
                    }
                    catch
                    {
                        MessageBox.Show("No connection to the Internet");
                    }

                }
        }

        private string GetUrlFromResponse(string response, string subString)
        {
            string answer = "";

            int indexOfSubstring = response.IndexOf(subString);
            if (indexOfSubstring > 0)
            {
                int i = indexOfSubstring;
                char findSymbol = '"';
                while ((response[i] != findSymbol) && (i < response.Length))
                {
                    answer += response[i];
                    i++;
                }
                if (i >= response.Length)
                    throw new ArgumentException();

                return answer;
            }
            else
                throw new ArgumentException();
        }

        private void SaveMP3(object VideoURL)
        {
            try
            {
                var youtube = YouTube.Default;
                var vid = youtube.GetVideo(VideoURL.ToString());

                File.WriteAllBytes(vid.FullName, vid.GetBytes());

                var inputFile = new MediaFile { Filename = vid.FullName };
                var outputFile = new MediaFile { Filename = $"{vid.FullName}.mp3" };

                using (var engine = new Engine())
                {
                    engine.GetMetadata(inputFile);

                    engine.Convert(inputFile, outputFile);
                }
                
                Dispatcher.BeginInvoke(new ThreadStart(delegate { AddMusic(outputFile.Filename); }));
            }
            catch
            {
                MessageBox.Show($"Your song not found");
            }

        }

        private void showSearch_Visible(object sender, RoutedEventArgs e)
        {
            SearchField.Height = 30;
            ListMusic.Height = 150;
            SearchField.Visibility = Visibility.Visible;
            show_SearchField.Visibility = Visibility.Hidden;
            hide_SearchField.Visibility = Visibility.Visible;
        }

        private void hideSearch_Visible(object sender, RoutedEventArgs e)
        {
            SearchField.Height = 0;
            ListMusic.Height = 170;
            SearchField.Visibility = Visibility.Hidden;
            hide_SearchField.Visibility = Visibility.Hidden;
            show_SearchField.Visibility = Visibility.Visible;
        }
    }
}
