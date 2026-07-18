using NLog;
using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using Torch;
using Torch.API;


namespace Grid_Removal_Warning
{
    public class Plugin : TorchPluginBase
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

        // Event handler for when the game state changes
        private void OnGameStateChanged(
            MySandboxGame game,
            TorchGameState newstate)
        {
            if (newstate != TorchGameState.Loaded)
                return;

            Log.Info("Game loaded. Resolving block definitions...");

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


            RunScan();


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
