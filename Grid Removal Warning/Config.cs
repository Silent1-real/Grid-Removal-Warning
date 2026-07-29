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

        // ---- General ----
        private bool _enable = true;

        [Display(
            Order = 1,
            GroupName = "General",
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
            GroupName = "General",
            Name = "Minimum Grid Block Count",
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

        // ---- Scanning ----
        private bool _enableAutomaticScan = true;

        [Display(
            Order = 3,
            GroupName = "Scanning",
            Name = "Enable Automatic Scanning",
            Description = "If disabled, the plugin will not perform scheduled scans. Administrators can still use !grw scan and !grw warn manually or Use Essential plugin for scheduled scans before removal.")]
        public bool EnableAutomaticScan
        {
            get => _enableAutomaticScan;
            set
            {
                _enableAutomaticScan = value;
                OnPropertyChanged();
            }
        }
        private int _scanIntervalMinutes = 30;

        [Display(
            Order = 4,
            GroupName = "Scanning",
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

        // ---- Checks ----
        private bool _enableBlockCheck = true;

        [Display(
            Order = 6,
            GroupName = "Checks",
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
        private List<string> _requiredBlocks = new List<string>();

        [Display(
            Order = 7,
            GroupName = "Checks",
            Name = "Required Blocks",
            Description = "Blocks every player grid must contain."
            + "the plugin have its own name resolver but its important to write what comes after MyObjectBuilder_ correctly. "
            + "worth to note that resolver is not case sensetive wether you type beacon or Beacon does not matter.+"
            + "Since plugin use parrent category for search it automaticly includeds any subtype example DLC or potenital modded version")]

        public List<string> RequiredBlocks
        {
            get => _requiredBlocks;
            set => SetValue(ref _requiredBlocks, value);
        }
        private bool _enableNameCheck = true;

        [Display(
            Order = 8,
            GroupName = "Checks",
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
        private List<string> _genericGridNames = new List<string>();

        [Display(
            Order = 9,
            GroupName = "Checks",
            Name = "Generic Grid Names",
            Description = "Names considered default and requiring renaming.")]
        public List<string> GenericGridNames
        {
            get => _genericGridNames;
            set => SetValue(ref _genericGridNames, value);
        }

        private bool _ignoreSubgridsForBlockCheck = true;

        [Display(
            Order = 10,
            GroupName = "Checks",
            Name = "Ignore Subgrids for Required Block Check",
            Description = "If Checked, subgrids attached via rotors/pistons/hinges are excluded from the required block check.")]
        public bool IgnoreSubgridsForBlockCheck
        {
            get => _ignoreSubgridsForBlockCheck;
            set
            {
                _ignoreSubgridsForBlockCheck = value;
                OnPropertyChanged();
            }
        }

        private bool _ignoreSubgridsForNameCheck = true;

        [Display(
            Order = 11,
            GroupName = "Checks",
            Name = "Ignore Subgrids for Generic Name Check",
            Description = "If Checked, subgrids attached via rotors/pistons/hinges are excluded from the generic name check.")]
        public bool IgnoreSubgridsForNameCheck
        {
            get => _ignoreSubgridsForNameCheck;
            set
            {
                _ignoreSubgridsForNameCheck = value;
                OnPropertyChanged();
            }
        }
        private Language _PreferredLanguage = Language.English;
        // Preferred language for messages sent to players and admins
        [Display(
            Order = 12,
            GroupName = "General",
            Name = "Messages Preferred Language",
            Description = "Language used for Messages.")]

        public Language PreferredLanguage
        {
            get => _PreferredLanguage;
            set
            {
                _PreferredLanguage = value;
                OnPropertyChanged();

            }
        }
    }
}