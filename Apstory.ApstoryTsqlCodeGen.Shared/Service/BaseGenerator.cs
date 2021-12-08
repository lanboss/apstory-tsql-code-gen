using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;
using System;

namespace Apstory.ApstoryTsqlCodeGen.Shared.Service
{
    public abstract class BaseGenerator
    {
        protected readonly string _Tab = "    ";
        protected readonly string _NewLine = "\r\n";
        protected readonly bool _Threaded;
        protected readonly ISqlTablesRepository _TableRepository;

        protected readonly string _GenPath = "";
        protected readonly string _ModelGenPath = ""; // "Gen."
        protected readonly string _GenPathNamespace = ".Gen";

        public BaseGenerator(ISqlTablesRepository tableRepository, string genPath, string modelGenPath, string genPathNamespace, bool threaded)
        {
            _TableRepository = tableRepository;
            _Threaded = threaded;
            _GenPath = genPath;
            _ModelGenPath = modelGenPath;
            _GenPathNamespace = genPathNamespace;
        }

        protected void LogOutput(string text = "")
        {
            if (_Threaded)
                return;

            Console.Write(text);
        }

        protected void LogOutputLine(string text = "")
        {
            if (_Threaded)
                return;

            Console.WriteLine(text);
        }

        protected void LogError(string errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorMessage);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
