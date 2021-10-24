using System;

namespace GeraDocLegendaMKV
{
    public partial class GeraDocLegendaMKV
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {

                GeraDocLegendaMKV ger = new GeraDocLegendaMKV();

                ger.ProcessaVideo(args[0]);
            } else
            {
                Console.WriteLine("Falta informar a pasta que contém os arquivos MKV + legendas .srt");
            }
        }
    }
}
