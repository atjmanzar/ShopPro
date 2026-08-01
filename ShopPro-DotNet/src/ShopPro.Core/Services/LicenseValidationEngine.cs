using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class LicenseState
    {
        public bool IsValid { get; set; }
        public bool IsTrial { get; set; }
        public int RemainingTrialDays { get; set; }
        public string MachineFingerprint { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public LicenseType Type { get; set; } = LicenseType.Trial;
        public string Message { get; set; } = string.Empty;
    }

    public class LicenseValidationEngine
    {
        private readonly ShopDbContext _db;
        public const int TrialDurationDays = 14;

        public LicenseValidationEngine(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<LicenseState> ValidateCurrentLicenseAsync()
        {
            var fingerprint = HardwareFingerprintGenerator.GenerateMachineFingerprint();
            var activeLicense = await _db.Licenses.FirstOrDefaultAsync(l => l.IsActivated);

            if (activeLicense != null)
            {
                if (activeLicense.HardwareFingerprint != fingerprint)
                {
                    return new LicenseState
                    {
                        IsValid = false,
                        IsTrial = false,
                        MachineFingerprint = fingerprint,
                        Message = "License Mismatch: This commercial license is locked to a different machine."
                    };
                }

                return new LicenseState
                {
                    IsValid = true,
                    IsTrial = false,
                    MachineFingerprint = fingerprint,
                    CustomerName = activeLicense.RegisteredCustomerName,
                    Type = activeLicense.Type,
                    Message = $"Activated Commercial License ({activeLicense.Type})"
                };
            }

            // Trial Evaluation
            var trial = await _db.Licenses.FirstOrDefaultAsync(l => l.Type == LicenseType.Trial);
            if (trial == null)
            {
                trial = new LicenseInformation
                {
                    LicenseKey = "TRIAL-EVAL-KEY",
                    RegisteredCustomerName = "Evaluation Workstation",
                    HardwareFingerprint = fingerprint,
                    Type = LicenseType.Trial,
                    IsActivated = false,
                    TrialStartDate = DateTime.UtcNow
                };
                _db.Licenses.Add(trial);
                await _db.SaveChangesAsync();
            }

            var daysUsed = (DateTime.UtcNow - trial.TrialStartDate).Days;
            int remainingDays = Math.Max(0, TrialDurationDays - daysUsed);

            bool isTrialValid = remainingDays > 0;
            return new LicenseState
            {
                IsValid = isTrialValid,
                IsTrial = true,
                RemainingTrialDays = remainingDays,
                MachineFingerprint = fingerprint,
                CustomerName = trial.RegisteredCustomerName,
                Type = LicenseType.Trial,
                Message = isTrialValid ? $"14-Day Evaluation Trial Active ({remainingDays} days remaining)" : "Evaluation Trial Expired. Please activate a commercial license."
            };
        }

        public async Task<(bool Success, string Message)> ActivateLicenseKeyAsync(string licenseKey, string customerName)
        {
            if (string.IsNullOrWhiteSpace(licenseKey) || !licenseKey.Trim().StartsWith("PRO-"))
            {
                return (false, "Invalid License Key format. Key must begin with 'PRO-'.");
            }

            var fingerprint = HardwareFingerprintGenerator.GenerateMachineFingerprint();
            var license = new LicenseInformation
            {
                LicenseKey = licenseKey.Trim().ToUpper(),
                RegisteredCustomerName = string.IsNullOrWhiteSpace(customerName) ? "Commercial Customer" : customerName.Trim(),
                HardwareFingerprint = fingerprint,
                Type = LicenseType.CommercialStandard,
                IsActivated = true,
                ActivatedAt = DateTime.UtcNow
            };

            _db.Licenses.Add(license);
            await _db.SaveChangesAsync();

            return (true, "ShopPro Commercial License Activated Successfully!");
        }

        public async Task DeactivateCurrentLicenseAsync()
        {
            var active = await _db.Licenses.Where(l => l.IsActivated).ToListAsync();
            foreach (var l in active)
            {
                l.IsActivated = false;
            }
            await _db.SaveChangesAsync();
        }
    }
}
