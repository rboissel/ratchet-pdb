using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdbDump
{
    class Program
    {
        static void Main(string[] args)
        {
            Ratchet.IO.Format.PDB pdb = Ratchet.IO.Format.PDB.Open( System.IO.File.Open(args[0], System.IO.FileMode.Open, System.IO.FileAccess.Read));
            Console.WriteLine("PDB:");
            Console.WriteLine(" * Version: " + pdb.Version.ToString());
            Console.WriteLine(" * Guid: " + pdb.Guid.ToString());
            Console.WriteLine(" * Signature: " + pdb.Signature.ToString());
            Console.WriteLine(" * Modules: ");


            foreach (Ratchet.IO.Format.PDB.Module module in pdb.Modules)
            {
                Console.WriteLine("    * Name: " + module.Name);
                Console.WriteLine("    * Files: ");
                foreach (var file in module.Files)
                {
                    Console.WriteLine("      * " + file.Path);
                    Console.WriteLine("        * Lines info: ");

                    foreach (var line in file.Lines)
                    {
                        Console.WriteLine("          l." + line.LineNumber + " (info:" + line.Info + ")");
                    }
                    Console.WriteLine();
                }
                
                Console.WriteLine();
            }
        }
    }
}
