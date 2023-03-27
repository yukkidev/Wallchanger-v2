using System;
//using System.Drawing.Drawing2D;
//using System.Text;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
public class AlbumButton : Button
{
    public ContextMenu AlbumButtonContextMenu;
    public StackPanel stack = new();

    public AlbumButton(int Width, int Height, string albumName)
    {
        // set some stuff
        this.Width = Width; this.Height = Height;
        this.AlbumButtonContextMenu = new();

        stack.Orientation = Orientation.Vertical;
        stack.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(new TextBlock {Text=albumName, HorizontalAlignment=HorizontalAlignment.Center});

        this.AddChild(stack);
    }

    public void CreateImage(string path)
    {
        // create images at runtime https://codedocu.com/Net-Framework/WPF/Images/WPF_colon_-Create-image-at-runtime?1808

        try
        {
            Image newImage = new();
            BitmapImage srcImage = new();

            srcImage.BeginInit();
            srcImage.UriSource = new Uri(path, UriKind.Absolute);
            srcImage.CacheOption = BitmapCacheOption.OnLoad; // this fixed the caching problem where images couldn't be deleted because they weren't cached by the program, now they are so the image file itself doesn't need to be open
            srcImage.EndInit();

            newImage.Source = srcImage;
            newImage.Stretch = Stretch.UniformToFill;

            this.stack.Children.Add(newImage);
        }
        catch (Exception)
        {
        }


    }
}
