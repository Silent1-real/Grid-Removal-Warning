using System.Collections.Generic;
using Torch;
using Torch.Views;

namespace Grid_Removal_Warning
{
    public class Config : ViewModel
    {
        public static Config CreateDefault()
        {
            Config cfg = new Config();

            cfg.RequiredBlocks = new List<string>()
    {
        "Beacon"
    };

            cfg.GenericGridNames = new List<string>()
    {
        "Large Grid",
        "Small Grid",
        "Static Grid"
    };

            return cfg;
        }
        private bool _enable = true;

        [Display(
            Order = 1,
            Name = "Enable Plugin",
            Description = "Enable or disable Grid Removal Warning.")]
        public bool Enable
        {
            get => _enable;
            set
            {
                _enable = value;
                OnPropertyChanged();
            }
        }
        private int _minimumBlocks = 50;

        [Display(
            Order = 2,
            Name = "Minimum Grid Size",
            Description = "Ignore grids smaller than this.")]
        public int MinimumBlocks
        {
            get => _minimumBlocks;
            set
            {
                _minimumBlocks = value;
                OnPropertyChanged();
            }
        }
        private int _scanIntervalMinutes = 30;

        [Display(
            Order = 3,
            Name = "Scan Interval (Minutes)",
            Description = "Minutes between automatic scans.")]
        public int ScanIntervalMinutes
        {
            get => _scanIntervalMinutes;
            set
            {
                _scanIntervalMinutes = value;
                OnPropertyChanged();
            }
        }
        private bool _enableBlockCheck = true;

        [Display(
            Order = 4,
            Name = "Enable Required Block Check",
            Description = "Warn if required blocks are missing.")]
        public bool EnableBlockCheck
        {
            get => _enableBlockCheck;
            set
            {
                _enableBlockCheck = value;
                OnPropertyChanged();
            }
        }
        private bool _enableNameCheck = true;

        [Display(
            Order = 5,
            Name = "Enable Generic Name Check",
            Description = "Warn if grids still use default names.")]
        public bool EnableNameCheck
        {
            get => _enableNameCheck;
            set
            {
                _enableNameCheck = value;
                OnPropertyChanged();
            }
        }
        private List<string> _requiredBlocks;

        [Display(
            Order = 6,
            Name = "Required Blocks",
            Description = "Blocks every player grid must contain.")]
        public List<string> RequiredBlocks
        {
            get => _requiredBlocks;
            set => SetValue(ref _requiredBlocks, value);
        }
        private List<string> _genericGridNames;

        [Display(
            Order = 7,
            Name = "Generic Grid Names",
            Description = "Names considered default and requiring renaming.")]
        public List<string> GenericGridNames
        {
            get => _genericGridNames;
            set => SetValue(ref _genericGridNames, value);
        }
        private bool _enableAutomaticScan = true;

        [Display(
            Order = 8,
            Name = "Enable Automatic Scanning",
            Description = "If disabled, the plugin will not perform scheduled scans. Administrators can still use !grw scan and !grw warn manually.")]
        public bool EnableAutomaticScan
        {
            get => _enableAutomaticScan;
            set
            {
                _enableAutomaticScan = value;
                OnPropertyChanged();
            }
        }
    }
}

