using DiffEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace W1E1.Tests.Helpers
{
    public static class VerifyConfig
    {
        [ModuleInitializer]
        public static void Init()
        {
            DiffRunner.Disabled = true;
        }
    }
}
