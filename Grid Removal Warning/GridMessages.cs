using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grid_Removal_Warning
{

    public class MessageSet
    {
        // GridValidator.cs
        public string MissingPrefix;       // e.g. "Missing" + block name (English) appended after
        public string GenericGridName;     // "Generic grid name"

        // Plugin.cs - scan/warn immediate replies
        public string ScanAlreadyRunning;      // "A scan is already in progress. You'll receive the report when it finishes."
        public string ScanStarted;             // "Scan started. You'll receive the report when it finishes."
        public string WarnAlreadyRunning;      // "A scan is already in progress. Warnings will be sent once it finishes."
        public string WarnStarted;             // "Scan started. Warnings will be sent once it finishes."

        // Plugin.cs - scan/warn deferred admin reports
        public string NoGridsRequireWarnings;      // "No grids require warnings."
        public string GridsRequireAttention;       // "Found {0} grids requiring attention." - {0} = count
        public string WarningsSentToPlayers;       // "Warnings sent to affected players."
        public string NoGridsRequireAttention;     // "No grids require attention."
        public string OwnerLabel;                  // "Owner:"
        public string BlocksLabel;                 // "Blocks:"

        // Plugin.cs - player-facing warn/check messages
        public string GridsRequireAttentionHeader;     // "The following grids require attention:"
        public string NoneOfYourGridsRequireAttention; // "None of your grids require attention."
        public string FoundYourGridsRequiringAttention;// "Found {0} of your grids requiring attention." - {0} = count
        public string PleaseWaitBeforeChecking;        // "Please wait {0}s before checking again." - {0} = seconds

        // GridCommands.cs
        public string InGameOnlyCommand; // "This command only works in-game."
    }
    // English
    public static class GridMessages
    {
        public static MessageSet English() => new MessageSet

        {
            // GridValidator.cs
            MissingPrefix = "Missing",
            GenericGridName = "Generic grid name",

            // Plugin.cs - scan/warn immediate replies
            ScanAlreadyRunning = "A scan is already in progress. You'll receive the report when it finishes.",
            ScanStarted = "Scan started. You'll receive the report when it finishes.",
            WarnAlreadyRunning = "A scan is already in progress. Warnings will be sent once it finishes.",
            WarnStarted = "Scan started. Warnings will be sent once it finishes.",

            // Plugin.cs - scan/warn deferred admin reports
            NoGridsRequireWarnings = "No grids require warnings.",
            GridsRequireAttention = "Found {0} grids requiring attention.",
            WarningsSentToPlayers = "Warnings sent to affected players.",
            NoGridsRequireAttention = "No grids require attention.",
            OwnerLabel = "Owner:",
            BlocksLabel = "Blocks:",

            // Plugin.cs - player-facing warn/check messages
            GridsRequireAttentionHeader = "The following grids require attention:",
            NoneOfYourGridsRequireAttention = "None of your grids require attention.",
            FoundYourGridsRequiringAttention = "Found {0} of your grids requiring attention.",
            PleaseWaitBeforeChecking = "Please wait {0}s before checking again.",

            // GridCommands.cs
            InGameOnlyCommand = "This command only works in-game."

        };
        // German @DarkFight
        public static MessageSet German() => new MessageSet
        {
            // GridValidator.cs
            MissingPrefix = "Fehlender",
            GenericGridName = "Name der Blockkonstruktion",
            // Plugin.cs - scan/warn immediate replies
            ScanAlreadyRunning = "Ein Scan läuft bereits. Sie erhalten den Bericht, wenn er abgeschlossen ist.",
            ScanStarted = "Scan gestartet. Sie erhalten den Bericht, wenn er abgeschlossen ist.",
            WarnAlreadyRunning = "Ein Scan läuft bereits. Warnungen werden gesendet, sobald er abgeschlossen ist.",
            WarnStarted = "Scan gestartet. Warnungen werden gesendet, sobald er abgeschlossen ist.",
            // Plugin.cs - scan/warn deferred admin reports
            NoGridsRequireWarnings = "Grids haben keine Warnungen.",
            GridsRequireAttention = "{0} Grids erfordern Aufmerksamkeit.",
            WarningsSentToPlayers = "Warnungen an betroffene Spieler gesendet.",
            NoGridsRequireAttention = "Grids benötigen keine Aufmerksamkeit.",
            OwnerLabel = "Besitzer:",
            BlocksLabel = "Blöcke:",
            // Plugin.cs - player-facing warn/check messages
            GridsRequireAttentionHeader = "Die folgenden Grids erfordern Aufmerksamkeit:",
            NoneOfYourGridsRequireAttention = "Keines Ihrer Grids erfordert Aufmerksamkeit.",
            FoundYourGridsRequiringAttention = "{0} Ihrer Grids erfordern Aufmerksamkeit.",
            PleaseWaitBeforeChecking = "Bitte warten Sie {0}s, bevor Sie erneut prüfen.",
            // GridCommands.cs
            InGameOnlyCommand = "Dieser Befehl funktioniert nur im Spiel."
        };

        // Russian
        public static MessageSet Russian() => new MessageSet
        {
            // GridValidator.cs
            MissingPrefix = "Отсутствует",
            GenericGridName = "Общее имя сетки",
            // Plugin.cs - scan/warn immediate replies
            ScanAlreadyRunning = "Сканирование уже выполняется. Вы получите отчет, когда оно завершится.",
            ScanStarted = "Сканирование началось. Вы получите отчет, когда оно завершится.",
            WarnAlreadyRunning = "Сканирование уже выполняется. Предупреждения будут отправлены после его завершения.",
            WarnStarted = "Сканирование началось. Предупреждения будут отправлены после его завершения.",
            // Plugin.cs - scan/warn deferred admin reports
            NoGridsRequireWarnings = "Нет сеток, требующих предупреждений.",
            GridsRequireAttention = "Найдено {0} сеток, требующих внимания.",
            WarningsSentToPlayers = "Предупреждения отправлены затронутым игрокам.",
            NoGridsRequireAttention = "Нет сеток, требующих внимания.",
            OwnerLabel = "Владелец:",
            BlocksLabel = "Блоки:",
            // Plugin.cs - player-facing warn/check messages
            GridsRequireAttentionHeader = "Следующие сетки требуют внимания:",
            NoneOfYourGridsRequireAttention = "Ни одна из ваших сеток не требует внимания.",
            FoundYourGridsRequiringAttention = "Найдено {0} ваших сеток, требующих внимания.",
            PleaseWaitBeforeChecking = "Пожалуйста, подождите {0} секунд перед повторной проверкой.",
            // GridCommands.cs
            InGameOnlyCommand = "Эта команда работает только в игре."
        };

        //French
        public static MessageSet French() => new MessageSet
        {
            // GridValidator.cs
            MissingPrefix = "Manquant",
            GenericGridName = "Nom de grille générique",
            // Plugin.cs - scan/warn immediate replies
            ScanAlreadyRunning = "Un scan est déjà en cours. Vous recevrez le rapport une fois qu'il sera terminé.",
            ScanStarted = "Scan démarré. Vous recevrez le rapport une fois qu'il sera terminé.",
            WarnAlreadyRunning = "Un scan est déjà en cours. Les avertissements seront envoyés une fois qu'il sera terminé.",
            WarnStarted = "Scan démarré. Les avertissements seront envoyés une fois qu'il sera terminé.",
            // Plugin.cs - scan/warn deferred admin reports
            NoGridsRequireWarnings = "Aucune grille ne requiert d'avertissement.",
            GridsRequireAttention = "{0} grilles requièrent une attention particulière.",
            WarningsSentToPlayers = "Avertissements envoyés aux joueurs concernés.",
            NoGridsRequireAttention = "Aucune grille ne requiert d'attention particulière.",
            OwnerLabel = "Propriétaire :",
            BlocksLabel = "Blocs :",
            // Plugin.cs - player-facing warn/check messages
            GridsRequireAttentionHeader = "Les grilles suivantes requièrent une attention particulière :",
            NoneOfYourGridsRequireAttention = "Aucune de vos grilles ne requiert d'attention particulière.",
            FoundYourGridsRequiringAttention = "{0} de vos grilles requièrent une attention particulière.",
            PleaseWaitBeforeChecking = "Veuillez patienter {0}s avant de vérifier à nouveau.",
            // GridCommands.cs
            InGameOnlyCommand = "Cette commande ne fonctionne que dans le jeu."
        };

        public static MessageSet Get(Config config)
        {
            switch (config.PreferredLanguage)
            {
                case Language.German:
                    return German();
                case Language.Russian:
                    return Russian();
               // case Language.French:
                //    return French();
                default:
                    return English();
            }
        }
    }
}


