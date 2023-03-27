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
// new WPButton class
public class WPButton : Button
{
    public string? Path;
    public ContextMenu WPButtonContextMenu;
    public CheckBox multiselectCheckBox;
    public bool selected;
    public StackPanel buttonStack;
    
    public WPButton(int Width, int Height, string imagePath)
    {
        // set some stuff
        this.Width = Width; this.Height = Height;
        this.WPButtonContextMenu = new ContextMenu();
        this.selected = false;
        this.buttonStack = new StackPanel();

        this.multiselectCheckBox = new CheckBox()
        {
            RenderSize = new System.Windows.Size(Width / 5, Width / 5),
            VerticalContentAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
        };

        this.AddChild(buttonStack);
        this.CreateImage(imagePath);
        this.DisableCheckBox();
    }

    public void EnableCheckBox()
    {
        this.buttonStack.Children.Remove(this.multiselectCheckBox);
        this.buttonStack.Children.Insert(0, this.multiselectCheckBox);
    }

    public void DisableCheckBox()
    {
        this.buttonStack.Children.Remove(this.multiselectCheckBox);
    }

    public void CreateImage(string path)
    {
        Path = path;

        // create images at runtime https://codedocu.com/Net-Framework/WPF/Images/WPF_colon_-Create-image-at-runtime?1808
        
        Image newImage = new();
        //BitmapImage srcImage = new BitmapImage();
        //FileStream fs = new FileStream(path, FileMode.Open);
        BitmapImage srcImage = new();

        srcImage.BeginInit();
        srcImage.UriSource = new Uri(path, UriKind.Absolute);
        srcImage.CacheOption = BitmapCacheOption.OnLoad; // this fixed the caching problem where images couldn't be deleted because they weren't cached by the program, now they are so the image file itself doesn't need to be open
        srcImage.EndInit();

        newImage.Source = srcImage;
        newImage.Stretch = Stretch.Uniform;

        this.buttonStack.Children.Add(newImage);
    }
}
