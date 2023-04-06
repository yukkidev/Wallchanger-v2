using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.ComponentModel;
using Brushes = System.Windows.Media.Brushes;
using System.Diagnostics;
using System.Windows.Threading;
using System.Timers;
using Xceed.Wpf.Toolkit;
using ControlzEx.Standard;

//using Wpf.Ui.Controls;

namespace Wallchanger_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]

        private static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32
        uiParam, String pvParam, UInt32 fWinIni);
        private static readonly UInt32 SPI_SETDESKWALLPAPER = 20;
        private static readonly UInt32 SPIF_UPDATEINIFILE = 0x1;

        private WPButton? selectedButton;
        private ScrollViewer? currentTab;
        private bool multiSelectMode;
        private bool closed = false;
        private bool closeToTray;
        private List<WPButton>? selectedButtonsList;

        public WPContainer? allTab;
        public WPContainer? favTab;
        public string wallpapersDirectory;
        public int currentWPIndex = 0; // resets when change in album in tray

        private int totalScheduleRows = 0;

        public MainWindow()
        {
            InitializeComponent();

            // check for files: albums.json, config.ini, and wallpapers dir, if they don't exist then create them.             
            CheckForDependantFilesAndFolders();

            LoadSettings();
            // dynamically add WPContainers as tabs
            AddWPContainers();

            // add event to trigger multi-select mode
            this.KeyDown += MainWindow_KeyDown;
        }

        // METHODS and functions

        public void CloseWindow()
        {
            closed = true;
            window.Close();
        }

        public void SetWP(string wallpaper)
        {
            _ = SystemParametersInfo(SPI_SETDESKWALLPAPER, 1, wallpaper, SPIF_UPDATEINIFILE);
        }
        public int RandomSetWP(List<string> images)
        {
            // random set from images list

            Random random = new();

            if (images.Count > 0)
            {
                int randomInt = random.Next(0, images.Count);
                string randomWP = images[randomInt];

                SetWP(randomWP);

                return randomInt;
            }

            return 0;

        }

        public void NextSetWP(List<string> images)
        {
            if (currentWPIndex < images.Count-1)
            {
                currentWPIndex++;
            }

            SetWP(images[currentWPIndex]);
        }

        public void PreviousSetWP(List<string> images)
        {
            if (currentWPIndex > 0)
            {
                currentWPIndex--;
            }

            SetWP(images[currentWPIndex]);
        }

        public void ActivateControlButtons()
        {
            if (!multiSelectMode)
            {
                if (((WPContainer)currentTab.Parent).Header.ToString() == "Favorites" && (currentTab.Parent as WPContainer).WPanel.Children.Contains(selectedButton))
                {
                    favButton.Content = "Unfavorite";
                }

                setButton.IsEnabled = true;
            }

            addToAlbumButton.IsEnabled = true;
            favButton.IsEnabled = true;
        }

        public void DeactivateControlButtons()
        {
            favButton.Content = "Favorite";

            addToAlbumButton.IsEnabled = false;
            favButton.IsEnabled = false;
            setButton.IsEnabled = false;
        }


        public Dictionary<string, List<string>> GetJsonFile(string fileName) => JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(fileName));

        public Dictionary<string, string> GetSettingsFile(string fileName) => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(fileName));


        public List<string> GetImagesViaPath(string folderName)
        {
            // get all images from path
            List<string> images = new(System.IO.Directory.EnumerateFiles(folderName, "*"));
            Console.WriteLine("Got images from path + " + folderName);
            return images;
        }

        public WPContainer GetTabItemFromHeader(string headerName)
        {
            WPContainer? tab = null;

            foreach (object wp in albumTabs.Items) // could optimize this, don't like the for loop every time a tab is added
            {
                if (wp is WPContainer)
                {
                    if ((wp as WPContainer).Header.ToString() == headerName)
                    {
                        tab = wp as WPContainer;
                    }
                }
            }

            return tab;
        }
        private void LoadSettings()
        {
            bool shouldRandomize = false;
            string randomizeAlbum = "";

            if (File.Exists("prefs.json"))
            {
                Dictionary<string, string> prefsJson = GetSettingsFile("prefs.json");

                closeToTrayCheckBox.IsChecked = bool.TryParse(prefsJson["close_to_tray"], out bool closeToTrayValue) ? closeToTrayValue : (bool?)null;
                checkForUpdatesCheckBox.IsChecked = bool.TryParse(prefsJson["check_for_updates"], out bool checkUpdatesValue) ? checkUpdatesValue : (bool?)null;
                themeComboBox.SelectedValue = prefsJson["theme"];
                
                randomizeOnStartupCheckBox.IsChecked = (bool.TryParse(prefsJson["randomize_on_start"], out bool randomizeValue) ? randomizeValue : (bool?)null);
                shouldRandomize = (bool)randomizeOnStartupCheckBox.IsChecked;
                
                randomizeFromComboBox.SelectedValue = randomizeAlbum = prefsJson["randomize_from_album"];
            }

            // randomize if should randomize
            if (shouldRandomize)
            {
                if (randomizeAlbum == null || randomizeAlbum == "")
                {
                    randomizeAlbum = "All";
                }

                this.RandomSetWP(GetImagesFromAlbum(randomizeAlbum));
            }

            // load in albums into randomizeFromComboBox
            randomizeFromComboBox.Items.Add("All");

            List<string> albumNames = GetAlbumKeysFromJson();
            
            foreach (string albumName in albumNames)
            {
                ComboBoxItem item = new()
                {
                    Content = albumName
                };
                randomizeFromComboBox.Items.Add(item);
            }

            // load schedules here and start them
        }

        private void QueueDeleteFile(String filePath)
        {
            // initialize GC
            GC.Collect();
            GC.WaitForPendingFinalizers();

            File.Delete(filePath);
        }

        private static List<string> GetAlbumKeysFromJson()
        {
            var albumsJson = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText("albums.json"));
            return albumsJson.Keys.ToList();
        }



        private void AddImagesToAlbum(List<string> images, string albumName)
        {
            // get json object
            var albumsJson = GetJsonFile("albums.json");

            // get the album from the json, if it doesn't exist, create it. 
            List<string> albumFromJson;

            if (!albumsJson.ContainsKey(albumName))
            {
                albumsJson[albumName] = new List<string>();
            }
            albumFromJson = albumsJson[albumName];

            // add the images from param to the album, then convert it to set to eliminate dupes, then back to list for the json. 
            albumFromJson.AddRange(images);
            HashSet<string> uniqueAlbumFromJson = new(albumFromJson);
            albumsJson[albumName] = uniqueAlbumFromJson.ToList<string>();

            // convert to string for json output, then write to file and reload containers. 

            WriteToJsonFile("albums.json", albumsJson);

            foreach (object tab in albumTabs.Items)
            {
                if (tab is WPContainer && (tab as WPContainer).Header.ToString() == albumName)
                {
                    WPContainer localTab = (tab as WPContainer);

                    AddButtons(images, localTab.WPanel);
                    ReloadWPContainers();

                    if (images.Count > 1)
                    {
                        localTab.DisableSPanel();
                    }

                    break;
                }
            }
        }

        private void ImportImages(string? albumName = null)
        {
            Microsoft.Win32.OpenFileDialog filesDialog = ImagesFileDialog();
            Nullable<bool> result = filesDialog.ShowDialog();

            if (result == true)
            {
                // copy images to wallpapers directory, add images to current tab if not all tab then reload current tab and all, otherwise just reload all tab. 
                foreach (string img in filesDialog.FileNames)
                {
                    string imgName = img.ToString().Split("\\").Last();
                    string newImgLocation = Directory.GetCurrentDirectory() + "\\wallpapers\\" + imgName;
                    try
                    {
                        //MessageBox.Show(newImgLocation);
                        File.Copy(img, newImgLocation, false); // from, to, don't overwrite

                        if (albumName != null && albumName != "All")
                        {
                            // load json, check if the key exists and load the data as a set, then add the new images. If any are duplicates, they won't show up twice. write that value back to the json.
                            List<string> imagesList = filesDialog.FileNames.ToList();
                            AddImagesToAlbum(imagesList, albumName);
                        }
                    }
                    catch (IOException iox)
                    {
                        Console.WriteLine(iox.Message);
                        if (!File.Exists(newImgLocation))
                        {
                            // something else happened other than same file exists O:
                            System.Windows.MessageBox.Show("Something else happened other than same file exists!\n" + iox.Message);
                        }
                    }
                }

                ReloadWPContainers();

            }
        }

        private void CheckForDependantFilesAndFolders()
        {
            // if no config.ini, and wallpapers dir, show first time popup
            // check for albums.json

            if (!File.Exists("albums.json"))
            {
                // create new json object with empty List<string> "Favorites"
                Dictionary<string, List<string>> newJsonObj = new()
                {
                    ["Favorites"] = new List<string>()
                };

                string jsonString = JsonConvert.SerializeObject(newJsonObj);
                File.WriteAllText(@"albums.json", jsonString);
            }

            if (!File.Exists("config.ini"))
            {
                File.Create("config.ini");
            }

            if (!Directory.Exists("wallpapers"))
            {
                Directory.CreateDirectory("wallpapers");
            }
        }


        private void WriteToJsonFile(string fileName, Dictionary<string, List<string>> jsonAlbumDict)
        {
            string jsonString = JsonConvert.SerializeObject(jsonAlbumDict);
            File.WriteAllText(fileName, jsonString);
        }

        private void SaveSettings(string fileName, Dictionary<string, string> settingsDict)
        {
            string jsonString = JsonConvert.SerializeObject(settingsDict);
            File.WriteAllText(fileName, jsonString);
        }

        private void AddButtons(List<string> images, WPPanel panel)
        {
            foreach (string image in images)
            {

                if (!panel.GetChildNames().Contains(image))
                {
                    WPButton btn = new(160, 90, image);

                    MenuItem removeImageMenuItem = new() { Header = "Remove" };

                    Separator separator = new();

                    MenuItem deleteImageMenuItem = new() { Header = "Delete" };

                    ContextMenu contextMenu = new();

                    contextMenu.Items.Add(removeImageMenuItem);
                    contextMenu.Items.Add(separator);
                    contextMenu.Items.Add(deleteImageMenuItem);

                    btn.WPButtonContextMenu = contextMenu;

                    // add menuItemButton events
                    removeImageMenuItem.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        removeImageMenuItem_Click(sender, e, btn);
                    };

                    deleteImageMenuItem.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        deleteImageMenuItem_Click(sender, e, btn); // uh im trying to delete all the images from each tab, so using albumTabs.Items delete all of one btn in each of their WPContainers IG
                    };

                    btn.Click += WPButton_Click;

                    btn.MouseDoubleClick += delegate (object sender2, MouseButtonEventArgs e2)
                    {
                        WPButton_DoubleClick(sender2, e2, btn.Path);
                    };

                    btn.MouseRightButtonUp += delegate (object sender3, MouseButtonEventArgs e3)
                    {
                        WPButton_MouseRightClickUp(sender3, e3, btn);
                    };

                    btn.multiselectCheckBox.Click += MultiselectCheckBox_Click;

                    panel.Children.Add(btn);
                }
            }
        }

        private String WPButtonToStringPath(WPButton btn) => btn.Path;

        private void AddAlbums(WrapPanel panel)
        {
            Dictionary<string, List<string>> albumsJson = GetJsonFile("albums.json");
            List<string> albums = albumsJson.Keys.ToList();

            foreach (string album in albums)
            {
                AlbumButton albumBtn = new(200, 200, album);

                if (albumsJson[album].Count > 0)
                {
                    albumBtn.CreateImage(albumsJson[album][0]);
                }

                MenuItem deleteAlbumButton = new() { Header = "Delete" };
                albumBtn.AlbumButtonContextMenu.Items.Add(deleteAlbumButton);

                // add menuItemButton events

                albumBtn.AlbumButtonContextMenu.MouseDown += delegate (object sender, MouseButtonEventArgs e)
                {
                    AlbumButtonContextMenu_MouseDown(sender, e);
                };

                albumBtn.Click += delegate (object sender, RoutedEventArgs e)
                {
                    AlbumButton_Click(sender, e, album);
                };

                panel.Children.Add(albumBtn);
            }
        }

        private WPContainer AddTabItem(string headerName, List<string>? images = null, int pos = -1, bool switch_to = false)
        {
            // remove it if it exists

            WPContainer? oldTab = GetTabItemFromHeader(headerName);

            albumTabs.Items.Remove(oldTab);

            ContentControl tabHeaderContentControl = new()
            {
                Content = headerName,
            };

            WPContainer tab = new(tabHeaderContentControl);

            if (headerName != "Albums") // if tabs are "All" or "Favorites," then you cannot close or delete them
            {

                if (headerName != "All" && headerName != "Favorites")
                {
                    ContextMenu _tabContextMenu = new();

                    MenuItem renameMenuItem = new() { Header = "Rename" };
                    MenuItem closeMenuItem = new() { Header = "Close" };
                    MenuItem deleteMenuItem = new() { Header = "Delete" };

                    _tabContextMenu.StaysOpen = false;
                    _tabContextMenu.Items.Add(renameMenuItem);
                    _tabContextMenu.Items.Add(closeMenuItem);
                    _tabContextMenu.Items.Add(new Separator());
                    _tabContextMenu.Items.Add(deleteMenuItem);

                    tabHeaderContentControl = new()
                    {
                        Content = headerName,
                        ContextMenu = _tabContextMenu
                    };

                    tab = new WPContainer(tabHeaderContentControl);

                    renameMenuItem.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        RenameMenuItem_Click(sender, e, tab);
                    };

                    closeMenuItem.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        CloseMenuItem_Click(sender, e, tab);
                    };

                    deleteMenuItem.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        DeleteMenuItem_Click(sender, e, tab, headerName);
                    };

                    tab.MouseRightButtonUp += delegate (object sender, MouseButtonEventArgs e)
                    {
                        tabHeader_MouseRightButtonUp(sender, e, tab);
                    };
                }

                if (headerName == "Favorites")
                {
                    ContextMenu _tabContextMenu = new();

                    MenuItem closeMenuItem = new() { Header = "Close" };

                    _tabContextMenu.StaysOpen = false;

                    _tabContextMenu.Items.Add(closeMenuItem);

                    tabHeaderContentControl = new()
                    {
                        Content = headerName,
                        ContextMenu = _tabContextMenu
                    };

                    tab = new WPContainer(tabHeaderContentControl);

                    closeMenuItem.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        CloseMenuItem_Click(sender, e, tab);
                    };

                    tab.MouseRightButtonUp += delegate (object sender, MouseButtonEventArgs e)
                    {
                        tabHeader_MouseRightButtonUp(sender, e, tab);
                    };
                }

                tab.addImagesButton.Click += delegate (object sender2, RoutedEventArgs e2)
                {
                    AddImagesButton_Click(sender2, e2, headerName);
                };

                tab.sViewer.AllowDrop = true;
                tab.sViewer.Drop += SViewer_Drop;

                tab.AllowDrop = true;
                tab.Drop += Tab_Drop;
            }

            if (images != null && images.Count > 0)
            {
                tab.DisableSPanel();
                AddButtons(images, tab.WPanel);

            } else if (images == null && headerName == "Albums")
            {
                //tab.SwitchTheme(currentTheme); // switches theme if tab is "Albums" tab
                tab.DisableSPanel();
                AddAlbums(tab.WPanel);
            }

            if (pos > -1)
            {
                albumTabs.Items.Insert(pos, tab);
            } else {
                albumTabs.Items.Add(tab);
            }

            // will add the album to the opened albums file
            if (headerName!= "Albums" && headerName != "All")
            {

                List<string> existingValues = File.ReadAllText("opened_albums.txt").Split(',').ToList();
                if (!existingValues.Contains(headerName))
                {
                    File.AppendAllText("opened_albums.txt", (existingValues.Count > 0 ? "," : "") + headerName);
                }

            }
            
            currentTab = tab.sViewer;

            if (switch_to == true)
            {
                albumTabs.SelectedItem = tab;
            }
            return tab;
        }

        private void AddWPContainers()
        {
            // load json file, for each key create a WPContainer, add to root, foreach container, add buttons, fix json if missing images

            Dictionary<string, List<string>> albumsJson = GetJsonFile("albums.json");
            bool fixJson = false;
            List<String> allImages = GetImagesViaPath(($"{Directory.GetCurrentDirectory()}\\wallpapers"));
            Dictionary<string, List<string>> newAlbumsJson = GetJsonFile("albums.json");

            // create All tab
            AddTabItem("Albums", null, 0); // special TabItem for opening albums

            TabItem separator = new()
            {
                IsEnabled = false,
                IsHitTestVisible = false,
                Content = new Separator
                {
                    Background = new SolidColorBrush(Colors.Transparent),
                    Opacity = 0,
                },

                Width = 10
            };
            separator.GotFocus += Separator_GotFocus;
            albumTabs.Items.Add(separator);

            allTab = AddTabItem("All", allImages, 2);
            //allTab.SwitchTheme(currentTheme);


            // go through each album and add the album to the TabSelector thingy
            List<string> openedAlbums = new();

            try
            {
                openedAlbums = File.ReadAllText("opened_albums.txt")
                  .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                  .ToList();
            }
            catch (FileNotFoundException ex)
            {
                File.CreateText("opened_albums.txt");
            }
            
            foreach (string album in openedAlbums)
            {
                List<String> imagePaths = albumsJson[album].ToList();

                foreach (String image in albumsJson[album])
                {
                    // go through each image in the album, and check if the image doesn't exist in images folder then delete it from the album list
                    if (!allImages.Contains(image)) {
                        imagePaths.Remove(image);

                        newAlbumsJson[album] = imagePaths;
                        fixJson = true;
                    }
                }


                if (albumsJson.ContainsValue(albumsJson[album]))
                {
                    if (album == "Favorites")
                    {
                        favTab = AddTabItem(album, imagePaths);
                    } else
                    {
                        AddTabItem(album, imagePaths);
                    }
                }
            }

            if (fixJson)
            {
                WriteToJsonFile("albums.json", newAlbumsJson);
            }

        }

        private void ActivateMultiSelectMode()
        {
            multiSelectMode = true;

            ActivateControlButtons();

            setButton.IsEnabled = false;
            favButton.Content = "Favorite"; // multiselect mode doesn't support unfavoriting multiple buttons, so just set this to "Favorite" always

            CancelSelelectedImagesButton.Visibility = Visibility.Visible;
            SelectedImagesText.Visibility = Visibility.Visible;


            foreach (object btn in (currentTab.Parent as WPContainer).WPanel.Children)
            {
                if (btn is WPButton)
                {
                    (btn as WPButton).EnableCheckBox();
                }
            }
        }

        private void DeactivateMultiSelectMode()
        {
            multiSelectMode = false;

            CancelSelelectedImagesButton.Visibility = Visibility.Hidden;
            SelectedImagesText.Visibility = Visibility.Hidden;


            if (currentTab != null)
            {
                foreach (var btn in from object btn in (currentTab.Parent as WPContainer).WPanel.Children
                                    where btn is WPButton
                                    select btn)
                {
                    (btn as WPButton).multiselectCheckBox.IsChecked = false;
                    (btn as WPButton).DisableCheckBox();
                }
            }
            DeactivateControlButtons();
        }

        private void DisableSPanelFromAlbum(string albumName)
        {
            foreach (var tab in from object tab in albumTabs.Items
                                where tab is WPContainer
                                where (tab as WPContainer).Header.ToString() == albumName
                                select tab)
            {
                (tab as WPContainer).DisableSPanel();
            }
        }

        private void ReloadWPContainers()
        {
            // reset the containers
            int oldIndex = albumTabs.SelectedIndex;

            albumTabs.Items.Clear();

            // re-add them
            AddWPContainers();

            if (oldIndex > albumTabs.Items.Count - 1)
            {
                oldIndex--;
            }

            currentTab = (albumTabs.Items[oldIndex] as WPContainer).sViewer; // set current tab
            albumTabs.SelectedIndex = oldIndex;
        }

        private void AddSelectedButtonToAlbum(string albumName)
        {
            if (multiSelectMode)
            {
                AddImagesToAlbum(selectedButtonsList.ConvertAll(new Converter<WPButton, String>(WPButtonToStringPath)), albumName);
            }
            else
            {
                List<string> strings = new()
                {
                    selectedButton.Path
                };
                AddImagesToAlbum(strings, albumName);
            }

            DisableSPanelFromAlbum(albumName);
        }

        private void RemovePathFromJsonAlbum(string path, string albumName)
        {
            Dictionary<string, List<string>> albumsJson = GetJsonFile("albums.json");
            albumsJson[albumName].Remove(path);
            WriteToJsonFile("albums.json", albumsJson);
        }

        private void RemoveSelectedButtonFromFavorites()
        {
            // remove button.Path from json
            RemovePathFromJsonAlbum(selectedButton.Path, "Favorites");
            favTab.RemoveButton(selectedButton.Path); // could probably refactor some other code to use this instead   

            if ((WPContainer)currentTab.Parent == favTab)
            {
                DeactivateControlButtons();
            }
        }

        private bool IsButtonInAlbum(WPButton btn, string albumName)
        {
            List<string> album = GetJsonFile("albums.json")[albumName];

            return album.Contains(btn.Path);
        }

        // EVENTS

        private void AlbumButtonContextMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // delete the album from here
        }

        private Microsoft.Win32.OpenFileDialog ImagesFileDialog()
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new()
            {
                Title = "Add some images!",
                Filter = "All image files|*.png;*.jpg;*.bmp;*.gif|png files (*.png)|*.png|jpg files (*.jpg)|*.jpg|bmp files (*.bmp)|*.bmp|gif files (*.gif)|*.gif",
                Multiselect = true
            };

            return openFileDlg;
        }

        private void HandleDrop(DragEventArgs e, string header)
        {

            List<string> files = new();
            List<string> fileNamesFixed = new();

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                files = ((string[])e.Data.GetData(DataFormats.FileDrop)).ToList();
            }
            else
            {
                return;
            }

            List<string> goodFileExts = new()
            {
                "png", "jpg", "bmp", "gif"
            };

            string wallpapersDir = Directory.GetCurrentDirectory() + "\\wallpapers\\";

            foreach (string file in files)
            {
                string imgName = file.ToString().Split("\\").Last();
                string newImgLocation = wallpapersDir + imgName;

                if (goodFileExts.Contains(imgName.Split(".").Last()))
                {
                    try
                    {
                        //MessageBox.Show(newImgLocation);
                        File.Copy(file, newImgLocation, false); // from, to, don't overwrite
                        fileNamesFixed.Add(newImgLocation);
                    }
                    catch (IOException iox)
                    {
                        Console.WriteLine(iox.Message);
                        if (!File.Exists(newImgLocation))
                        {
                            // something else happened other than same file exists O:
                            System.Windows.MessageBox.Show("Something else happened other than same file exists!\n" + iox.Message);
                        }

                    }
                }
            }
            if (header != "All")
            {
                AddImagesToAlbum(fileNamesFixed, header);
            }
        }

        private void SViewer_Drop(object sender, DragEventArgs e)
        {
            WPContainer tab = (WPContainer)(sender as ScrollViewer).Parent;
            string header = tab.Header.ToString();

            HandleDrop(e, header);

            ReloadWPContainers();
        }

        private void Tab_Drop(object sender, DragEventArgs e)
        {
            WPContainer tab = sender as WPContainer;
            string header = tab.Header.ToString();
            HandleDrop(e, header);

            ReloadWPContainers();
        }

        private void MultiselectCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox)
            {
                (sender as CheckBox).IsChecked = !(sender as CheckBox).IsChecked;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // check if key pressed is Esc and if multiSelectMode is enabled, if it is then disable it for all buttons in currentTab
            if ((e.Key is Key.Escape) && (this.multiSelectMode == true))
            {
                this.DeactivateMultiSelectMode();
            }

        }

        private void Separator_GotFocus(object sender, RoutedEventArgs e)
        {
            albumTabs.SelectedIndex += 1;
        }

        private void removeImageMenuItem_Click(object sender, RoutedEventArgs e, WPButton btn)
        {

            // removes it from album, doesn't work in All
            String header = (currentTab.Parent as WPContainer).Header.ToString();

            List<WPButton>? buttons = new() { btn };

            if (multiSelectMode)
            {
                buttons = selectedButtonsList;
            }

            if (header != "All")
            {
                foreach (WPButton _btn in buttons)
                {
                    //panel.Children.Remove(_btn);

                    Dictionary<string, List<string>> albumsJson = GetJsonFile("albums.json");
                    albumsJson[header].Remove(_btn.Path);
                    WriteToJsonFile("albums.json", albumsJson);
                }
            }

            //if (panel.Children.Count == 1)
            //{
            //    WPContainer currentContainer = currentTab.Parent as WPContainer;
            //    currentContainer.EnableSPanel();
            //}

            DeactivateMultiSelectMode();
            ReloadWPContainers();
        }

        private void deleteImageMenuItem_Click(object sender, RoutedEventArgs e, WPButton btn)
        {
            // delete btn from the wallpapers directory, remove it from every panel.Children

            List<WPContainer> tabs = new();

            foreach (object wPContainer in albumTabs.Items)
            {
                if (wPContainer is WPContainer)
                {
                    tabs.Add((WPContainer)wPContainer);
                }
            }



            List<WPButton> buttons = new() { btn };

            if (multiSelectMode)
            {
                buttons = selectedButtonsList;
            }

            foreach (WPButton _btn in buttons)
            {

                foreach (WPContainer wP in tabs) // foreach of the tabs WrapPanels
                {
                    // remove it from the dictionary
                    String header = wP.Header.ToString();
                    Dictionary<string, List<string>> albumsJson = GetJsonFile("albums.json");

                    if (header != "All" && header != "Albums")
                    {
                        albumsJson[header].Remove(_btn.Path);
                        WriteToJsonFile("albums.json", albumsJson);

                        AddTabItem(header, albumsJson[header]);

                    } else
                    {
                        wP.WPanel.Children.Remove(_btn);

                        //AddTabItem("All", GetImagesViaPath(($"{Directory.GetCurrentDirectory()}\\wallpapers")), 0); // for All tab
                    }
                }

                // queue delete it
                Task.Run(() => QueueDeleteFile(_btn.Path));

                ReloadWPContainers();
            }
        }

        private void AlbumButton_Click(object sender, RoutedEventArgs e, String albumName)
        {
            // add the clicked album to tabs
            // set it as the current selected album
            DeactivateControlButtons(); // so randomize button isn't active

            Dictionary<String, List<String>> albumsJson = GetJsonFile("albums.json");

            AddTabItem(albumName, albumsJson[albumName], 3, true);


        }

        private void RenameMenuItem_Click(object sender, RoutedEventArgs e, WPContainer tab)
        {

            MainBorder.Opacity = .5;

            renameAlbumPopup.PlacementTarget = window;
            renameAlbumPopup.Placement = PlacementMode.MousePoint;
            renameAlbumPopup.IsOpen = true;
        }


        private void CloseMenuItem_Click(object sender, RoutedEventArgs e, WPContainer tab)
        {
            // closes the tab if not "All"

            string header = tab.Header.ToString();

            if (header != "All")
            {
                albumTabs.Items.Remove(tab);
                currentTab = (albumTabs.Items[2] as WPContainer).sViewer;


                List<string> existingValues = File.ReadAllText("opened_albums.txt").Split(',').ToList();
                if (existingValues.Remove(header))
                {
                    // Only update the file if the value was actually removed
                    File.WriteAllText("opened_albums.txt", string.Join(",", existingValues));
                }

            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e, WPContainer tab, string headerName)
        {
            if (headerName != "All" && headerName != "Favorites")
            {
                Dictionary<string, List<string>> albumsJson = GetJsonFile("albums.json");

                albumsJson.Remove(headerName);

                WriteToJsonFile("albums.json", albumsJson);

                ReloadWPContainers();
            }
        }

        private void tabHeader_MouseRightButtonUp(object sender, MouseButtonEventArgs e, WPContainer tab)
        {

            // only shows the ContextMenu if mouse is over the tab itself
            TabItem tabItem = sender as TabItem;

            bool mouseOverHeader = (tabItem as UIElement).IsMouseOver;
            var mousePos = e.GetPosition(null);


            if (mouseOverHeader)
            {
                tab.tabHeaderContextMenu.IsOpen = true;
            }
        }

        private void AddImagesButton_Click(object sender, RoutedEventArgs e, string currentTab) => ImportImages(currentTab);

        private void WPButton_Click(object sender, EventArgs e)
        {

            WPButton btn = (sender as WPButton);

            // check if "Shift" is held while this event goes off, if it is then set multiSelectMode = true
            if ((!multiSelectMode) && ((Keyboard.Modifiers & ModifierKeys.Shift) > 0))
            {
                selectedButtonsList = new();

                ActivateMultiSelectMode();
                selectedButtonsList.Add(btn);
                btn.multiselectCheckBox.IsChecked = true;

                SelectedImagesText.Text = selectedButtonsList.Count + " images selected";
            }
            else if (multiSelectMode)
            {
                btn.EnableCheckBox();
                btn.multiselectCheckBox.IsChecked = !btn.multiselectCheckBox.IsChecked;

                if (btn.multiselectCheckBox.IsChecked == true)
                {
                    selectedButtonsList.Add(btn);
                } else
                {
                    selectedButtonsList.Remove(btn);
                }

                SelectedImagesText.Text = selectedButtonsList.Count + " images selected";
            }
            else
            {

                if (IsButtonInAlbum(btn, "Favorites"))
                {
                    favButton.Content = "Unfavorite";
                }
                else
                {
                    favButton.Content = "Favorite";
                }

                ActivateControlButtons();
                selectedButton = btn;
            }
        }

        private static void WPButton_DoubleClick(object sender, MouseButtonEventArgs e, string path)
        {
            if (e.ChangedButton is MouseButton.Left)
            {
                _ = SystemParametersInfo(SPI_SETDESKWALLPAPER, 1, path, SPIF_UPDATEINIFILE);
            }
        }

        private void WPButton_MouseRightClickUp(object sender, MouseButtonEventArgs e, WPButton btn)
        {
            btn.WPButtonContextMenu.IsOpen = true;
            btn.WPButtonContextMenu.Focus();
        }

        private void optionsButton_Click(object sender, RoutedEventArgs e)
        {
            MainBorder.Opacity = .5;

            OptionsPopup.MaxHeight = window.Height;
            OptionsPopup.MaxWidth = window.Width - 200;
            OptionsPopup.Width = window.Width - 200;
            OptionsPopup.Height = window.Height - 250;

            OptionsPopup.MinHeight = 300;
            OptionsPopup.MinWidth = 450;

            OptionsPopup.PlacementTarget = window;
            OptionsPopup.Placement = PlacementMode.Center;
            OptionsPopup.IsOpen = true;
        }



        private void ApplyButton_Click(object sender, RoutedEventArgs e, System.Windows.Media.Color color)
        {
            Background = new SolidColorBrush(color);
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            addPopup.IsOpen = true;
            addPopup.Focus();
        }

        private void addToAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            //add selected images to an album
            Popup albumsPopup = new();
            StackPanel popupStack = new();
            List<String> albums = GetAlbumKeysFromJson();

            albumsPopup.Child = popupStack;

            foreach (String albumName in albums)
            {
                Button button = new()
                {
                    Content = albumName,
                    FontSize = 15.4,
                    MinWidth = 90

                };

                button.Click += delegate (object sender, RoutedEventArgs e)
                {
                    AddImageToAlbumButton_Click(sender, e, albumName, albumsPopup);
                };

                popupStack.Children.Add(button);
            }
            albumsPopup.IsOpen = true;
            albumsPopup.StaysOpen = false;
            albumsPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            albumsPopup.Focus();
        }

        private void AddImageToAlbumButton_Click(object sender, RoutedEventArgs e, string albumName, Popup popup)
        {
            AddSelectedButtonToAlbum(albumName);

            // close popup
            popup.IsOpen = false;
        }

        private void favButton_Click(object sender, RoutedEventArgs e)
        {
            // if this.selectMultiple mode is on, then add multiple otherwise just currentButton.Path
            if (!multiSelectMode)
            {
                if (favButton.Content == "Unfavorite")
                {
                    RemoveSelectedButtonFromFavorites();
                    favButton.Content = "Favorite";
                }
                else
                {
                    AddSelectedButtonToAlbum("Favorites");
                    favButton.Content = "Unfavorite";
                }
            }
            else
            {
                List<string> images = new();
                selectedButtonsList.ForEach(btn => images.Add(btn.Path));
                AddImagesToAlbum(images, "Favorites");
            }

            currentTab.Focus();
        }

        private List<string> GetImagesFromAlbum(string album)
        {
            if (album != "All")
            {
                Dictionary<String, List<String>> albumsJson = GetJsonFile(@"albums.json");
                return albumsJson[album];
            }

            // will return all
            return GetImagesViaPath(($"{Directory.GetCurrentDirectory()}\\wallpapers"));
        }
    

        private void randButton_Click(object sender, RoutedEventArgs e)
        {
            String header = (currentTab.Parent as WPContainer).Header.ToString();
            List<string> images = new();
            if (multiSelectMode)
            {
                images = selectedButtonsList.ConvertAll(new Converter<WPButton, String>(WPButtonToStringPath));
            }
            else if (header != "All" && header != "Albums")
            {
                Dictionary<String, List<String>> albumsJson = GetJsonFile(@"albums.json");
                images = albumsJson[header];
            } else {
                images = GetImagesViaPath(($"{Directory.GetCurrentDirectory()}\\wallpapers"));
            }

            RandomSetWP(images);
        }

        private void setButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SystemParametersInfo(SPI_SETDESKWALLPAPER, 1, selectedButton.Path, SPIF_UPDATEINIFILE);
        }

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DeactivateControlButtons();
            selectedButton = null;
        }

        private void AddImagesButton_Click(object sender, RoutedEventArgs e)
        {
            addPopup.IsOpen = false;
            WPContainer headerName = (WPContainer)currentTab.Parent;
            ImportImages(headerName.Header.ToString());
        }

        private void CreateAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            addPopup.IsOpen = false;
            createAlbumPopup.IsOpen= true;
            // create an album popup
        }

        private void CancelAddButton_Click(object sender, RoutedEventArgs e)
        {
            addPopup.IsOpen = false; // just closes popup
        }

        private void cancelCreateButton_Click(object sender, RoutedEventArgs e)
        {
            createAlbumPopup.IsOpen = false;
        }

        private void confirmCreateAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            string newAlbumName = NewAlbumName.Text;

            if (newAlbumName.Length > 0 && newAlbumName.ToLower() != "all" && newAlbumName.ToLower() != "favorites" && newAlbumName.ToLower() != "albums" && !newAlbumName.Contains(","))
            {
                // add to json as a new key,

                var albumsJson = GetJsonFile("albums.json");

                if (!albumsJson.ContainsKey(newAlbumName))
                {
                    albumsJson[newAlbumName] = new List<string>();
                }

                WriteToJsonFile("albums.json", albumsJson);

                createAlbumPopup.IsOpen = false;
                
                // reset text
                NewAlbumName.Text = "";

                // refresh the tabs 
                ReloadWPContainers();
    
                //AddTabItem(newAlbumName);
                albumTabs.SelectedIndex = albumTabs.Items.Count-1;

            }
            else
            {
                System.Windows.MessageBox.Show("Illegal album name! Cannot use: "+newAlbumName);
            }

        }

        private void albumTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                currentTab = (ScrollViewer)albumTabs.SelectedContent;
                DeactivateControlButtons();
                DeactivateMultiSelectMode();
            }
        }

        private void CancelSelelectedImagesButton_Click(object sender, RoutedEventArgs e)
        {
            DeactivateMultiSelectMode();
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            string closeToTrayStr = GetSettingsFile("prefs.json")["close_to_tray"];

            bool.TryParse(closeToTrayStr, out bool closeToTrayVal);

            closed = !closeToTrayVal;

            if (!closed)
            {
                e.Cancel = true;
                window.WindowState = System.Windows.WindowState.Minimized;
                window.Hide();
            }
        }

        private void OptionsPopup_Closed(object sender, EventArgs e)
        {
            MainBorder.Opacity = 1;
        }

        private void CloseOptions_Click(object sender, RoutedEventArgs e)
        {
            // save the settings to prefs.json

            Dictionary<string, string> settingsDict = new()
            {
                ["close_to_tray"] = closeToTrayCheckBox.IsChecked.ToString(),
                ["check_for_updates"] = checkForUpdatesCheckBox.IsChecked.ToString(),
                ["theme"] = themeComboBox.Text,
                ["randomize_on_start"] = randomizeOnStartupCheckBox.IsChecked.ToString(),
                ["randomize_from_album"] = randomizeFromComboBox.Text
            };

            SaveSettings("prefs.json", settingsDict);

            OptionsPopup.IsOpen = false;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void scheduleButton_Click(object sender, RoutedEventArgs e)
        {
            MainBorder.Opacity = .5;

            SchedulePopup.MaxHeight = window.Height;
            SchedulePopup.MaxWidth = window.Width - 200;
            SchedulePopup.Width = window.Width - 200;
            SchedulePopup.Height = window.Height - 250;

            SchedulePopup.MinHeight = 300;
            SchedulePopup.MinWidth = 450;

            SchedulePopup.PlacementTarget = window;
            SchedulePopup.Placement = PlacementMode.Center;
            SchedulePopup.IsOpen = true;
        }

        private void CloseSchedule_Click(object sender, RoutedEventArgs e)
        {
            SchedulePopup.IsOpen = false;
        }

        private void SchedulePopup_Closed(object sender, EventArgs e)
        {
            MainBorder.Opacity = 1;
        }

        private void cancelRenameButton_Click(object sender, RoutedEventArgs e)
        {
            renameAlbumPopup.IsOpen = false;
        }

        private void renameAlbumPopup_Closed(object sender, EventArgs e)
        {
            MainBorder.Opacity = 1;
        }

        private void confirmRenameButton_Click(object sender, RoutedEventArgs e)
        {
            renameAlbumPopup.IsOpen = false;
            string newAlbumName = renameText.Text;
            string oldAlbumName = ((WPContainer)currentTab.Parent).Header.ToString();

            var albumDict = GetJsonFile("albums.json");

            albumDict[newAlbumName] = albumDict[oldAlbumName];
            albumDict.Remove(oldAlbumName);

            WriteToJsonFile("albums.json", albumDict);

            ReloadWPContainers();

            renameText.Text = "";
        }

        private ComboBox createComboBoxWithItems(string[] items)
        {
            ComboBox comboBox = new();

            foreach(string item in items)
            {
                comboBox.Items.Add(item);
            }

            return comboBox;
        }

        public StackPanel beginCreateNewSchedule(int row)
        {
            StackPanel scheduleCreationStack = new()
            {
                Margin = new Thickness(5),
                Orientation = Orientation.Horizontal,
                Background = Brushes.LightGray
            };
            scheduleCreationStack.Margin = new Thickness(4);

            // create type label
            Label typeLabel = new() { Content = "Schedule Type: " };

            // create type Combo
            ComboBox typeCombo = createComboBoxWithItems(new string[]{"Set Time", "Recurring"});

            // upon the selection changing create a new event handler with custom args
            typeCombo.SelectionChanged += delegate (object sender, SelectionChangedEventArgs e)
            {
                TypeCombo_SelectionChanged(sender, e, scheduleCreationStack);
            };

            // add to the scheduleCreationStack
            scheduleCreationStack.Children.Add(typeLabel);
            scheduleCreationStack.Children.Add(typeCombo);

            // add scheduleCreationStack to main stack always on top
            Grid.SetColumn(scheduleCreationStack, 1);
            Grid.SetRow(scheduleCreationStack, row);

            scheduleGrid.Children.Add(scheduleCreationStack);

            return scheduleCreationStack;
        }

        private void NumericOnly(System.Object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = IsTextNumeric(e.Text);

        }

        private static bool IsTextNumeric(string str)
        {
            System.Text.RegularExpressions.Regex reg = new("[^0-9]");
            return reg.IsMatch(str);

        }

        private void scheduleCreateSetTime(StackPanel creationStack)
        {
            // create a from label and combo
            Label fromLabel = new() { Content="From album:"}; 
                                                              
            List<string> fromValues = GetAlbumKeysFromJson();
            fromValues.Insert(0, "All");
            ComboBox fromComboBox = createComboBoxWithItems(fromValues.ToArray()); // creates a combobox with the album names as items

            // create a time input with an am/pm combo
            Label timeLabel = new() { Content = "At" }; // 4
            //TextBox timeInput = new() { MinWidth=40, MaxWidth=50, MaxHeight = 25, MaxLength = 10 }; // 5

            DateTimeUpDown timePicker = new()
            {
                Format = DateTimeFormat.ShortTime
            };

            //timeInput.PreviewTextInput += NumericOnly;
            //ComboBox timeComboBox = createComboBoxWithItems(new string[]{ "AM", "PM" }); // 6

            // create a function combo - random/next/previous
            Label functionLabel = new() { Content = "Function:" }; // 7
            ComboBox functionCombo = createComboBoxWithItems(new string[] { "Random", "Next", "Previous" }); // 8

            creationStack.Children.Add(fromLabel);// 2
            creationStack.Children.Add(fromComboBox);// 3 
            creationStack.Children.Add(timeLabel); // 4
            creationStack.Children.Add(timePicker); // 5
            creationStack.Children.Add(functionLabel); // 6
            creationStack.Children.Add(functionCombo); // 7
        }

        private void scheduleCreateRecurring(StackPanel creationStack)
        {
            // create a from label and combo
            Label fromLabel = new() { Content = "From album:" };

            List<string> fromValues = GetAlbumKeysFromJson();
            fromValues.Insert(0, "All");
            ComboBox fromComboBox = createComboBoxWithItems(fromValues.ToArray()); // creates a combobox with the album names as items

            // create a recurring time input with seconds/hours/minutes combo
            Label timeLabel = new() { Content = "Every" };
            TextBox timeInput = new() { MinWidth = 40, MaxWidth = 50, MaxHeight=25, MaxLength=10 }; // ensure is integer
            timeInput.PreviewTextInput += NumericOnly;
            ComboBox timeComboBox = createComboBoxWithItems(new string[] { "seconds", "minutes", "hours", "days" });

            // create a function combo - random/next/previous
            Label functionLabel = new() { Content = "Function:" };
            ComboBox functionCombo = createComboBoxWithItems(new string[] { "Random", "Next", "Previous" });

            creationStack.Children.Add(fromLabel);
            creationStack.Children.Add(fromComboBox);
            creationStack.Children.Add(timeLabel);
            creationStack.Children.Add(timeInput);
            creationStack.Children.Add(timeComboBox);
            creationStack.Children.Add(functionLabel);
            creationStack.Children.Add(functionCombo);

        }

        private void newScheduleButton_Click(object sender, RoutedEventArgs e)
        {

            // add a new row
            StackPanel creationStack = beginCreateNewSchedule(totalScheduleRows);
            scheduleGrid.RowDefinitions.Add(new RowDefinition());

            // create a new checkbox, sched, and delete button
            CheckBox activeBox = new() { MinWidth=40, Padding=new Thickness(5), VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, IsChecked = false};
            // checkbox click callback
            
            activeBox.Checked += delegate (object sender, RoutedEventArgs e)
            {
                ActiveBox_Checked(sender, e, creationStack);
            };

            Grid.SetColumn(activeBox, 0);
            Grid.SetRow(activeBox, totalScheduleRows);
            scheduleGrid.Children.Add(activeBox);


            Button deleteButton = new() { Content = "Delete", MinWidth = 40, Padding = new Thickness(5)};
            // delete button callback
            deleteButton.Click += delegate (object sender, RoutedEventArgs e)
            {
                DeleteButton_Click(sender, e, creationStack, activeBox);
            };

            Grid.SetColumn(deleteButton, 2);
            Grid.SetRow(deleteButton, totalScheduleRows);
            scheduleGrid.Children.Add(deleteButton);

            // increase total rows
            totalScheduleRows++;
        }

        private void ActiveBox_Checked(object sender, RoutedEventArgs e, StackPanel creationStack)
        {
            if (creationStack.Children.Count <= 0) // if theres not enough stuff
            {
                return;
            }

            if (!((bool)(sender as CheckBox).IsChecked)) // if checkbox gets unchecked
            {
                //string selection = (ComboBox)creationStack.Children[];

            }

            foreach (var child in creationStack.Children)
            {
                if (child is ComboBox)
                {
                    ComboBox comboBox = (ComboBox)child;
                    if ((comboBox.SelectedValue == null) || (comboBox.SelectedItem == ""))
                    {
                        (sender as CheckBox).IsChecked = false;
                        return;
                    }

                } else if (child is TextBox)
                {
                    TextBox textBox = (child as TextBox);

                    if ((textBox.Text == null) || (textBox.Text == ""))
                    {
                        (sender as CheckBox).IsChecked = false;
                        return;
                    }
                }
            }

            string currentMode = ((ComboBox)creationStack.Children[1]).Text;

            // if recurring
            if (currentMode.ToLower() == "recurring")
            {
                string albumName = ((ComboBox)creationStack.Children[3]).Text;
                string timeAsString = ((TextBox)creationStack.Children[5]).Text.ToString();
                string timeScaleString = ((ComboBox)creationStack.Children[6]).Text;
                string functionString = ((ComboBox)creationStack.Children[8]).Text;


                if (double.TryParse(timeAsString, out double time))
                {

                    if (timeScaleString.ToLower() == "seconds")
                    {
                        time /= 60.0;
                    }
                    else if (timeScaleString.ToLower() == "hours")
                    {
                        time *= 60.0;
                    }

                    if (time > 0)
                    {
                        DispatcherTimer recurring_timer = new();
                        recurring_timer.Tick += delegate (object sender, EventArgs e)
                        {
                            recurring_timer_Tick(sender, e, functionString, albumName);
                        };

                        StartRecurringTimer(recurring_timer, time);
                    }
                    else
                    {
                        (sender as CheckBox).IsChecked = false;
                    }
                }
                else
                {
                    (sender as CheckBox).IsChecked = false;
                    return;
                }
            }

            // elif set time

            else if (currentMode.ToLower() == "set time")
            {
                Timer _timer = new();

                Trace.WriteLine("SET TIME");

                string albumName = ((ComboBox)creationStack.Children[3]).Text;
                DateTimeUpDown time = (DateTimeUpDown)creationStack.Children[5];
                string functionString = ((ComboBox)creationStack.Children[7]).Text;


                _timer.Elapsed += delegate (object sender, ElapsedEventArgs e)                    
                {
                    _timer_Elapsed(sender, e, functionString, albumName, (DateTime)time.Value);
                };
                StartTimer(_timer, (DateTime)time.Value);                       
            }
        }

        private void _timer_Elapsed(object? sender, ElapsedEventArgs e, string functionToPerform, string albumName, DateTime time)
        {
            // do some wallpaper stuff

            if (functionToPerform.ToLower() == "random")
            {
                if (!(albumName == "All"))
                {
                    Trace.WriteLine(albumName);
                    RandomSetWP(GetImagesFromAlbum(albumName));
                }

                Trace.WriteLine("all"+albumName);
                RandomSetWP(GetImagesViaPath($"{Directory.GetCurrentDirectory()}\\wallpapers"));

            }
            else if (functionToPerform.ToLower() == "next")
            {
                if (!(albumName == "All"))
                {
                    Trace.WriteLine(albumName);
                    NextSetWP(GetImagesFromAlbum(albumName));

                }
                Trace.WriteLine("all" + albumName);
                NextSetWP(GetImagesViaPath($"{Directory.GetCurrentDirectory()}\\wallpapers"));

            }
            else if (functionToPerform.ToLower() == "previous")
            {
                if (!(albumName == "All"))
                {
                    Trace.WriteLine(albumName);
                    PreviousSetWP(GetImagesFromAlbum(albumName));
                }

                Trace.WriteLine("all" + albumName);
                PreviousSetWP(GetImagesViaPath($"{Directory.GetCurrentDirectory()}\\wallpapers"));
            }

            (sender as Timer).Stop(); // restart timer with time until next time :33
            DateTime newTime = new();
            newTime.AddDays(1); // reset timer for a day!
            StartTimer((sender as Timer), time);
        }

        private void recurring_timer_Tick(object? sender, EventArgs e, string functionToPerform, string albumName)
        {
            // do wallpaper changing stuff in here! this is the timer :D
            
            if (functionToPerform.ToLower() == "random")
            {
                if (!(albumName == "All"))
                {
                    RandomSetWP(GetImagesFromAlbum(albumName));
                }

                RandomSetWP(GetImagesViaPath($"{Directory.GetCurrentDirectory()}\\wallpapers"));
            
            }
            else if (functionToPerform.ToLower() == "next")
            {
                if (!(albumName == "All"))
                {
                    NextSetWP(GetImagesFromAlbum(albumName));
                }
                NextSetWP(GetImagesViaPath($"{Directory.GetCurrentDirectory()}\\wallpapers"));

            }
            else if (functionToPerform.ToLower() == "previous")
            {
                if (!(albumName == "All"))
                {
                    PreviousSetWP(GetImagesFromAlbum(albumName));
                }

                PreviousSetWP(GetImagesViaPath($"{Directory.GetCurrentDirectory()}\\wallpapers"));

            }
        }

        private void StartTimer(Timer _timer, DateTime setTime)
        {
            Trace.WriteLine("set time:"+setTime + "\nnow time:" + DateTime.Now);

            TimeSpan timeDiff = setTime - DateTime.Now;

            Trace.WriteLine("timeDiff:" + timeDiff);

            _timer.Interval = timeDiff.TotalMilliseconds;
            _timer.Start();
        }

        private void StartRecurringTimer(DispatcherTimer _timer, double intervalInMinutes)
        {
            _timer.Interval = TimeSpan.FromMinutes(intervalInMinutes); // 
            _timer.Start();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e, StackPanel creationStack, CheckBox checkBox)
        {
            // delete from json file

            // remove creationStack, checkbox and delete button
            Grid parentGrid = (Grid)creationStack.Parent;
            Button button = sender as Button;

            parentGrid.Children.Remove(creationStack);
            parentGrid.Children.Remove(button);
            parentGrid.Children.Remove(checkBox);

            totalScheduleRows--;
        }

        private void ResetCreationStack(StackPanel creationStack, string selection)
        {


            if (selection == "Set Time")
            {
                creationStack.Children.RemoveRange(2, creationStack.Children.Count);
                scheduleCreateSetTime(creationStack);
            }
            else if (selection == "Recurring")
            {
                creationStack.Children.RemoveRange(2, creationStack.Children.Count);
                scheduleCreateRecurring(creationStack);
            }
            else
            {
                // this never runs
                System.Windows.MessageBox.Show(selection);
            }

        }

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e, StackPanel creationStack)
        {
            string selection = ((ComboBox)sender).SelectedItem.ToString();
            ResetCreationStack(creationStack, selection);
        }
    }
}
