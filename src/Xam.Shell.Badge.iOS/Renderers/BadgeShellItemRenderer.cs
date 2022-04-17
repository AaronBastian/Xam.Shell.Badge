using AsyncAwaitBestPractices;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace Xam.Shell.Badge.iOS.Renderers
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class BadgeShellItemRenderer : ShellItemRenderer
    {
        private readonly string[] _applyPropertyNames =
            new string[]
            {
                Badge.TextProperty.PropertyName,
                Badge.TextColorProperty.PropertyName,
                Badge.BackgroundColorProperty.PropertyName
            };

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public BadgeShellItemRenderer(IShellContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Occurs when the view has appeared.
        /// </summary>
        /// <param name="animated"></param>
        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            /**
             * This looks bad but it is necessary to force the execution to the
             * right thread. The value of 1 is here because we have to have SOME
             * value for the delay. If we make this too big (e.g. 500ms) then
             * the renderer doesn't think it has a color and applies a default
             * instead of picking up the specified color.
             */
            await Task.Delay(1);
            Device.InvokeOnMainThreadAsync(this.InitBadges).SafeFireAndForget();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void OnShellSectionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnShellSectionPropertyChanged(sender, e);

            if (_applyPropertyNames.All(x => x != e.PropertyName))
            {
                return;
            }

            Device
                .InvokeOnMainThreadAsync(() => this.UpdateBadge((ShellSection)sender))
                .SafeFireAndForget();
        }

        private void InitBadges()
        {
            for (var index = 0; index < this.ShellItem.Items.Count; index++)
            {
                this.UpdateBadge(this.ShellItem.Items.ElementAtOrDefault(index));
            }
        }

        private void UpdateBadge(ShellSection item)
        {
            int index = this.ShellItem.Items.IndexOf(item);
            string text = Badge.GetText(item);
            var textColor = Badge.GetTextColor(item);
            var bg = Badge.GetBackgroundColor(item);
            this.ApplyBadge(index, text, bg, textColor);
        }

        private void ApplyBadge(int index, string text, Color bg, Color textColor)
        {
            if (this.TabBar.Items.Any())
            {
                int.TryParse(text, out int badgeValue);

                if (!string.IsNullOrEmpty(text))
                {
                    if (badgeValue == 0)
                    {
                        this.TabBar.Items[index].BadgeValue = string.Empty; // the dot character caused heartache
                        this.TabBar.Items[index].BadgeColor = UIColor.Clear;
                    }
                    else
                    {
                        this.TabBar.Items[index].BadgeValue = text;
                        this.TabBar.Items[index].BadgeColor = bg.ToUIColor();
                    }

                    this.SetTabBarAppearance(bg.ToUIColor());
                }
                else
                {
                    this.TabBar.Items[index].BadgeValue = default;
                    this.TabBar.Items[index].BadgeColor = UIColor.Clear;
                }
            }
        }

        private void SetTabBarAppearance(UIColor color)
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                var tabBarAppearance = this.TabBar.StandardAppearance.Copy() as UITabBarAppearance;

                SetTabBarItemBadgeAppearance(tabBarAppearance.StackedLayoutAppearance, color);
                SetTabBarItemBadgeAppearance(tabBarAppearance.InlineLayoutAppearance, color);
                SetTabBarItemBadgeAppearance(tabBarAppearance.CompactInlineLayoutAppearance, color);

                this.TabBar.StandardAppearance = tabBarAppearance;

                if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
                {
                    this.TabBar.ScrollEdgeAppearance = tabBarAppearance;
                }
            }
        }

        private static void SetTabBarItemBadgeAppearance(UITabBarItemAppearance appearance, UIColor color)
            => appearance.Normal.BadgeBackgroundColor = color;
    }
}