using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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

namespace Wallchanger_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // dynamically add all images, and remove noImagesPanel, reset horizontal and vertical alignment of wpContainer
            // testing code!
            wpContainer.Children.Remove(noImagesPanel);

            List<String> images = GetImagesViaPath("C:\\Users\\Deyvohn\\Pictures\\Wallpapers");
            foreach (String img in images)
            {
                Console.WriteLine(img);
                WPButton btn = new WPButton();
                wpContainer.Children.Add(btn.make_Wallpaper_button(img));
            }

        }
        private void optionsButton_Click(object sender, RoutedEventArgs e)
        {
            // open options menu
            optionsButton.Content = "Clicked!";
        }
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            // add images
        }

        private void addToAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            // add selected images to an album
        }

        private void favButton_Click(object sender, RoutedEventArgs e)
        {
            // add selected buttons to favorites album
        }

        private void randButton_Click(object sender, RoutedEventArgs e)
        {
            // randomly set Wp
        }

        private void setButton_Click(object sender, RoutedEventArgs e)
        {
            // set currently selected button as Wp
        }

        private void albumsBox_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // for albums combobox
        }

        private void wpContainer_Initialized(object sender, EventArgs e)
        {
            // add images here
        }

        private void addImagesFromNoImagesPanel_Click(object sender, RoutedEventArgs e)
        {
            // add images, then remove the noImagesPanel
        }

        private List<String> GetImagesViaPath(String folderName)
        {
            // get all images from path
            List<String> images = new List<String>(System.IO.Directory.EnumerateFiles(folderName, "*"));
            Console.WriteLine("Got images from path + " + folderName);
            return images;
        }

        static private void AddAButton(String path)
        {
            // init new WPButton class, add it to WrapPanel
        }

        static private void AddButtons(List<String> images)
        {
            // List<String> in_images_folder = 
        }
    }
}

// new WPButton class
public class WPButton
{
    public Button make_Wallpaper_button(String path)
    {
        BitmapImage imageFile = new BitmapImage();
        imageFile = new BitmapImage(new Uri(path));

        Button picture = new Button
        {
            Name = "wallpaperButton",
            Width = 160,
            Height = 90,
            Content = imageFile
        };

        return picture;
    }
}