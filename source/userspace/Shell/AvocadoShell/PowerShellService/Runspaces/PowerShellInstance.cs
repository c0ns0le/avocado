﻿using AvocadoShell.Engine;
using AvocadoShell.PowerShellService.Host;
using AvocadoShell.PowerShellService.Modules;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using UtilityLib.MiscTools;
using UtilityLib.Processes;

namespace AvocadoShell.PowerShellService.Runspaces
{
    sealed class PowerShellInstance
    {
        public event EventHandler<ExecDoneEventArgs> ExecDone;
        public event EventHandler ExitRequested;

        readonly IShellUI shellUI;
        readonly ExecutingPipeline pipeline;
        readonly Autocomplete autocomplete;

        public PowerShellInstance(IShellUI ui) : this(ui, null) { }

        public PowerShellInstance(IShellUI ui, string remoteComputerName)
        {
            shellUI = ui;

            // Create PowerShell service objects.
            var remoteInfo = createRemoteInfo(remoteComputerName);
            var powershell = createPowershell(ui, remoteInfo);
            pipeline = createPipeline(powershell.Runspace);

            // No support for autocompletion while remoting.
            if (!IsRemote) autocomplete = new Autocomplete(powershell);
        }

        public bool IsRemote => pipeline.Runspace.RunspaceIsRemote;

        public string GetWorkingDirectory() => pipeline.GetWorkingDirectory();

        public async Task InitEnvironment()
        {
            shellUI.WriteOutputLine($"Booting avocado [v{Config.Version}]");
            await doWork(
                "Starting autocompletion service",
                autocomplete?.InitializeService());
            await doWork("Running startup scripts", runStartupScripts);
        }

        async Task doWork(string message, Action action)
            => await doWork(message, Task.Run(action));

        async Task doWork(string message, Task work)
        {
            if (work == null) return;
            shellUI.WriteCustom($"{message}...", Config.SystemFontBrush, false);
            await work;
            shellUI.WriteOutputLine("Done.");
        }

        void runStartupScripts()
        {
            ExecuteCommand(
                EnvUtils.GetEmbeddedText("AvocadoShell.Assets.startup.ps1"));
        }

        public void ExecuteCommand(string cmd)
        {
            pipeline.AddScript(cmd);
            pipeline.Execute();
        }

        public void Stop() => pipeline.Stop();

        public async Task<string> GetCompletion(
            string input,
            int index,
            bool forward)
        {
            // No support for autocompletion while remoting.
            if (IsRemote) return null;
            return await autocomplete.GetCompletion(input, index, forward);
        }
        
        WSManConnectionInfo createRemoteInfo(string computerName)
        {
            return computerName == null
                ? null
                : new WSManConnectionInfo { ComputerName = computerName };
        }
        
        PowerShell createPowershell(IShellUI ui, WSManConnectionInfo remoteInfo)
        {
            var powershell = PowerShell.Create();
            powershell.Runspace = createRunspace(ui, remoteInfo);
            return powershell;
        }
        
        Runspace createRunspace(IShellUI ui, WSManConnectionInfo remoteInfo)
        {
            // Initialize custom PowerShell host.
            var host = new CustomHost(ui);
            host.ExitRequested += (s, e) => ExitRequested(this, e);

            // Initialize local or remote runspace.
            var runspace = remoteInfo == null
                ? RunspaceFactory.CreateRunspace(host)
                : RunspaceFactory.CreateRunspace(host, remoteInfo);
            runspace.Open();

            return runspace;
        }

        ExecutingPipeline createPipeline(Runspace runspace)
        {
            var pipeline = new ExecutingPipeline(runspace);
            pipeline.Done += (s, e) => ExecDone(this, e);
            pipeline.OutputReceived += onOutputReceived;
            pipeline.ErrorReceived += onErrorReceived;
            return pipeline;
        }

        void onOutputReceived(object sender, IEnumerable<string> e)
            => e.ForEach(shellUI.WriteOutputLine);

        void onErrorReceived(object sender, IEnumerable<string> e)
            => e.ForEach(shellUI.WriteErrorLine);
    }
}