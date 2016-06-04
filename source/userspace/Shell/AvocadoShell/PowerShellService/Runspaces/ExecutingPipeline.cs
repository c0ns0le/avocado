﻿using AvocadoShell.PowerShellService.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using UtilityLib.MiscTools;

namespace AvocadoShell.PowerShellService.Runspaces
{
    sealed class ExecutingPipeline
    {
        public event EventHandler<ExecDoneEventArgs> Done;
        public event EventHandler<IEnumerable<string>> OutputReceived;
        public event EventHandler<IEnumerable<string>> ErrorReceived;

        Pipeline pipeline;

        public Runspace Runspace => pipeline.Runspace;

        public ExecutingPipeline(Runspace runspace)
        {
            pipeline = runspace.CreatePipeline();
        }

        public void Stop()
        {
            // Only stop the pipeline if it is running.
            if (pipeline.PipelineStateInfo.State == PipelineState.Running)
            {
                pipeline.StopAsync();
            }
        }

        public void AddScript(string script)
            => pipeline.Commands.AddScript(script);

        public void Execute()
        {
            pipeline.StateChanged += onPipelineStateChanged;
            pipeline.Output.DataReady += onOutputDataReady;
            pipeline.Error.DataReady += onErrorDataReady;
            pipeline.InvokeAsync();
        }

        void onPipelineStateChanged(object sender, PipelineStateEventArgs e)
        {
            string error;
            switch (e.PipelineStateInfo.State)
            {
                case PipelineState.Completed:
                    error = null;
                    break;
                case PipelineState.Failed:
                    error = e.PipelineStateInfo.Reason.Message;
                    break;
                case PipelineState.Stopped:
                    error = "Execution aborted.";
                    break;
                default: return;
            }

            // Reset the pipeline.
            pipeline = pipeline.Runspace.CreatePipeline();

            // Fire event indicating execution of the pipeline is finished.
            Done(this, new ExecDoneEventArgs(error));
        }

        public async Task<string> GetWorkingDirectory()
        {
            // SessionStateProxy properties are not supported in remote 
            // runspaces, so we must manually get the working directory by
            // running a PowerShell command.
            if (pipeline.Runspace.RunspaceIsRemote)
            {
                var result = await RunBackgroundCommand(
                    "$PWD.Path.Replace($HOME, '~')");
                return result.First();
            }

            var homeDir = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);
            return pipeline.Runspace.SessionStateProxy
                .Path.CurrentLocation.Path
                .Replace(homeDir, "~");
        }

        public async Task<IEnumerable<string>> RunBackgroundCommand(
            string command)
        {
            var tempPipeline = pipeline.Runspace.CreatePipeline(command);
            var result = await Task.Run(() => tempPipeline.Invoke());
            return result.Select(l => l.ToString());
        }

        void onOutputDataReady(object sender, EventArgs e)
        {
            var outputList = new List<string>();

            var reader = sender as PipelineReader<PSObject>;
            while (reader.Count > 0)
            {
                OutputFormatter
                    .FormatPSObject(reader.Read())
                    .ForEach(outputList.Add);
            }

            OutputReceived(this, outputList);
        }
        
        void onErrorDataReady(object sender, EventArgs e)
        {
            var errorList = new List<string>();

            var reader = sender as PipelineReader<object>;
            while (reader.Count > 0)
            {
                errorList.Add(reader.Read().ToString());
            }

            ErrorReceived(this, errorList);
        }
    }
}