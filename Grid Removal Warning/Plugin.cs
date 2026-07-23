using NLog;
using Sandbox;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Views;
using VRageMath;

namespace Grid_Removal_Warning
{
    public class Plugin : TorchPluginBase, IWpfPlugin
    {
        // Configuration for the plugin
        private VeryPersistent<Config> _config;
        public Config Config => _config?.Data;

        // Scanner for detecting grids in the game world
        private GridScanner scanner;
        // Validator for checking the properties of detected grids
        private GridValidator validator;

        private BlockDefinitionResolver resolver;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private DateTime nextScanTime;

        // Requesters waiting on the current (or next) cycle to complete.
        // Steam IDs are collected purely for where to send the reply -
        // it does not matter who asked or how many times.
        private List<ulong> pendingScanRequesters = new List<ulong>();
        private List<ulong> pendingWarnRequesters = new List<ulong>();

        // Hardcoded per-player cooldown for !grw check - not worth exposing in config.
        private static readonly TimeSpan CheckCooldown = TimeSpan.FromSeconds(30);
        private readonly Dictionary<long, DateTime> lastCheckTimes = new Dictionary<long, DateTime>();

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

        private void OnGameStateChanged(MySandboxGame game, TorchGameState newstate)
        {
            if (newstate != TorchGameState.Loaded)
                return;

            var requiredBlockDefinitions = resolver.ResolveRequiredBlocks(Config.RequiredBlocks);

            validator = new GridValidator(Config, requiredBlockDefinitions);

            nextScanTime = DateTime.Now.AddSeconds(30);

            Log.Info("Game loaded. First scan scheduled.");
        }

        // ---------------- Entry points for commands ----------------

        // Returns the immediate chat reply for the admin who ran !grw scan.
        public string RequestScan(ulong requesterSteamId)
        {
            pendingScanRequesters.Add(requesterSteamId);

            if (scanner.IsScanning)
            {
                return "A scan is already in progress. You'll receive the report when it finishes.";
            }

            scanner.StartScan();
            return "Scan started. You'll receive the report when it finishes.";
        }

        // Returns the immediate chat reply for the admin who ran !grw warn.
        public string RequestWarn(ulong requesterSteamId)
        {
            pendingWarnRequesters.Add(requesterSteamId);

            if (scanner.IsScanning)
            {
                return "A scan is already in progress. Warnings will be sent once it finishes.";
            }

            scanner.StartScan();
            return "Scan started. Warnings will be sent once it finishes.";
        }

        // Instant, single-player scan for !grw check. Not part of the batched cycle.
        // Returns false (with a cooldown message) if the player checked too recently.
        public bool TryCheckPlayer(long identityId, out List<GridValidationResult> results, out string cooldownMessage)
        {
            if (lastCheckTimes.TryGetValue(identityId, out var lastCheck) &&
                DateTime.Now < lastCheck + CheckCooldown)
            {
                var remaining = (lastCheck + CheckCooldown) - DateTime.Now;
                results = null;
                cooldownMessage = $"Please wait {Math.Ceiling(remaining.TotalSeconds)}s before checking again.";
                return false;
            }

            lastCheckTimes[identityId] = DateTime.Now;

            var grids = scanner.ScanSinglePlayer(identityId);

            results = new List<GridValidationResult>();

            foreach (var grid in grids)
            {
                var result = validator.ValidateGrid(grid);

                if (result.HasProblems)
                {
                    results.Add(result);
                }
            }

            cooldownMessage = null;
            return true;
        }

        // ---------------- Main update loop ----------------

        public override void Update()
        {
            base.Update();

            if (validator == null)
                return;

            // A cycle already in flight (started automatically or by a command) -
            // keep stepping it every tick regardless of what triggered it.
            if (scanner.IsScanning)
            {
                bool finished = scanner.StepScan();

                if (finished)
                {
                    OnCycleComplete();
                    nextScanTime = DateTime.Now.AddMinutes(Config.ScanIntervalMinutes);
                }

                return;
            }

            // No cycle running - only auto-start one on the schedule if enabled.
            if (!Config.EnableAutomaticScan)
                return;

            if (DateTime.Now < nextScanTime)
                return;

            Log.Info("Starting scheduled grid scan...");
            scanner.StartScan();
        }

        private void OnCycleComplete()
        {
            var grids = scanner.GetResults();

            var warnings = new List<GridValidationResult>();

            foreach (var grid in grids)
            {
                var result = validator.ValidateGrid(grid);

                if (result.HasProblems)
                {
                    warnings.Add(result);
                }
            }

            Log.Info($"Validation complete. Grids with warnings: {warnings.Count}.");

            if (pendingWarnRequesters.Count > 0)
            {
                NotifyAffectedPlayers(warnings);
                SendScanSummaryTo(pendingWarnRequesters, warnings, "Warnings sent to affected players.");
            }

            if (pendingScanRequesters.Count > 0)
            {
                SendFullReportTo(pendingScanRequesters, warnings);
            }

            pendingWarnRequesters.Clear();
            pendingScanRequesters.Clear();
        }

        // ---------------- Single-player check (called directly by the command, not batched) ----------------

        public void SendCheckResultTo(ulong steamId, List<GridValidationResult> warnings)
        {
            var chat = Torch.CurrentSession?.Managers?.GetManager<IChatManagerServer>();
            if (chat == null)
                return;

            string message;

            if (warnings.Count == 0)
            {
                message = "None of your grids require attention.";
            }
            else
            {
                message = $"Found {warnings.Count} of your grids requiring attention.\n\n";

                foreach (var warning in warnings)
                {
                    message += $"{warning.Grid.Name} | Blocks: {warning.Grid.BlockCount}\n";

                    foreach (var problem in warning.Problems)
                    {
                        message += $"  - {problem}\n";
                    }

                    message += "\n";
                }
            }

            chat.SendMessageAsOther("Grid Removal Warning", message, Color.Yellow, steamId);
        }

        public void SendCooldownMessageTo(ulong steamId, string cooldownMessage)
        {
            var chat = Torch.CurrentSession?.Managers?.GetManager<IChatManagerServer>();
            if (chat == null)
                return;

            chat.SendMessageAsOther("Grid Removal Warning", cooldownMessage, Color.Yellow, steamId);
        }

        // ---------------- Chat dispatch ----------------
        // Send messages to affected players and admins after a scan cycle completes.
        private void NotifyAffectedPlayers(List<GridValidationResult> warnings)
        {
            var chat = Torch.CurrentSession?.Managers?.GetManager<IChatManagerServer>();
            if (chat == null)
                return;
            // Group warnings by player and send a message to each affected player.
            foreach (var playerWarnings in warnings.GroupBy(w => w.Grid.OwnerId))
            {
                var ownerId = playerWarnings.Key;
                var identity = MySession.Static.Players.TryGetIdentity(ownerId);

                if (identity == null)
                    continue;

                var player = Torch.CurrentSession.KeenSession.Players
                    .GetOnlinePlayers()
                    .FirstOrDefault(p => p.Identity != null && p.Identity.IdentityId == ownerId);

                if (player == null)
                    continue;

                string message = "The following grids require attention:\n\n";

                foreach (var warning in playerWarnings)
                {
                    message += $"{warning.Grid.Name}\n";

                    foreach (var problem in warning.Problems)
                    {
                        message += $"  \u2022 {problem}\n";
                    }

                    message += "\n";
                }

                chat.SendMessageAsOther("Grid Removal Warning", message, Color.Yellow, player.Id.SteamId);
            }
        }
        // Send a summary of the scan results to the admins who requested it.
        private void SendScanSummaryTo(List<ulong> steamIds, List<GridValidationResult> warnings, string summary)
        {
            var chat = Torch.CurrentSession?.Managers?.GetManager<IChatManagerServer>();
            if (chat == null)
                return;

            string message = warnings.Count == 0
                ? "No grids require warnings."
                : $"Found {warnings.Count} grids requiring attention. {summary}";

            foreach (var steamId in steamIds.Distinct())
            {
                chat.SendMessageAsOther("Grid Removal Warning", message, Color.Yellow, steamId);
            }
        }
        // Send a full report of the scan results to the admins who requested it.
        private void SendFullReportTo(List<ulong> steamIds, List<GridValidationResult> warnings)
        {
            var chat = Torch.CurrentSession?.Managers?.GetManager<IChatManagerServer>();
            if (chat == null)
                return;

            string message;

            if (warnings.Count == 0)
            {
                message = "No grids require attention.";
            }
            else
            {
                message = $"Found {warnings.Count} grids requiring attention.\n\n";

                foreach (var warning in warnings)
                {
                    message += $"{warning.Grid.Name} | Owner: {warning.Grid.OwnerName} | Blocks: {warning.Grid.BlockCount}\n";

                    foreach (var problem in warning.Problems)
                    {
                        message += $"  - {problem}\n";
                    }

                    message += "\n";
                }
            }

            foreach (var steamId in steamIds.Distinct())
            {
                chat.SendMessageAsOther("Grid Removal Warning", message, Color.Yellow, steamId);
            }
        }

        public void Save()
        {
            _config.Save();
        }

        public override void Dispose()
        {
            base.Torch.GameStateChanged -= OnGameStateChanged;
            base.Dispose();
        }
    }
}
