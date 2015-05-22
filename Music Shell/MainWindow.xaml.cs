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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.Web;
using System.Windows.Threading;
using System.Windows.Resources;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.Net.NetworkInformation;
using System.Windows.Media.Animation;

namespace Music_Shell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    enum MediaState { Play, Pause }
    enum AppState { UserSongs, Friends, GlobalSearch }

    public partial class MainWindow : Window
    {
        private User user;
        private MediaPlayer player;
        private DispatcherTimer timer;
        private MediaState state = MediaState.Pause;
        private AppState appState = AppState.UserSongs;
        private bool IsToggle;

        public MainWindow()
        {
            InitializeComponent();
            Properties.Settings.Default.cachePath = Directory.GetCurrentDirectory() + "\\AudioCache";
            user = new User();
            AudioCache.GetAudio(rightListBox, user);
            leftListBox.SelectionChanged += new SelectionChangedEventHandler(leftListBox_SelectionChanged);
            rightListBox.SelectionChanged += new SelectionChangedEventHandler(rightListBox_SelectionChanged);
            player = new MediaPlayer();
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            player.MediaOpened += new EventHandler(player_MediaOpened);
            player.MediaEnded += new EventHandler(player_MediaEnded);
            player.MediaFailed += new EventHandler<ExceptionEventArgs>(player_MediaFailed);
            trackSlider.Tag = true;
        }

        private bool CheckInternet()
        {
            IPStatus status = IPStatus.BadRoute;
            try
            {
                Ping p = new Ping();
                PingReply pr = p.Send(@"vk.com");
                status = pr.Status;
            }
            catch { }
            if (status == IPStatus.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            if (brd.Height > 0)
                searchButton_Click(null, null);
        }

        private void authButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AuthorizationWindow authorizationWindow = new AuthorizationWindow();
                authorizationWindow.ShowDialog();

                if (user.Auth(usernameLabel, avatarImage, leftListBox, rightListBox))
                    authButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
            catch (Exception)
            {
                MessageBox.Show("Authorization error!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            TrayIcon.Icon = null;
        }

        void leftListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            rightListBox.Items.Clear();
            ListBox list = sender as ListBox;
            if (list.Items.Count != 0)
            {
                switch (appState)
                {
                    case AppState.UserSongs:
                        if (list.SelectedIndex == 0)
                            user.LoadAllSongs(200, rightListBox, 1);
                        else
                        {
                            Album selectedAlbum = user.albums[list.SelectedIndex - 1];
                            user.LoadSongsFromAlbum(selectedAlbum, rightListBox);
                        }
                        break;
                    case AppState.Friends:
                        user.currentFriendIndex = list.SelectedIndex;
                        user.friends[user.currentFriendIndex].loadAllSongs(200, rightListBox, user.tracks);
                        break;
                }
            }
        }

        void rightListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = sender as ListBox;
            int counterOfLoads = 0;

            if (list.Items.Count != 0)
            {
                user.currentSongIndex = list.SelectedIndex;


                if (AudioCache.IsInCache(list.SelectedItem.ToString()))
                    player.Open(new Uri(Properties.Settings.Default.cachePath + "/" + list.SelectedItem.ToString() + ".mp3"));
                else
                {
                    player.Open(new Uri(user.tracks[user.currentSongIndex].url));
                    Thread saveToAudioCache = new Thread(SaveToAudioCache);
                    saveToAudioCache.IsBackground = true;
                    saveToAudioCache.Start();
                }

                player.Play();
                state = MediaState.Play;
                playButton.Style = FindResource("pauseButton") as Style;
            }
        }

        private void SaveToAudioCache()
        {
            user.Save(user.currentSongIndex, true);
        }

        /// <summary>
        /// Метод для воспроизведения аудиозаписи
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void player_MediaOpened(object sender, EventArgs e)
        {
            MediaPlayer mp = sender as MediaPlayer;
            String time = mp.NaturalDuration.ToString();
            startTimeLabel.Content = "00:00";

            state = MediaState.Play;

            time = time.Remove(time.IndexOf('.'));
            time = time.Remove(0, 3);
            finishTimeLabel.Content = time;
            if (user.tracks[user.currentSongIndex].artist.Length + user.tracks[user.currentSongIndex].title.Length + 1 > 50)
            {
                titleLabel.Text = user.tracks[user.currentSongIndex].artist + " – " + user.tracks[user.currentSongIndex].title;
                labelMinTitle.Content = "";
            }
            else
            {
                labelMinTitle.Content = user.tracks[user.currentSongIndex].artist + " – " + user.tracks[user.currentSongIndex].title;
                titleLabel.Text = "";
            }

            trackSlider.Maximum = mp.NaturalDuration.TimeSpan.TotalSeconds;
            trackSlider.Value = 0;

            timer.Start();
            mp.Play();
        }


        void player_MediaEnded(object sender, EventArgs e)
        {
            if (user.currentSongIndex != user.tracks.Count() - 1)
            {
                user.currentSongIndex++;
                player.Open(new Uri(user.tracks[user.currentSongIndex].url));
                rightListBox.SelectedIndex = user.currentSongIndex;
            }
        }

        void player_MediaFailed(object sender, ExceptionEventArgs e)
        {
            MessageBox.Show("Error while loading audio", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (Convert.ToBoolean(trackSlider.Tag) != false)
            {
                trackSlider.Value = player.Position.TotalSeconds;
            }
            int currentSeconds = Convert.ToInt32(player.Position.TotalSeconds);
            int currentMinutes = currentSeconds / 60;
            currentSeconds = currentSeconds % 60;
            string min = currentMinutes.ToString();
            string sec = currentSeconds.ToString();
            min = (min.Length > 1) ? min : ("0" + min);
            sec = (sec.Length > 1) ? sec : ("0" + sec);
            startTimeLabel.Content = min + ":" + sec;
        }

        private void trackSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            trackSlider.Tag = true;
            player.Pause();
            player.Position = TimeSpan.FromSeconds(trackSlider.Value);
            player.Play();
        }

        private void trackSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            trackSlider.Tag = false;
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (user.checkToken())
            {
                if (user.Auth(usernameLabel, avatarImage, leftListBox, rightListBox))
                    authButton.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
            brd.Height = 0;
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (state == MediaState.Pause)
                {
                    user.currentSongIndex = (user.currentSongIndex < 0) ? 0 : user.currentSongIndex;

                    if (player.Source != null)
                    {
                        player.Play();
                        state = MediaState.Play;
                        playButton.Style = FindResource("pauseButton") as Style;
                    }
                    else if (user.tracks[user.currentSongIndex] != null)
                    {
                        string audioTitle = user.tracks[user.currentSongIndex].artist + "-" + user.tracks[user.currentSongIndex].title;
                        if (AudioCache.IsInCache(audioTitle))
                            player.Open(new Uri(Properties.Settings.Default.cachePath + "/" + audioTitle + ".mp3"));
                        else
                        {
                            player.Open(new Uri(user.tracks[user.currentSongIndex].url));
                        }
                        state = MediaState.Play;
                        playButton.Style = FindResource("pauseButton") as Style;
                    }
                }
                else
                {
                    state = MediaState.Pause;
                    player.Pause();
                    playButton.Style = FindResource("playButton") as Style;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Can't play track!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void soundButton_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double startVolume = player.Volume;
            if (e.Delta > 0 && player.Volume < 1.0)
            {
                player.Volume += 0.05;
                volumeLabel.Content = Convert.ToInt16(player.Volume * 100).ToString();
                if (player.Volume > 0 && startVolume == 0)
                    soundButton.Style = FindResource("soundButton1") as Style;
                else if (player.Volume > 0.33 && startVolume < 0.33)
                    soundButton.Style = FindResource("soundButton2") as Style;
                else if (player.Volume > 0.66 && startVolume < 0.66)
                    soundButton.Style = FindResource("soundButton3") as Style;
                else if (player.Volume == 0)
                    soundButton.Style = FindResource("soundButton0") as Style;
            }
            else if (e.Delta < 0 && player.Volume > 0.0)
            {
                player.Volume -= 0.05;
                volumeLabel.Content = Convert.ToInt16(player.Volume * 100).ToString();
                if (player.Volume < 100 && startVolume == 100)
                    soundButton.Style = FindResource("soundButton3") as Style;
                else if (player.Volume < 0.66 && startVolume > 0.66)
                    soundButton.Style = FindResource("soundButton2") as Style;
                else if (player.Volume < 0.33 && startVolume > 0.33)
                    soundButton.Style = FindResource("soundButton1") as Style;
                else if (player.Volume == 0)
                    soundButton.Style = FindResource("soundButton0") as Style;
            }
        }

        private void prevButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (player.Position.TotalSeconds > 5)
                {
                    player.Position = TimeSpan.FromMilliseconds(0);
                }
                else if (user.currentSongIndex != 0)
                {
                    user.currentSongIndex--;
                    player.Open(new Uri(user.tracks[user.currentSongIndex].url));
                    rightListBox.SelectedIndex = user.currentSongIndex;
                }
                else
                {
                    player.Pause();
                    player.Position = TimeSpan.FromMilliseconds(0);
                    state = MediaState.Pause;
                    playButton.Style = FindResource("playButton") as Style;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Can't play previous track!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (user.currentSongIndex < user.tracks.Count() - 1)
                {
                    user.currentSongIndex++;
                    player.Open(new Uri(user.tracks[user.currentSongIndex].url));
                    rightListBox.SelectedIndex = user.currentSongIndex;
                }
                else
                {
                    user.currentSongIndex = 0;
                    player.Open(new Uri(user.tracks[user.currentSongIndex].url));
                    rightListBox.SelectedIndex = user.currentSongIndex;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Can't play next track!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void friendsButton_Click(object sender, RoutedEventArgs e)
        {
            leftListBox.Items.Clear();
            rightListBox.Items.Clear();
            user.loadFriends(leftListBox);
            leftLabel.Content = "friends";
            appState = AppState.Friends;
        }

        private void userTracksButton_Click(object sender, RoutedEventArgs e)
        {
            leftListBox.Items.Clear();
            rightListBox.Items.Clear();
            user.LoadAlbums(leftListBox);
            appState = AppState.UserSongs;
            leftLabel.Content = "albums";
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation da = new DoubleAnimation();
            if (!IsToggle)
            {
                da.To = 40;
                da.Duration = TimeSpan.FromSeconds(0.35);
                brd.BeginAnimation(Border.HeightProperty, da);
                brd.BeginAnimation(Border.OpacityProperty, da);
                IsToggle = true;
            }
            else
            {
                da.To = 0;
                da.Duration = TimeSpan.FromSeconds(0.35);
                brd.BeginAnimation(Border.HeightProperty, da);
                brd.BeginAnimation(Border.OpacityProperty, da);
                IsToggle = false;
            }
            searchTextBox.Focus();
            searchTextBox.SelectAll();
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                searchTextBox.Visibility = Visibility.Hidden;

                if (searchTextBox.Text != "")
                {
                    leftListBox.Items.Clear();
                    rightListBox.Items.Clear();
                    user.globalSearch(searchTextBox.Text, 200, rightListBox);
                }
            }
        }

        private void searchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            searchTextBox.Visibility = Visibility.Hidden;
        }

        private void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.path != null)
            {
                Thread saveAudio = new Thread(SaveAudio);
                saveAudio.IsBackground = true;
                saveAudio.Start();
            }
        }

        private void SaveAudio()
        {
            user.Save(user.currentSongIndex);
        }
        #region Tray
        /// <summary>
        /// override для перегрузки абстрактной реализации метода( унаследовано от абстрактного метода OnSourceInitialized) 
        /// </summary>
        /// <param name="e"></param>

        protected override void OnSourceInitialized(EventArgs e)
        {
            // базовый функционал приложения в момент запуска для возможности последующего возврата
            base.OnSourceInitialized(e);
            createTrayIcon();
        }
        private System.Windows.Forms.NotifyIcon TrayIcon = null;
        private System.Windows.Controls.ContextMenu TrayMenu = null;

        private bool createTrayIcon()
        {
            bool result = false;
            if (TrayIcon == null)
            {
                TrayIcon = new System.Windows.Forms.NotifyIcon();
                TrayIcon.Icon = Music_Shell.Properties.Resources.icon128;
                TrayIcon.Text = "Music Shell";
                TrayMenu = Resources["TrayMenu"] as System.Windows.Controls.ContextMenu;// создание контекстного меню трея
                TrayIcon.Click += delegate(object sender, EventArgs e)//Делегат обрабатывающий щелчок мыши
                {
                    if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Left)
                        ShowHideMainWindow(sender, null);
                    else
                    {
                        TrayMenu.IsOpen = true;
                        Activate();
                    }
                };
                result = true;
            }
            else
                result = true;
            TrayIcon.Visible = true;
            return result;
        }
        private void ShowHideMainWindow(object sender, RoutedEventArgs e)
        {
            TrayMenu.IsOpen = false;
            if (IsVisible)
            {
                Hide();
                (TrayMenu.Items[0] as System.Windows.Controls.MenuItem).Header = "Показать";//Изменение надписи на пункте контектного меню
            }
            else
            {
                Show();

                (TrayMenu.Items[0] as System.Windows.Controls.MenuItem).Header = "Скрыть";//Изменение надписи на пункте контектного меню
                WindowState = CurrentWindowState;
                Activate();
            }
        }
        //Отмена отображения вкладки на панели задач
        private WindowState fCurrentWindowState = WindowState.Normal;
        /// <summary>
        /// Текущее отображение окна
        /// </summary>
        public WindowState CurrentWindowState
        {
            get { return fCurrentWindowState; }
            set { fCurrentWindowState = value; }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                Hide();//Если минимизированно окно
                (TrayMenu.Items[0] as System.Windows.Controls.MenuItem).Header = "Показать";
            }
            else
                CurrentWindowState = WindowState;//Запоминание текущего состояния окна
        }
        private bool fCanClose = false;
        public bool CanClose
        { //Возможность выхода из приложения
            get { return fCanClose; }
            set { fCanClose = value; }
        }

        //Определения обработчика выхода из приложения
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!CanClose) { }
        }
        #endregion

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void searchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                rightListBox.Items.Clear();
                user.globalSearch(searchTextBox.Text, 100, rightListBox);
            }
            else
            {
            }
        }

        private void authButton_MouseEnter(object sender, MouseEventArgs e)
        {
            avatarImage.Visibility = Visibility.Hidden;
        }

        private void authButton_MouseLeave(object sender, MouseEventArgs e)
        {
            avatarImage.Visibility = Visibility.Visible;
        }
    }
}
