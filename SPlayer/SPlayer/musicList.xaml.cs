using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.IO;

namespace SPlayer
{
    /// <summary>
    /// Логика взаимодействия для musicList.xaml
    /// </summary>
    public partial class musicList : UserControl
    {
        public musicList(string title, int count, TimeSpan TotalTime)
            {
                InitializeComponent();
                lblTrack_Name.Text = title;
                Count.Text = Convert.ToString(count);
                lblDuration_Track.Text = TotalTime.ToString(@"mm\:ss");

                load_CoverSmall_Db();
            }

        public void load_CoverSmall_Db()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                string path_Parent = System.IO.Path.GetFullPath(@"..\..\");
                string str_save = lblTrack_Name.Text;
                str_save = new string(str_save.TakeWhile(x => x != '-').ToArray());
                string search = str_save.Replace(" ", "");
                string coverPath = "";

                try
                {
                    using (DataModel.MusicContext db = new DataModel.MusicContext())
                    {
                        var music = db.Musics;
                        foreach (DataModel.Music m in music)
                        {
                            if (m.MusicId == search)
                            {
                                coverPath = m.Cover_Path;
                            }
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("--not connection--", "Notification", MessageBoxButton.OK, MessageBoxImage.Hand);
                }

                try
                {
                    list_Cover.ImageSource = BitmapFrame.Create(new Uri(path_Parent +
                    coverPath + "/" + search + ".jpg"));
                }
                catch
                {
                    list_Cover.ImageSource = BitmapFrame.Create(new Uri(path_Parent + "Covers/NotFound.jpg"));
                }
            }));
        }

            public int NumberInLIst
            {
                get { return Convert.ToInt32(Count.Text); }
                set
                {
                    Count.Text = Convert.ToString(value + 1);
                }
            }

            public string Path { get; set; }
        }
}
