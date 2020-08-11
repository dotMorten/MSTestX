using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MSTestX.UnitTestRunner.Views
{
    /// <summary>
    /// Displays the content of a test attachment
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AttachmentPage : ContentPage
    {
        internal AttachmentPage(Microsoft.VisualStudio.TestPlatform.ObjectModel.UriDataAttachment attachment)
        {
            InitializeComponent();
            this.Title = attachment?.Uri.OriginalString.Split('\\', '/').LastOrDefault() ?? "";
            LoadFile(attachment);
        }
        private void LoadFile(Microsoft.VisualStudio.TestPlatform.ObjectModel.UriDataAttachment attachment)
        { 
            if (string.IsNullOrEmpty(attachment.Uri.LocalPath) || !File.Exists(attachment.Uri.LocalPath))
            {
                status.Text = "File not found\n" + attachment.Uri;
            }
            try
            {
                bool isImage = false;
                using (var f = File.OpenRead(attachment.Uri.LocalPath))
                {
                    byte[] data = new byte[8];
                    int count = f.Read(data, 0, 8);
                    if ((count >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4e && data[3] == 0x47 && data[4] == 0x0d && data[5] == 0x0a && data[6] == 0x1a && data[7] == 0x0a) //PNG
                        || (count >= 4 && data[0] == 0xFF && data[1] == 0xD8 && (data[2] == 0xDD && data[3] == 0xE0 || data[2] == 0xFF && data[3] == 0xE1))) //JPEG
                    {
                        isImage = true;
                    }
                }
                if (!isImage)
                {
                    contentViewText.Text = File.ReadAllText(attachment.Uri.LocalPath);
                    contentView.IsVisible = true;
                }
                else
                {
                    imageView.Source = new StreamImageSource()
                    {
                        Stream = (token) =>
                        {
                            return Task.FromResult((Stream)File.OpenRead(attachment.Uri.LocalPath));
                        }
                    };
                    imageView.IsVisible = true;
                }
            }
            catch(System.Exception ex)
            {
                status.Text = "Failed to load file\n" + ex.Message;
            }
        }
    }
}