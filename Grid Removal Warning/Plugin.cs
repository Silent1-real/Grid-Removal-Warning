using NLog;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Commands;
using Torch.Views;


namespace Grid_Removal_Warning
{
    public class Plugin : TorchPluginBase, IWpfPlugin
    {
        // Configuration for the plugin
        private VeryPersistent<Config> _config;
        // Public property to access the configuration data
        public Config Config => _config?.Data;
        // Scanner for detecting grids in the game world
        private GridScanner scanner;
        // Validator for checking the properties of detected grids
        private GridValidator validator;

        private BlockDefinitionResolver resolver;

        private static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();
        // Initialize the plugin and set up event handlers
        private DateTime nextScanTime;

        // Cooldown tracking - shared by automatic scans and every !grw command
        private DateTime lastScanTime = DateTime.MinValue;
        private List<GridValidationResult> lastWarnings = new List<GridValidationResult>();

        private void LoadConfig()
        {
            var configFile = Path.Combine(StoragePath, "GridRemovalWarning.cfg");

            _config = VeryPersistent<Config>.Load(
                configFile,
                true,
                Config.CreateDefault
            );

            Log.Info("Configuration loaded.");
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            LoadConfig();

            scanner = new GridScanner(Config);

            resolver = new BlockDefinitionResolver();

            nextScanTime = DateTime.Now.AddMinutes(Config.ScanIntervalMinutes);

            base.Torch.GameStateChanged += OnGameStateChanged;
        }

        public UserControl GetControl()
        {
            return new PropertyGrid
            {
                DataContext = Config
            };
        }

        // Event handler for when the game state changes
        private void OnGameStateChanged(
            MySandboxGame game,
            TorchGameState newstate)
        {
            if (newstate != TorchGameState.Loaded)
                return;

            var requiredBlockDefinitions =
                resolver.ResolveRequiredBlocks(Config.RequiredBlocks);

            validator = new GridValidator(
                Config,
                requiredBlockDefinitions
            );

            nextScanTime = DateTime.Now.AddSeconds(30);

            Log.Info("Game loaded. First scan scheduled.");
        }

        public List<GridValidationResult> RunScan()
        {
            if (DateTime.Now < lastScanTime.AddMinutes(Config.ScanCooldownMinutes))
            {
                Log.Info("Scan skipped - cooldown active, returning last known results.");
                return lastWarnings;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Info("Starting grid scan beeboo beeb ...");

            var grids = scanner.Scan();

            Log.Info($"Scanned {grids.Count} valid grids.");

            List<GridValidationResult> warnings = new List<GridValidationResult>();

            foreach (var grid in grids)
            {
                var result = validator.ValidateGrid(grid);

                if (result.HasProblems)
                {
                    warnings.Add(result);
                }
            }
            stopwatch.Stop();

            Log.Info($"Grid scan completed in {stopwatch.ElapsedMilliseconds} ms." +
                $"Grids scanned: {grids.Count}." +
                $"Grids with warnings : {warnings.Count}."
                );

            lastScanTime = DateTime.Now;
            lastWarnings = warnings;

            return warnings;
        }

        public void Save()
        {
            _config.Save();
        }

        public override void Update()
        {
            // Call the base Update method to ensure proper functionality
            base.Update();
            if (!Config.EnableAutomaticScan)
                return;

            if (validator == null)
                return;

            if (DateTime.Now < nextScanTime)
                return;
            
            var commandManager = Torch.CurrentSession?.Managers?.GetManager<CommandManager>();
            commandManager?.HandleCommandFromServer("!grw warn");

            nextScanTime = DateTime.Now.AddMinutes(Config.ScanIntervalMinutes);
        }
        // Clean up event handlers when the plugin is disposed
        public override void Dispose()
        {
            base.Torch.GameStateChanged -= OnGameStateChanged;

            base.Dispose();
        }
    }
}