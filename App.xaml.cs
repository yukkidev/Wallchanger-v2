using System;
using System.Windows;
using System.Collections.Generic;
using Forms = System.Windows.Forms;
using System.Linq;
using System.IO;

namespace Wallchanger_v2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly Forms.NotifyIcon _notifyIcon;
        private int currentIndex = 0;
        private Forms.ToolStripButton selectedButton;
        private bool activated = false;
        private List<string>? images;
        private Forms.ToolStripDropDownButton dropDownMenu;

        public App()
        {
             _notifyIcon = new();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // set _notifyIcon
            _notifyIcon.Icon = new System.Drawing.Icon("icon.ico");
            _notifyIcon.Text = "WallChanger";
            _notifyIcon.MouseDown += _notifyIcon_MouseDown;
            _notifyIcon.Visible = true;

            _notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();

            _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripButton("     Open     ", null, OnOpenClicked));
            _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());

            dropDownMenu = new Forms.ToolStripDropDownButton();

            _notifyIcon.ContextMenuStrip.Items.Add(dropDownMenu);
            
            _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripButton("     Next     ", null, OnNextClicked));
            _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripButton("     Previous     ", null, OnPreviousClicked));
            _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripButton("     Randomize     ", null, OnRandomizeClicked));
            //_notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripButton("     Options     ", null, OnOptionsClicked));

            _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());
            _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripButton("     Exit     ", null, OnExitClicked));

            base.OnStartup(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            MainWindow wnd = (MainWindow)Application.Current.MainWindow;
            Dictionary<string, List<string>> albumsJson = wnd.GetJsonFile("albums.json");

            if (!activated)
            {
                selectedButton = new Forms.ToolStripButton("All", null, OnAlbumButtonClicked);

                dropDownMenu.DropDownItems.Add(selectedButton);

                dropDownMenu.Text = "Album: " + selectedButton.Text;

                foreach (string album in albumsJson.Keys.ToList())
                {
                    dropDownMenu.DropDownItems.Add(new Forms.ToolStripButton(album, null, OnAlbumButtonClicked));
                }

                activated = true;
            }
            
            if (selectedButton.ToString() == "All")
            {
                images = wnd.GetImagesViaPath(($"{Directory.GetCurrentDirectory()}\\wallpapers"));
            }
            else
            {
                images = albumsJson[selectedButton.ToString()];
            }
            
            base.OnActivated(e);
        }

        private void OnAlbumButtonClicked(object sender, EventArgs e)
        {
            currentIndex = 0;
            selectedButton = sender as Forms.ToolStripButton;

            MessageBox.Show(selectedButton.Text);
                
            dropDownMenu.Text = "Album: " + selectedButton.Text;
        }

        private void OnOpenClicked(object sender, EventArgs e)
        {
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            //MainWindow.Activate();
        }

        private void OnNextClicked(object sender, EventArgs e)
        {
            MainWindow wnd = (MainWindow)Application.Current.MainWindow;

            currentIndex = Math.Max(0, Math.Min(currentIndex + 1, images.Count-1));

            if (images.Count > 0)
            {
                wnd.SetWP(images[currentIndex]);
            }
        }

        private void OnPreviousClicked(object sender, EventArgs e)
        {
            MainWindow wnd = (MainWindow)Application.Current.MainWindow;

            currentIndex = Math.Min(images.Count-1, Math.Max(0, currentIndex - 1));

            if (images.Count > 0)
            {
                wnd.SetWP(images[currentIndex]);
            }
        }
        private void OnRandomizeClicked(object sender, EventArgs e)
        {
            MainWindow wnd = (MainWindow)Application.Current.MainWindow;

            currentIndex = wnd.RandomSetWP(images);
            
        }
        private void OnOptionsClicked(object sender, EventArgs e)
        {
            // implement when options exists
        }
        private void OnExitClicked(object sender, EventArgs e)
        {
            MainWindow wnd = (MainWindow)Application.Current.MainWindow;
            wnd.CloseWindow();
        }

        private void _notifyIcon_MouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
             
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                //MainWindow.Activate();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon.Dispose();
            base.OnExit(e);
        }

    }
}
