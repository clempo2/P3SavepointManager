using Multimorphic.NetProcMachine.Machine;
using Multimorphic.P3;
using Multimorphic.P3App.Data;
using Multimorphic.P3App.Modes;
using Multimorphic.P3App.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Multimorphic.P3App.GUI.Selector;
using System;
using Multimorphic.NetProc;
using Multimorphic.NetProcMachine.LEDs;

namespace Multimorphic.P3SM.Modes
{
    public class P3SMAttractMode : AttractMode
	{
        // Names of menus
        private const string STATE_ACTION = "Action";
        private const string STATE_PROFILE = "Profile";
        private const string STATE_APP = "App";
        private const string STATE_SAVEPOINT = "Savepoint";
        private const string STATE_NEWNAME = "NewName";

        // Names of file operations
        private const string ACTION_RENAME = "Rename Savepoint";
        private const string ACTION_COPY   = "Copy Savepoint";
        private const string ACTION_DELETE = "Delete Savepoint";
        private const string ACTION_EXIT = "Exit";

        private const string BACKUP_EXTENSION = ".backup";

        private string state;
        private string action;
        private string profileName;
        private string appName;
        private string savepointName;
        private string newSavepointName;
        private string backupPath;

        // Need higher priority than ProfileManagerMode
		public P3SMAttractMode (P3Controller controller, int priority, string SceneName)
			: base(controller, 52111, SceneName)
		{
            AddModeEventHandler("Evt_ProfileSelectorResult", ProfileSelectorResultEventHandler, base.Priority);
            AddModeEventHandler("Evt_ProfileSelectorCancelled", ProfileSelectorCancelledEventHandler, base.Priority);
            AddModeEventHandler("Evt_ConfirmationBoxConfirm", ConfirmationBoxConfirmEventHandler, base.Priority);
            AddModeEventHandler("Evt_ProfileNameEntryCompleted", ProfileNameEntryCompletedEventHandler, base.Priority);
            AddModeEventHandler("Evt_ProfileNameEntryCancelled", ProfileNameEntryCancelledEventHandler, base.Priority);
        }

        public override void mode_started ()
		{
			base.mode_started ();

            // Remove <None> and "Add Profile" from profile selection menu
            GameAttribute addProfileAttr = data.GetGameAttribute("SelectAddProfileAllowed");
            addProfileAttr.Set(false);
            GameAttribute noneProfileAttr = data.GetGameAttribute("SelectNoneProfileAllowed");
            noneProfileAttr.Set(false);
        }

        public override void mode_stopped()
        {
            RemoveModeEventHandler("Evt_ProfileSelectorResult", ProfileSelectorResultEventHandler, base.Priority);
            RemoveModeEventHandler("Evt_ProfileSelectorCancelled", ProfileSelectorCancelledEventHandler, base.Priority);
            RemoveModeEventHandler("Evt_ConfirmationBoxConfirm", ConfirmationBoxConfirmEventHandler, base.Priority);
            RemoveModeEventHandler("Evt_ProfileNameEntryCompleted", ProfileNameEntryCompletedEventHandler, base.Priority);
            RemoveModeEventHandler("Evt_ProfileNameEntryCancelled", ProfileNameEntryCancelledEventHandler, base.Priority);
            base.mode_stopped();
        }

        private string GetProfilePath()
        {
            // Note: stock profiles under .../DefaultProfiles never have savepoints
            // because they serve as seeds for new profiles, never as player profiles.

            return DataManager.GetDefaultP3Path() + "PlayerProfiles" + Path.DirectorySeparatorChar + profileName;
        }

        private string GetSavedGamesPath()
        {
            return GetProfilePath() + Path.DirectorySeparatorChar + appName + Path.DirectorySeparatorChar + "SavedGames";
        }

        private string GetSavepointPath()
        {
            // Only valid after the Savepoint was selected
            return GetSavedGamesPath() + Path.DirectorySeparatorChar + savepointName + ".gamestate";
        }

        private string GetNewSavepointPath()
        {
            // Only valid after the new savepoint name has been entered
            return GetSavedGamesPath() + Path.DirectorySeparatorChar + newSavepointName + ".gamestate";
        }

        private List<string> GetSubdirectories(string parentPath)
        {
            DirectoryInfo parentDir = new DirectoryInfo(parentPath);
            DirectoryInfo[] subDirs = parentDir.GetDirectories();
            List<string> list = subDirs.Select(subDir => subDir.Name).ToList();
            return list;
        }

        private List<string> GetFileNamesWithoutExtension(string parentPath, string searchPattern)
        {
            DirectoryInfo parentDir = new DirectoryInfo(parentPath);
            List<string> list = new List<string>();
            if (parentDir.Exists)
            {
                FileInfo[] files = parentDir.GetFiles(searchPattern);
                list = files.Select(fileInfo => Path.GetFileNameWithoutExtension(fileInfo.Name)).ToList();
            }
            return list;
        }

        public override void SceneLiveEventHandler(string evtName, object evtData)
        {
            base.SceneLiveEventHandler(evtName, evtData);
            ushort[] blue = { 0, 0, 50, 255 };
            foreach (LED led in p3.LEDs.Values)
            {
                LEDScript script = new LEDScript(led, Priority);
                LEDHelpers.OnLED(p3, script, blue);
            }
            PromptForAction();
        }

        private void PromptForAction()
        {
            action = null;
            profileName = null;
            appName = null;
            savepointName = null;
            newSavepointName = null;
            backupPath = null;

            state = STATE_ACTION;

            List<string> list = new List<string>();
            list.Add("Choose Action");
            list.Add(ACTION_RENAME);
            list.Add(ACTION_COPY);
            list.Add(ACTION_DELETE);
            list.Add(ACTION_EXIT);

            SetSelectorData("ProfileSelector", list);
            OpenDialog("ProfileDialog");
        }

        private void PromptForProfileName()
        {
            state = STATE_PROFILE;
            PostModeEventToModes("Evt_ChooseProfile", false);
        }

        private void PromptForApp()
        {
            state = STATE_APP;

            List<string> list = GetSubdirectories(GetProfilePath());
            list.Insert(0, "Choose App");

            SetSelectorData("ProfileSelector", list);
            OpenDialog("ProfileDialog");
        }

        private void PromptForSavepoint()
        {
            state = STATE_SAVEPOINT;

            List<string> list = GetFileNamesWithoutExtension(GetSavedGamesPath(), "*.gamestate");
            if (list.Count == 0)
            {
                ShowPopup("No Savepoints", PromptForApp);
                return;
            }

            list.Insert(0, "Choose Savepoint");

            SetSelectorData("ProfileSelector", list);
            OpenDialog("ProfileDialog");
        }

        private void PromptForNewName()
        {
            state = STATE_NEWNAME;
            SetSelectorData("ProfileNameTextSelector", new TextSelectorData("Enter new savepoint", new List<string>(), ""));
            OpenDialog("ProfileNameEditor");
        }

        private void PromptForOverwrite()
        {
            PromptForConfirmation("Overwrite " + newSavepointName + "?");
        }

        private void PromptForDelete()
        {
            PromptForConfirmation("Delete " + savepointName + "?");
        }

        private void PromptForConfirmation(string question)
        {
            List<string> list = new List<string>();
            list.Add("Confirm");
            list.Add(question);
            list.Add("No");
            list.Add("Yes");
            SetSelectorData("ConfirmationSelector", list);
            OpenDialog("ConfirmationDialog");
        }

        private void PromptPreviousState()
        {
            // Go back one state
            if (state == STATE_NEWNAME)
            {
                PromptForSavepoint();
            }
            else if (state == STATE_SAVEPOINT)
            {
                PromptForApp();
            }
            else if (state == STATE_APP)
            {
                PromptForProfileName();
            }
            else
            {
                PromptForAction();
            }
        }

        private void ShowPopup(string message, Action prompt)
        {
            Logger.LogError("P3SMAttractMode ShowPopup " + message);
            // loop is a work-around for color bug in TextReceiver
            for (int i = 0; i < 10; i++)
                PostModeEventToModes("Evt_ShowPopup", message);
            if (prompt != null)
            {
                delay("prompt", EventType.None, 3.0, new Multimorphic.P3.VoidDelegateNoArgs(prompt));
            }
        }

        private bool ProfileSelectorResultEventHandler(string evtName, object evtData)
        {
            string selection = (string)evtData;
            CloseDialog("ProfileDialog");

            // Advance one state

            if (state == STATE_ACTION)
            {
                action = selection;
                if (action == ACTION_EXIT)
                {
                    PostModeEventToGUI("Evt_Exit", 0);
                }
                else
                {
                    PromptForProfileName();

                }
            }
            else if (state == STATE_PROFILE)
            {
                profileName = selection;
                PromptForApp();
            }
            else if (state == STATE_APP)
            {
                appName = selection;
                PromptForSavepoint();
            }
            else if (state == STATE_SAVEPOINT)
            {
                savepointName = selection;
                if (action == ACTION_DELETE)
                {
                    PromptForDelete();
                }
                else
                {
                    PromptForNewName();
                }
            }
            return EVENT_STOP;
        }

        private bool ProfileSelectorCancelledEventHandler(string evtName, object evtData)
        {
            CloseDialog("ProfileDialog");
            PromptPreviousState();
            return EVENT_STOP;
        }

        private bool ConfirmationBoxConfirmEventHandler(string evtName, object evtData)
        {
            bool confirmed = (int)evtData == 1;

            if (confirmed)
            {
                if (action == ACTION_RENAME)
                {
                    RenameSavepoint(true);
                }
                else if (action == ACTION_COPY)
                {
                    CopySavepoint(true);
                }
                else if (action == ACTION_DELETE)
                {
                    DeleteSavepoint();
                }
            }
            else
            {
                // Cancel everything and go back to top menu
                PromptForAction();
            }
            return EVENT_STOP;
        }

        private bool ProfileNameEntryCompletedEventHandler(string evtName, object evtData)
        {
            newSavepointName = ((string)evtData).Trim();
            CloseDialog("ProfileNameEditor");

            if (action == ACTION_RENAME)
            {
                RenameSavepoint(false);
            }
            else if (action == ACTION_COPY)
            {
                CopySavepoint(false);
            }
            return EVENT_STOP;
        }

        private bool ProfileNameEntryCancelledEventHandler(string evtName, object evtData)
        {
            CloseDialog("ProfileNameEditor");
            PromptPreviousState();
            return EVENT_STOP;
        }

        private void RenameSavepoint(bool confirmed)
        {
            string savepoint = GetSavepointPath();
            string newSavepoint = GetNewSavepointPath();

            if (CheckDestinationExists(savepoint, newSavepoint, confirmed))
            {
                return;
            }

            Logger.Log(LogCategories.Mode, "RenameSavepoint: " + savepoint + " To: " + newSavepoint);
            try
            {
                File.Move(savepoint, newSavepoint);
                ShowPopup("Savepoint Renamed", PromptForAction);
                DeleteBackup();
            }
            catch (Exception e)
            {
                Logger.LogError("Error renaming savepoint: " + savepoint + " To: " + newSavepoint);
                Logger.LogException(e);
                ShowPopup("Rename Failed", PromptForAction);
                RestoreBackup();
            }
        }

        private void CopySavepoint(bool confirmed)
        {
            string savepoint = GetSavepointPath();
            string newSavepoint = GetNewSavepointPath();

            if (CheckDestinationExists(savepoint, newSavepoint, confirmed))
            {
                return;
            }

            Logger.Log(LogCategories.Mode, "CopySavepoint: " + savepoint + " To: " + newSavepoint);
            try
            {
                File.Copy(savepoint, newSavepoint);
                ShowPopup("Savepoint Copied", PromptForAction);
                DeleteBackup();
            }
            catch (Exception e)
            {
                Logger.LogError("Error copying savepoint: " + savepoint + " To: " + newSavepoint);
                Logger.LogException(e);
                ShowPopup("Copy Failed", PromptForAction);
                RestoreBackup();
            }
        }

        private void DeleteSavepoint()
        {
            string savepoint = GetSavepointPath();
            Logger.Log(LogCategories.Mode, "DeleteSavepoint: " + savepoint);
            try
            {
                File.Delete(savepoint);
                ShowPopup("Savepoint Deleted", PromptForAction);
            }
            catch (Exception e)
            {
                Logger.LogError("Error deleting savepoint: " + savepoint);
                Logger.LogException(e);
                ShowPopup("Delete Failed", PromptForAction);
            }
        }

        // Return true if the destination exists and we need to ask the user.
        // Return false if the operation can go ahead.
        private bool CheckDestinationExists(string savepoint, string newSavepoint, bool confirmed)
        {
            if (savepoint == newSavepoint)
            {
                ShowPopup("Enter a different name", PromptForNewName);
                return true;
            }
            if (File.Exists(newSavepoint))
            {
                if (!confirmed)
                {
                    PromptForOverwrite();
                    return true;
                }

                // Backup the file, if the operation fails we can restore later
                try
                {
                    CreateBackup(newSavepoint);
                }
                catch (Exception e)
                {
                    Logger.LogError("Error backuping savepoint: " + newSavepoint + " To: " + backupPath);
                    Logger.LogException(e);
                    ShowPopup("CreateBackup Failed", PromptForAction);
                    return true;
                }
            }

            // the operation can continue
            return false;
        }

        // Create a backup copy of a savepoint
        // throws on error
        private void CreateBackup(string newSavepoint)
        {
            backupPath = newSavepoint + BACKUP_EXTENSION;
            File.Move(newSavepoint, backupPath);
        }

        // Delete the backup if there is one
        // Never throws
        private void DeleteBackup()
        {
            try
            {
                if (backupPath != null)
                {
                    File.Delete(backupPath);
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Error deleting backup: " + backupPath);
                Logger.LogException(e);
                ShowPopup("DeleteBackup Failed", null);
                // Do not rethrow
            }
        }

        // Restores the savepoint if there is a backup
        // Never throws
        private void RestoreBackup()
        {
            try
            {
                if (backupPath != null)
                {
                    string originalSavepoint = backupPath.Substring(0, backupPath.Length - BACKUP_EXTENSION.Length);
                    File.Delete(originalSavepoint);
                    File.Move(backupPath, originalSavepoint);
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Error restoring backup: " + backupPath);
                Logger.LogException(e);
                // Do not overwrite existing popup dialog
                // Do not rethrow
            }
        }
    }
}

