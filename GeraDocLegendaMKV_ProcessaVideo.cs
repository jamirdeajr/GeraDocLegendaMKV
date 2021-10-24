using log4net;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace GeraDocLegendaMKV
{
    public partial class GeraDocLegendaMKV

    {

        int matrizX = 0;
        int matrizY = 0;

        int maxX = 3;
        int maxY = 6;
        int pagina = 1;
        int reducao = 3;
        Boolean novaPagina = true;
        Bitmap bitmapPagina = null;
        string thumbFile;
        string paginaFile;
        int seq;
        int numlin;
        int ultlin;
        double lastSeconds = 0.0;
        double totalSeconds;
        StringBuilder legenda;
        ConversionOptions options;
        MediaFile outputFile;
        MediaFile inputFile;
        TimeSpan ts;
        StringFormat sf;
        StringFormat sf2;
        Pen blk;


        private static readonly ILog log = LogManager.GetLogger(typeof(GeraDocLegendaMKV));

        public GeraDocLegendaMKV()
        {
            //LOG4Net
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead("log4net.config"));

            var repo = log4net.LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
        }


        public void ProcessaVideo(string pathvid)
        {


            blk = new Pen(Color.Black, 8);

            try
            {
                DirectoryInfo dInfoPdf = new DirectoryInfo(pathvid);

                foreach (FileInfo fi in dInfoPdf.EnumerateFiles("*.mkv", SearchOption.AllDirectories))
                {

                    try
                    {
                        var srtFile = Path.Combine(fi.DirectoryName, fi.Name.Replace(".mkv", ".srt"));

                        if (File.Exists(srtFile))
                        {
                            inputFile = new MediaFile { Filename = fi.FullName };
                            string line;
                            numlin = 0;

                            seq = 0;
                            lastSeconds = 0.0;

                            using (Engine engine = new Engine())
                            {
                                engine.GetMetadata(inputFile);

                                outputFile = new MediaFile();

                                legenda = new StringBuilder();
                                ts = new TimeSpan(0);
                                TimeSpan ts2 = new TimeSpan(0);
                                TimeSpan lastts = new TimeSpan(0);

                                //Font font = null;
                                sf = new StringFormat
                                {
                                    LineAlignment = StringAlignment.Center,
                                    Alignment = StringAlignment.Center
                                };

                                sf2 = new StringFormat
                                {
                                    LineAlignment = StringAlignment.Near,
                                    Alignment = StringAlignment.Near
                                };


                                paginaFile = Path.Combine(fi.DirectoryName, $"Pagina{pagina:0000}.jpg");

                                double distanciaMaxima = 30.0;

                                System.IO.StreamReader file = new System.IO.StreamReader(srtFile, Encoding.GetEncoding(1252));
                                while ((line = file.ReadLine()) != null)
                                {
                                    //É numero de linha ?
                                    if (Int32.TryParse(line, out numlin))
                                    {
                                        ultlin = numlin;

                                        totalSeconds = ts.TotalSeconds;



                                        //Agora imprime a legenda
                                        //Tem legenda?

                                        while ((lastSeconds > 0.0 && (totalSeconds - lastSeconds) > distanciaMaxima) || legenda.Length > 0)
                                        {
                                            GeraThumb(fi, engine);
                                        }

                                    }
                                    else
                                    {

                                        if (!String.IsNullOrEmpty(line) && line.Length > 12 && line.ElementAt(2) == ':' && line.ElementAt(5) == ':' && line.ElementAt(8) == ',')
                                        {
                                            lastts = ts;
                                            ts = TimeSpan.Parse(line.Substring(0, 12));

                                        }
                                        else
                                        {
                                            if (!String.IsNullOrEmpty(line))
                                                legenda.AppendLine(line);
                                        }
                                    }
                                }

                                //Agora imprime a legenda ULTIMA, se houver

                                //Ainda Tem legenda?
                                if (legenda.Length > 0)
                                {
                                    GeraThumb(fi, engine);
                                }

                                //Faltou fechar pagina?


                                if (!String.IsNullOrEmpty(paginaFile) && bitmapPagina != null)
                                {
                                    //Grava o bitmap
                                    using (var memoryStream = new MemoryStream())
                                    {
                                        bitmapPagina.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                                        using (var dest = new FileStream(paginaFile, FileMode.OpenOrCreate))
                                        {
                                            memoryStream.Seek(0, SeekOrigin.Begin);
                                            memoryStream.CopyTo(dest);
                                        }
                                    }
                                }



                                file.Close();
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        log.Error("Exp." + fi.FullName, e);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Exp.", e);
            }
        }

        public void GeraThumb(FileInfo fi, Engine engine)
        {
            #region Frame atual + inclusao de legenda
            seq++;

            thumbFile = Path.Combine(fi.DirectoryName, $"Seq_{seq:0000#}_Lin{numlin:000#}_{fi.Name.Replace(".mkv", ".jpg")}");

            outputFile.Filename = thumbFile;

            // A propriedade Seek define em qual momento do vídeo você pretende tirar o "snapshot"
            options = new ConversionOptions
            {
                Seek = ts
            };

            engine.GetThumbnail(inputFile, outputFile, options);


            Bitmap bitmap = null;

            using (var stream = File.OpenRead(thumbFile))
            {
                bitmap = (Bitmap)Bitmap.FromStream(stream);
            }

            using (bitmap)
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {

                    GraphicsPath path = new GraphicsPath();
                    graphics.InterpolationMode = InterpolationMode.High;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;

                    bool bold;
                    bool italic;

                    #region Legenda
                    if (legenda.Length > 0)
                    {
                        bold = false;
                        italic = false;

                        if (legenda.ToString().Contains("<i>") || legenda.ToString().Contains("</i>"))
                        {
                            italic = true;
                            legenda.Replace("<i>", "").Replace("</i>", "");
                        }
                        if (legenda.ToString().Contains("<b>") || legenda.ToString().Contains("</b>"))
                        {
                            bold = true;
                            legenda.Replace("<b>", "").Replace("</b>", "");
                        }

                        path.AddString(
                                legenda.ToString(),
                                FontFamily.GenericSansSerif,
                                (bold ? (int)FontStyle.Bold : 0)+
                                (italic ? (int)FontStyle.Italic : 0)
                                ,
                                (float)(bitmap.Height * .08),
                                new Point(bitmap.Width / 2,
                            (int)(bitmap.Height * .8)), sf);

                        
                        graphics.DrawPath(blk, path);
                        graphics.FillPath(Brushes.Yellow, path);

                        legenda.Clear();
                    }
                    #endregion

                    #region Time
                    if (ts != null)
                    {
                        try
                        {
                            GraphicsPath path2 = new GraphicsPath();
                            path2.AddString(

                                       ts.ToString(@"hh\:mm\:ss\.fff"),
                                       FontFamily.GenericSansSerif,
                                       (int)FontStyle.Bold,
                                       (float)(bitmap.Height * .04),
                                       new Point(4, 4), sf2);
                            graphics.FillPath(Brushes.White, path2);
                        }
                        catch (Exception e)
                        {
                            log.Error(e);
                        }
                    }

                    #endregion

                    //bitmap.Save(thumbFile);
                }


                lastSeconds = totalSeconds;
                #endregion

                #region Controla Matriz Imagens
                matrizX++;

                if (matrizX > maxX)
                {
                    matrizX = 0;
                    matrizY++;
                    if (matrizY > maxY)
                    {
                        matrizY = 0;
                        novaPagina = true;
                    }
                }
                #endregion

                #region  Gera Paginas
                if (novaPagina)
                {
                    if (bitmapPagina != null)
                    {
                        //Grava o bitmap
                        using (var memoryStream = new MemoryStream())
                        {
                            bitmapPagina.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                            using (var dest = new FileStream(paginaFile, FileMode.OpenOrCreate))
                            {
                                memoryStream.Seek(0, SeekOrigin.Begin);
                                memoryStream.CopyTo(dest);
                            }
                        }
                        bitmapPagina.Dispose();
                        bitmapPagina = null;
                    }

                    paginaFile = Path.Combine(fi.DirectoryName, $"Pagina{pagina:0000}.jpg");

                    bitmapPagina = new Bitmap((int)((bitmap.Width * maxX) / reducao), (int)((bitmap.Height * maxY) / reducao));

                    novaPagina = false;
                    pagina++;
                }

                if (pagina > 0 && bitmapPagina != null)
                {

                    using (Graphics g = Graphics.FromImage(bitmapPagina))
                    {
                        g.DrawImage(bitmap, (int)((bitmap.Width * matrizX) / reducao), (int)((bitmap.Height * matrizY) / reducao), (int)((bitmap.Width) / reducao), (int)((bitmap.Height) / reducao));
                    }

                    //Apaga origem
                    if (!String.IsNullOrEmpty(thumbFile))
                    {
                        File.Delete(thumbFile);
                    }
                }
            }
            #endregion
        }

    }
}
