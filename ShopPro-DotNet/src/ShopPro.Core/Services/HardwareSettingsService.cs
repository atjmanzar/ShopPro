using ShopPro.Data;
using ShopPro.Data.Entities;
using ShopPro.Hardware;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class HardwareConfig
    {
        public string ThermalPrinterName { get; set; } = "Generic / Text Only";
        public PaperWidth PaperWidth { get; set; } = PaperWidth.mm80;
        public bool AutoKickCashDrawer { get; set; } = true;
        public string VfdComPort { get; set; } = "COM1";
        public string ScaleComPort { get; set; } = "COM2";
        public string Gstin { get; set; } = "27AAAAA0000A1Z5";
        public string FooterMessage { get; set; } = "Thank you for shopping with ShopPro!";
    }

    public class HardwareSettingsService
    {
        private readonly ShopDbContext _db;

        public HardwareSettingsService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<HardwareConfig> GetHardwareConfigAsync()
        {
            var settings = await _db.HardwareSettings.ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

            var config = new HardwareConfig();
            if (settings.TryGetValue("ThermalPrinterName", out var pName)) config.ThermalPrinterName = pName;
            if (settings.TryGetValue("PaperWidth", out var pWidth)) config.PaperWidth = pWidth == "58mm" ? PaperWidth.mm58 : PaperWidth.mm80;
            if (settings.TryGetValue("AutoKickCashDrawer", out var aKick)) config.AutoKickCashDrawer = bool.Parse(aKick);
            if (settings.TryGetValue("VfdComPort", out var vfd)) config.VfdComPort = vfd;
            if (settings.TryGetValue("ScaleComPort", out var scale)) config.ScaleComPort = scale;
            if (settings.TryGetValue("Gstin", out var gstin)) config.Gstin = gstin;
            if (settings.TryGetValue("FooterMessage", out var footer)) config.FooterMessage = footer;

            return config;
        }

        public async Task SaveHardwareConfigAsync(HardwareConfig config)
        {
            await SaveSettingInternal("ThermalPrinterName", config.ThermalPrinterName);
            await SaveSettingInternal("PaperWidth", config.PaperWidth == PaperWidth.mm58 ? "58mm" : "80mm");
            await SaveSettingInternal("AutoKickCashDrawer", config.AutoKickCashDrawer.ToString());
            await SaveSettingInternal("VfdComPort", config.VfdComPort);
            await SaveSettingInternal("ScaleComPort", config.ScaleComPort);
            await SaveSettingInternal("Gstin", config.Gstin);
            await SaveSettingInternal("FooterMessage", config.FooterMessage);

            await _db.SaveChangesAsync();
        }

        private async Task SaveSettingInternal(string key, string val)
        {
            var setting = await _db.HardwareSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
            if (setting == null)
            {
                _db.HardwareSettings.Add(new HardwareSetting { SettingKey = key, SettingValue = val, UpdatedAt = DateTime.UtcNow });
            }
            else
            {
                setting.SettingValue = val;
                setting.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
