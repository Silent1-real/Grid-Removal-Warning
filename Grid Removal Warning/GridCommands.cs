using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace Grid_Removal_Warning
{
    [Category("grw")]
    public class GridCommands : CommandModule
    {
        // Starts a scan cycle (or attaches to one already running) and reports
        // results to the admin once it completes. No messages sent to players.
        [Command("scan", "Scans all grids and reports problems to you.")]
        [Permission(MyPromoteLevel.Admin)]
        public void GridScan()
        {
            var plugin = (Plugin)Context.Plugin;

            string reply = plugin.RequestScan(Context);

            Context.Respond(reply);
        }

        // Starts a scan cycle (or attaches to one already running). Once complete,
        // affected players are messaged and the requester gets a summary.
        [Command("warn", "Send grid warnings to affected online players.")]
        [Permission(MyPromoteLevel.Admin)]
        public void WarnPlayers()
        {
            var plugin = (Plugin)Context.Plugin;

            string reply = plugin.RequestWarn(Context);

            Context.Respond(reply);
        }

        // Instant, single-player check - not part of the batched cycle.
        [Command("check", "Checks your own grids for removal.")]
        [Permission(MyPromoteLevel.None)]
        public void CheckMyGrids()
        {
            var plugin = (Plugin)Context.Plugin;

            if (Context.Player == null)
            {
                Context.Respond(GridMessages.Get(plugin.Config).InGameOnlyCommand);
                return;
            }

            long identityId = Context.Player.IdentityId;
            ulong steamId = Context.Player.SteamUserId;

            bool success = plugin.TryCheckPlayer(identityId, out var myWarnings, out var cooldownMessage);

            if (!success)
            {
                plugin.SendCooldownMessageTo(steamId, cooldownMessage);
                return;
            }

            plugin.SendCheckResultTo(steamId, myWarnings);
        }
    }
}
