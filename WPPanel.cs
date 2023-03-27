//using System.Drawing.Drawing2D;
using System.Collections.Generic;
//using System.Text;
using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
public class WPPanel : WrapPanel
{
    public List<string> GetChildNames()
    {
        List<string> childNames = new();

        foreach (object child in Children)
        {
            if (child is WPButton)
            {
                childNames.Add(item: (child as WPButton).Path.ToString());
            }
        }

        return childNames;
    }

}
