using ShopPro.Data;
using System.Windows;

namespace ShopPro.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new ShopDbContext("");
            DbInitializer.Initialize(db);
        }
    }
}
