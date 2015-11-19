﻿using AvocadoShell.Engine;
using AvocadoShell.PowerShellService.Host;
using AvocadoUtilities;
using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using UtilityLib.Processes;

namespace AvocadoShell.PowerShellService
{
    sealed class PSEngine
    {
        public event EventHandler<ExecDoneEventArgs> ExecDone;

        readonly PowerShell ps;
        readonly IShellUI shellUI;
        readonly Autocomplete autocomplete;
        readonly PSDataCollection<PSObject> stdOut
            = new PSDataCollection<PSObject>();

        public PSEngine(IShellUI ui)
        {
            shellUI = ui;
            ps = createPowershell(ui);
            autocomplete = new Autocomplete(ps);
        }

        static string getIncludeScript(string dir, string script)
        {
            const string FMT = ". {0}";
            var path = Path.Combine(dir, script);
            return string.Format(FMT, path);
        }

        void addProfileScriptToExec()
        {
            var dir = RootDir.Avocado.Apps.MyAppPath;
            var script = getIncludeScript(dir, "profile.ps1");
            ps.AddScript(script);
        }

        void addUserCmdsToExec()
        {
            var cmdArg = EnvUtils.GetArg(0);
            if (cmdArg != null) ps.AddScript(cmdArg);
        }

        public async Task InitEnvironment()
        {
            shellUI.WriteSystemLine($"Welcome to avocado[v{Config.Version}]");

            shellUI.WriteSystemLine("Starting autocompletion service...");
            await autocomplete.InitializeService();
            
            shellUI.WriteSystemLine("Executing profile scripts...");
            runProfileScript();
        }

        void runProfileScript()
        {
            // Run user profile script.
            addProfileScriptToExec();

            // Execute any commands provided via commandline arguments to
            // this process.
            addUserCmdsToExec();

            // Perform the execution all at once.
            execute();
        }

        public void ExecuteCommand(string cmd)
        {
            ps.AddScript(cmd);
            execute();
        }

        void execute()
        {
            invocationStateSubscribe();
            ps.BeginInvoke<PSObject, PSObject>(null, stdOut);
        }

        public void Stop()
        {
            ps.BeginStop(null, null);
        }

        public async Task<string> GetCompletion(
            string input,
            int index,
            bool forward)
        {
            return await autocomplete.GetCompletion(
                input,
                index,
                forward);
        }

        PowerShell createPowershell(IShellUI ui)
        {
            var powershell = PowerShell.Create();
            powershell.Runspace = createRunspace(ui);
            
            // Inititalize output and error streams.
            stdOut.DataAdded += stdoutDataAdded;
            powershell.Streams.Error.DataAdded += stderrDataAdded;

            return powershell;
        }

        Runspace createRunspace(IShellUI ui)
        {
            var host = new CustomHost(ui);
            var runspace = RunspaceFactory.CreateRunspace(host);
            runspace.Open();
            return runspace;
        }

        void invocationStateSubscribe()
        {
            ps.InvocationStateChanged += onInvocationStateChanged;
        }

        void invocationStateUnsubscribe()
        {
            ps.InvocationStateChanged -= onInvocationStateChanged;
        }

        void onInvocationStateChanged(
            object sender,
            PSInvocationStateChangedEventArgs e)
        {
            string error;
            switch (e.InvocationStateInfo.State)
            {
                case PSInvocationState.Failed:
                    error = e.InvocationStateInfo.Reason.Message;
                    break;
                case PSInvocationState.Completed:
                    error = null;
                    break;
                case PSInvocationState.Stopped:
                    error = "Execution aborted.";
                    break;
                default: return;
            }
            finishExecution(error);
        }

        void finishExecution(string error)
        {
            invocationStateUnsubscribe();
            ps.Commands.Clear();

            // Fire event indicating the current execution has finished.
            var path = ps.Runspace.SessionStateProxy
                .Path.CurrentLocation.Path;
            ExecDone(this, new ExecDoneEventArgs(path, error));
        }

        void stderrDataAdded(object sender, DataAddedEventArgs e)
        {
            var data = ps.Streams.Error[e.Index].ToString();
            shellUI.WriteErrorLine(data);
        }

        void stdoutDataAdded(object sender, DataAddedEventArgs e)
        {
            var data = stdOut[e.Index].ToString();
            shellUI.WriteSystemLine(data);
            var str = ps.HistoryString;
        }
    }
}