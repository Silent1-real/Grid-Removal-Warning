using Sandbox.Game.World;
using System.Linq;
using Torch;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRageMath;


namespace Grid_Removal_Warning
{
    [Category("grw")]
    public class GridCommands : CommandModule
    {
        [Command("scan", " shows grids with problems")]

        [Permission(MyPromoteLevel.Admin)]

        public void GridScan()
        {
            var plugin = (Plugin)Context.Plugin;

            var warnings = plugin.RunScan();

            if (warnings.Count == 0)
            {
                Context.Respond("No grids require attention.");
                return;
            }

            Context.Respond($"Found {warnings.Count} grids requiring attention.");

            foreach (var warning in warnings)
            {
                Context.Respond(
                    $"{warning.Grid.Name} | Owner: {warning.Grid.OwnerName} | Blocks: {warning.Grid.BlockCount}"
                );

                foreach (var problem in warning.Problems)
                {
                    Context.Respond($"  - {problem}");
                }
            }
        }

        [Command("check", "Checks your own grids for removal.")]
        [Permission(MyPromoteLevel.None)]
        public void CheckMyGrids()
        {
            var plugin = (Plugin)Context.Plugin;

            var warnings = plugin.RunScan();

            var myWarnings = warnings
                .Where(w => w.Grid.OwnerId == Context.Player.IdentityId)
                .ToList();

            if (myWarnings.Count == 0)
            {
                Context.Respond("None of your grids require attention.");
                return;
            }

            Context.Respond($"Found {myWarnings.Count} of your grids requiring attention.");

            foreach (var warning in myWarnings)
            {
                Context.Respond(
                    $"{warning.Grid.Name} | Blocks: {warning.Grid.BlockCount}");

                foreach (var problem in warning.Problems)
                {
                    Context.Respond($"  - {problem}");
                }
            }
        }


        [Command("warn", "Send grid warnings to affected online players.")]

        [Permission(MyPromoteLevel.Admin)]
        public void WarnPlayers()
        {
            var plugin = (Plugin)Context.Plugin;

            var warnings = plugin.RunScan();

            if (warnings.Count == 0)
            {
                Context.Respond("No grids require warnings.");
                return;
            }


            foreach(var playerWarnings in warnings.GroupBy(w => w.Grid.OwnerId))

                {
                var ownerId = playerWarnings.Key;

                var identity = MySession.Static.Players.TryGetIdentity(ownerId);

                if (identity == null)
                    continue;
                
                var player = Context.Torch.CurrentSession.KeenSession.Players
                    .GetOnlinePlayers()
                    .FirstOrDefault(p => p.Identity != null && p.Identity.IdentityId == ownerId);

                if (player == null)
                    continue;

                string message =
                    "The following grids require attention:\n\n";

                foreach (var warning in playerWarnings)
                {
                    message += $"{warning.Grid.Name}\n";

                    foreach (var problem in warning.Problems)
                    {
                        message += $"  • {problem}\n";
                    }

                    message += "\n";
                }

                var chat = Context.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();

                chat.SendMessageAsOther(
                    "Grid Removal Warning",
                    message,
                    Color.Yellow,
                    player.Id.SteamId
                );
            }


            Context.Respond("Warnings sent to affected players.");
        }
    }
}