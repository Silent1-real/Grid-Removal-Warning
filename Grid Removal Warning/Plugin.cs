using NLog;
using Sandbox;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Commands;
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
        // Resolver for mapping block names to their definitions
        private BlockDefinitionResolver resolver;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private DateTime nextScanTime;

        // Requesters waiting on the current (or next) cycle to complete.
        // We hold the actual CommandContext so we can Respond() later, once the
        // cycle finishes - this works the same whether the command came from a
        // player in-game or from the Torch console.
        private List<CommandContext> pendingScanRequesters = new List<CommandContext>();
        private List<CommandContext> pendingWarnRequesters = new List<CommandContext>();

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

        // Returns the immediate reply for whoever ran !grw scan (player or console).
        public string RequestScan(CommandContext context)
        {
            pendingScanRequesters.Add(context);
            var messages = GridMessages.Get(Config);

            if (scanner.IsScanning)
            {
                return messages.ScanAlreadyRunning;
            }

            scanner.StartScan();
            return messages.ScanStarted;
        }

        // Returns the immediate reply for whoever ran !grw warn (player or console).
        public string RequestWarn(CommandContext context)
        {
            pendingWarnRequesters.Add(context);
            var messages = GridMessages.Get(Config);

            if (scanner.IsScanning)
            {
                return messages.WarnAlreadyRunning;
            }

            scanner.StartScan();
            return messages.WarnStarted;
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

                var messages = GridMessages.Get(Config);
                cooldownMessage = string.Format(messages.PleaseWaitBeforeChecking, Math.Ceiling(remaining.TotalSeconds));
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
                var messages = GridMessages.Get(Config);
                NotifyAffectedPlayers(warnings);
                RespondScanSummary(pendingWarnRequesters, warnings, messages.WarningsSentToPlayers);
            }

            if (pendingScanRequesters.Count > 0)
            {
                RespondFullReport(pendingScanRequesters, warnings);
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

            var messages = GridMessages.Get(Config);
            string message;

            if (warnings.Count == 0)
            {
                message = messages.NoneOfYourGridsRequireAttention;
            }
            else
            {
                message = $"{string.Format(messages.FoundYourGridsRequiringAttention, warnings.Count)}\n\n";

                foreach (var warning in warnings)
                {
                    message += $"{warning.Grid.Name} | {messages.BlocksLabel} {warning.Grid.BlockCount}\n";

                    foreach (var problem in warning.Problems)
                        message += $"  - {problem}\n";

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

        private void NotifyAffectedPlayers(List<GridValidationResult> warnings)
        {
            var chat = Torch.CurrentSession?.Managers?.GetManager<IChatManagerServer>();
            if (chat == null)
                return;
            
            var messages = GridMessages.Get(Config);

            foreach (var playerWarnings in warnings.GroupBy(w => w.Grid.OwnerId))
            {
                var ownerId = playerWarnings.Key;
                var identity = MySession.Static.Players.TryGetIdentity(ownerId);

                if (identity == null)
                    continue;
                // Find the player object for the owner of the grids with warnings
                var player = Torch.CurrentSession.KeenSession.Players
                    .GetOnlinePlayers()
                    .FirstOrDefault(p => p.IsRealPlayer && p.Identity != null && p.Identity.IdentityId == ownerId);

                if (player == null)
                    continue;

                string message = messages.GridsRequireAttentionHeader + "\n\n";

                foreach (var warning in playerWarnings)
                {
                    message += $"{warning.Grid.Name}\n";

                    foreach (var problem in warning.Problems)
                        message += $"  \u2022 {problem}\n";

                    message += "\n";
                }

                chat.SendMessageAsOther("Grid Removal Warning", message, Color.Yellow, player.Id.SteamId);
            }
        }

        private void RespondScanSummary(List<CommandContext> requesters, List<GridValidationResult> warnings, string summary)
        {
            var messages = GridMessages.Get(Config);

            string message = warnings.Count == 0
                ? messages.NoGridsRequireWarnings
                : $"{string.Format(messages.GridsRequireAttention, warnings.Count)} {summary}";

            foreach (var ctx in requesters)
                ctx.Respond(message); 
        }
        // ---------------- Full report dispatch ----------------
        private void RespondFullReport(List<CommandContext> requesters, List<GridValidationResult> warnings)
        {
            var messages = GridMessages.Get(Config);

            if (warnings.Count == 0)
            {
                foreach (var ctx in requesters)
                    ctx.Respond(messages.NoGridsRequireAttention);
                return;
            }

            foreach (var ctx in requesters)
            {
                ctx.Respond(string.Format(messages.GridsRequireAttention, warnings.Count));

                foreach (var warning in warnings)
                {
                    ctx.Respond($"{warning.Grid.Name} | {messages.OwnerLabel} {warning.Grid.OwnerName} | {messages.BlocksLabel} {warning.Grid.BlockCount}");

                    foreach (var problem in warning.Problems)
                        ctx.Respond($"  - {problem}");
                }
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
