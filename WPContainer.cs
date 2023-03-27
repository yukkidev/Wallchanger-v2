//using System.Drawing.Drawing2D;
//using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Windows.Media;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
public class WPContainer : TabItem
{
    // used for creating dynamic tabs when loading new images from different albums

    public WPPanel? WPanel;
    public StackPanel? SPanel;
    public Button? addImagesButton;
    public ContextMenu? tabHeaderContextMenu;
    public ScrollViewer? sViewer;

    public WPContainer(ContentControl cc) // init method
    {
        this.Header = cc.Content;

        this.tabHeaderContextMenu = cc.ContextMenu;

        sViewer = new ScrollViewer();
        this.AddChild(sViewer);

        WPanel = new WPPanel();

        WPanel.VerticalAlignment = VerticalAlignment.Center;
        WPanel.HorizontalAlignment = HorizontalAlignment.Center;

        sViewer.Content = WPanel;

        SPanel = new StackPanel();
        Label label = new (){Content="No images found.", FontSize=18};
        addImagesButton = new Button(){Content = "Add images", FontSize = 18};

        SPanel.Children.Add(label);
        SPanel.Children.Add(addImagesButton);
        WPanel.Children.Add(SPanel);
    }

    public void RemoveButton(string btnPath)
    {
        WPButton? btn = null;

        foreach (object wP in WPanel.Children)
        {
            if (wP is WPButton)
            {
                if ((wP as WPButton).Path == btnPath)
                {
                    btn = wP as WPButton; break;
                }
            }
        }

        if (btn != null)
        {
            this.WPanel.Children.Remove(btn);
        }

        if (WPanel.Children.Count == 1)
        {
            EnableSPanel();
        }
    }

    public List<string> GetImagePaths()
    {
        List<string> images = new();

        foreach (object btn in WPanel.Children)
        {
            if (btn is WPButton)
            {
                images.Add((btn as WPButton).Path);
            }
        }

        return images;
    }

    public void EnableSPanel()
    {
        SPanel.IsEnabled = true;
        SPanel.Visibility = Visibility.Visible;
    }

    public void DisableSPanel()
    {
        SPanel.IsEnabled = false;
        SPanel.Visibility = Visibility.Collapsed;
    }
}