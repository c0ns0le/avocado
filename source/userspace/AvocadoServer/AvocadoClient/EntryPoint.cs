﻿using AvocadoUtilities.CommandLine;
using System;

namespace AvocadoClient
{
    static class EntryPoint
    {
        static void Main(string[] args)
        {
            var error = Subcommand.Invoke();
            if (error != null) Console.Error.WriteLine(error);
        }
    }
}
