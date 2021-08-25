#if __IOS__ && !MAUI
using System;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(ContentPage), typeof(MSTestX.Renderers.PageRenderer))]

namespace MSTestX.Renderers
{
    /// <summary>
    /// Overrides the default iOS Page Renderer to add support for themes
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class PageRenderer : Xamarin.Forms.Platform.iOS.PageRenderer
    {
        /// <inheritdoc />
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            try
            {
                SetAppTheme();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"\t\t\tERROR: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);
            Console.WriteLine($"TraitCollectionDidChange: {TraitCollection.UserInterfaceStyle} != {previousTraitCollection?.UserInterfaceStyle}");

            if (this.TraitCollection.UserInterfaceStyle != previousTraitCollection?.UserInterfaceStyle)
            {
                SetAppTheme();
            }


        }

        void SetAppTheme()
        {

            if (this.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark)
            {
                if (RunnerApp.AppTheme == "dark")
                    return;
                //Add a Check for App Theme since this is called even when not changed really
                RunnerApp.Current.Resources = new Styles.DarkTheme();

                RunnerApp.AppTheme = "dark";
            }
            else
            {
                if (RunnerApp.AppTheme != "dark")
                    return;
                RunnerApp.Current.Resources = new Styles.DefaultTheme();
                RunnerApp.AppTheme = "light";
            }
        }
    }
}
#endif
