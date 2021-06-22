﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gridfiles;

namespace geokassa
{
    public class JsonTinCommand : Command
    {
        public JsonTinCommand(string name, string description = null) : base(name, description)
        {
            Name = name;
            Description = description;

            AddArgument(new Argument<FileInfo>("input", "Input file name"));
            AddArgument(new Argument<FileInfo>("output", "Output json file name"));

            AddOption(new Option("--epsgsource", "EPSG code source CRS") { Argument = new Argument<string>("epsgsource"), IsRequired = true });
            AddOption(new Option("--epsgtarget", "EPSG code target CRS") { Argument = new Argument<string>("epsgtarget"), IsRequired = true });
            AddOption(new Option("--version", "Grid version") { Argument = new Argument<string>("version") });

            Handler = CommandHandler.Create((TinModelParams pars) =>
            {
                return HandleCommand(pars);
            });
        }

        private int HandleCommand(TinModelParams par)
        {
            try
            {
                var tin = new TinModel();

                tin.OutputFileName = par.Output.FullName;
                tin.EpsgSource.CodeString = par.EpsgSource;
                tin.EpsgTarget.CodeString = par.EpsgTarget;
                tin.Coord.Version = par.Version;

                if (!tin.CptFile.ReadInputFile(par.Input.FullName))
                {
                    Console.WriteLine($"Could not read from input file {par.Input.Name}");
                    return -1;
                }
                tin.InitTriangleObject();
                if (!tin.Triangulate())
                {
                    Console.WriteLine($"Trianguleringa feila.");
                    return -1;
                }
                if (!tin.SerializeJson())
                {
                    Console.WriteLine($"Serialisering til Json feila.");
                    return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return -1;
                throw ex;
            }
        }
    }

    public class Lsc2GeoTiffCommand : Command
    {
        public Lsc2GeoTiffCommand(string name, string description = null) : base(name, description)
        {
            Name = name;
            Description = description;

            AddArgument(new Argument<FileInfo>("inputsource", "Source csv file (ID, X/lon, Y/lat, Z/h, Epoch)"));
            AddArgument(new Argument<FileInfo>("inputtarget", "Target csv file (ID, X/lon, Y/lat, Z/h, Epoch)"));
            AddArgument(new Argument<FileInfo>("output", "Output geotiff file"));

            AddOption(new Option<GeoTiffFile.TiffOutputTypeshort>("--type", "TiffOutputType") { Argument = new Argument<GeoTiffFile.TiffOutputTypeshort>("type") });
            AddOption(new Option("--gridname", "Grid name") { Argument = new Argument<string>("gridname"), IsRequired = true });
            AddOption(new Option("--email", "Product manager") { Argument = new Argument<string>("email") });
            AddOption(new Option("--area", "Area of use") { Argument = new Argument<string>("area") });
            AddOption(new Option("--desc", "Description") { Argument = new Argument<string>("desc") });
            AddOption(new Option("--dim", "Dimension") { Argument = new Argument<int>("dim") });
            AddOption(new Option("--epsg2d", "Source EPSG interpolation ('autority:XXXX')") { Argument = new Argument<string>("epsg2d"), IsRequired = true });
            AddOption(new Option("--epsgsource", "Source EPSG CRS ('autority:XXXX')") { Argument = new Argument<string>("epsgsource"), IsRequired = true });
            AddOption(new Option("--epsgtarget", "Target EPSG CRS ('autority:XXXX')") { Argument = new Argument<string>("epsgtarget"), IsRequired = true });
            AddOption(new Option("--lle", "Lower left longitude in grid (deg)") { Name = "LowerLeftLongitude", Argument = new Argument<double>("lle"), IsRequired = true });
            AddOption(new Option("--lln", "Lower left latitude in grid (deg)") { Name = "LowerLeftLatitude", Argument = new Argument<double>("lln"), IsRequired = true });
            AddOption(new Option("--de", "Longitude resolution in grid (deg)") { Name = "DeltaLongitude", Argument = new Argument<double>("de"), IsRequired = true });
            AddOption(new Option("--dn", "Latitude resolution in grid (deg)") { Name = "DeltaLatitude", Argument = new Argument<double>("dn"), IsRequired = true });
            AddOption(new Option("--rows", "Number of rows in grid") { Argument = new Argument<int>("rows"), IsRequired = true });
            AddOption(new Option("--cols", "Number of cols in grid") { Argument = new Argument<int>("cols"), IsRequired = true });
            AddOption(new Option("--tilesize", "Tile size (multiple of 16)") { Argument = new Argument<int>("tilesize"), IsRequired = true });
            AddOption(new Option("--agl", "Heights above ground level (m)") { Argument = new Argument<double>("agl") });
            AddOption(new Option("--c0", "Covariance signal - LSC (m2)") { Argument = new Argument<double>("c0"), IsRequired = true });
            AddOption(new Option("--cl", "Correlastion length - LSC (m)") { Argument = new Argument<double>("cl"), IsRequired = true });
            AddOption(new Option("--sn", "Covariance noise - LSC (m)") { Argument = new Argument<double>("sn"), IsRequired = true });

            Handler = CommandHandler.Create((Lsc2GeoTiffParams pars) =>
            {
                return HandleCommand(pars);
            });
        }

        private int HandleCommand(Lsc2GeoTiffParams par)
        {
            try
            {
                var tiff = new GeoTiffFile();

                tiff.ImageDescription = par.Desc ?? "";
                tiff.Area_of_use = par.Area ?? "";
                tiff.Email = par.Email ?? "";
                tiff.Dimensions = par.Dim == 0 ? 3 : par.Dim;
                tiff.TileSize = par.TileSize;
                tiff.TiffOutput = (GeoTiffFile.TiffOutputType)par.Type;                
                tiff.Epsg2d.CodeString = par.Epsg2d;
                tiff.EpsgSource.CodeString = par.EpsgSource;
                tiff.EpsgTarget.CodeString = par.EpsgTarget;
                tiff.NRows = par.Rows;
                tiff.NColumns = par.Cols;
                tiff.LowerLeftLatitude = (double)par.LowerLeftLatitude;
                tiff.LowerLeftLongitude = (double)par.LowerLeftLongitude;
                tiff.DeltaLatitude = (double)par.DeltaLatitude;
                tiff.DeltaLongitude = (double)par.DeltaLongitude;              
                tiff.CommonPoints.Agl = par.Agl;

                if (!tiff.ReadSourceFromFile(par.InputSource.FullName))
                {
                    Console.WriteLine($"Could not read {par.InputSource.Name}.");
                    return -1;
                }
                if (!tiff.ReadTargetFromFile(par.InputTarget.FullName))
                {
                    Console.WriteLine($"Could not read {par.InputTarget.Name}.");
                    return -1;
                }
                tiff.CleanNullPoints();
                if (!tiff.PopulatedGrid(par.C0, par.Cl, par.Sn))
                {
                    Console.WriteLine($"Gridding failed.");
                    return -1;
                }
                if (!tiff.GenerateGridFile(par.Output.FullName))
                {
                    Console.WriteLine($"Generation of tiff file {par.Output.Name} failed.");
                    return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return -1;
                throw ex;
            }
        }
    } 

    public class Bin2GeoTiffCommand : Command
    {
        public Bin2GeoTiffCommand(string name, string description = null) : base(name, description)
        {
            Name = name;
            Description = description;

            AddArgument(new Argument<FileInfo>("input", "Input bin file"));
            AddArgument(new Argument<FileInfo>("output", "Output GeoTiff file"));

            AddOption(new Option<GeoTiffFile.TiffOutputTypeshort>("--type", "TiffOutputType") { Argument = new Argument<GeoTiffFile.TiffOutputTypeshort>("type"), IsRequired = true });
            AddOption(new Option("--gridname", "Grid name") { Argument = new Argument<string>("gridname"), IsRequired = true });
            AddOption(new Option("--email", "Product manager") { Argument = new Argument<string>("email") });
            AddOption(new Option("--area", "Area of use") { Argument = new Argument<string>("area") });
            AddOption(new Option("--desc", "Description") { Argument = new Argument<string>("desc") });
            AddOption(new Option("--epsg2d", "Source EPSG interpolation CRS ('autority:XXXX')") { Argument = new Argument<string>("epsg2d"), IsRequired = true });
            AddOption(new Option("--epsg3d", "Source EPSG 3D CRS ('autority:XXXX')") { Argument = new Argument<string>("epsg3d"), IsRequired = true });
            AddOption(new Option("--epsgtarget", "Target EPSG CRS ('autority:XXXX')") { Argument = new Argument<string>("epsgtarget"), IsRequired = true });
            AddOption(new Option("--geoid", "Geoid- or separationmodel") { Argument = new Argument<bool>("geoid") });
            AddOption(new Option("--tilesize", "Tile size (multiple of 16)") { Argument = new Argument<int>("tilesize"), IsRequired = true });

            Handler = CommandHandler.Create((Bin2GeoTiffParams pars) =>
            {
                return HandleCommand(pars);
            });
        }

        private int HandleCommand(Bin2GeoTiffParams par)
        {
            try
            {
                var tiff = new GeoTiffFile();

                tiff.Grid_name = par.GridName ?? "";
                tiff.ImageDescription = par.Desc ?? "";
                tiff.Area_of_use = par.Area ?? "";
                tiff.Email = par.Email;
                tiff.TileSize = par.TileSize;
                tiff.Epsg2d.CodeString = par.Epsg2d;
                tiff.Epsg3d.CodeString = par.Epsg3d;
                tiff.EpsgTarget.CodeString = par.EpsgTarget;
                tiff.Dimensions = 1;
                tiff.TiffOutput = (GeoTiffFile.TiffOutputType)par.Type;
              
                if (!tiff.Gtx.ReadBin(par.Input.FullName))
                {
                    Console.WriteLine($"Importing of bin file {par.Input.Name} failed.");
                    return -1;
                }
                if (!tiff.GenerateGridFile(par.Output.FullName))
                {
                    Console.WriteLine($"Generation of tiff file {par.Output.Name} failed.");
                    return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return -1;
                throw ex;
            }
        }
    }

    public class Gri2GeoTiffCommand : Command
    {
        public Gri2GeoTiffCommand(string name, string description = null) : base(name, description)
        {
            Name = name;
            Description = description;
           
            AddArgument(new Argument<FileInfo> ("output", "Output GeoTiff file"));

            AddOption(new Option<GeoTiffFile.TiffOutputTypeshort>("--type", "TiffOutputType") { Argument = new Argument<GeoTiffFile.TiffOutputTypeshort>("type"), IsRequired = true });
            AddOption(new Option("--inpute", "Gri file - easting") { Argument = new Argument<string>("inpute") });
            AddOption(new Option("--inputn", "Gri file - northing") { Argument = new Argument<string>("inputn") });
            AddOption(new Option("--inputu", "Gri file - height") { Argument = new Argument<string>("inputu") });
            AddOption(new Option("--gridname", "Grid name") { Argument = new Argument<string>("gridname"), IsRequired = true });
            AddOption(new Option("--area", "Area of use") { Argument = new Argument<string>("area") });
            AddOption(new Option("--email", "Product manager") { Argument = new Argument<string>("email") });
            AddOption(new Option("--desc", "Description") { Argument = new Argument<string>("desc") });
            AddOption(new Option("--epsg2d", "Source EPSG interpolation CRS ('autority:XXXX')") { Argument = new Argument<string>("epsg2d"), IsRequired = true });
            AddOption(new Option("--epsg3d", "Source EPSG 3D CRS ('autority:XXXX')") { Argument = new Argument<string>("epsg3d"), IsRequired = true });
            AddOption(new Option("--epsgtarget", "Target EPSG CRS ('autority:XXXX')") { Argument = new Argument<string>("epsgtarget"), IsRequired = true });
            AddOption(new Option("--geoid", "Geoid- or separationmodel") { Argument = new Argument<bool>("geoid") });
            AddOption(new Option("--tilesize", "Tile size (multiple of 16)") { Argument = new Argument<int>("tilesize"), IsRequired = true });

            Handler = CommandHandler.Create((Gri2GeoTiffParams pars) =>
            {
                return HandleCommand(pars);
            });
        }

        private int HandleCommand(Gri2GeoTiffParams par)
        {
            try
            {
                var inputE = par.InputE;
                var inputN = par.InputN;
                var inputU = par.InputU;

                string inputEName = inputE != null ? inputE.FullName : "";
                string inputNName = inputN != null ? inputN.FullName : "";
                string inputUName = inputU != null ? inputU.FullName : "";

                var tiff = new GeoTiffFile(inputEName, inputNName, inputUName);
                
                tiff.Grid_name = par.GridName;
                tiff.ImageDescription = par.Desc ?? "";
                tiff.Area_of_use = par.Area ?? "";
                tiff.Email = par.Email ?? "";
                tiff.TileSize = par.TileSize;
                tiff.Epsg2d.CodeString = par.Epsg2d;
                tiff.Epsg3d.CodeString = par.Epsg3d;
                tiff.EpsgTarget.CodeString = par.EpsgTarget;
                tiff.Dimensions = par.Dim;
                tiff.TiffOutput = (GeoTiffFile.TiffOutputType)par.Type;

                if (!tiff.ReadGriFiles())
                {
                    Console.WriteLine($"Could not read gri files.");
                    return -1;
                }
                if (!tiff.GenerateGridFile(par.Output.FullName))
                {
                    Console.WriteLine($"Generation of tiff file {par.Output.Name} failed.");
                    return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return -1;
                throw ex;
            }
        }
    }

    public class Gtx2GeoTiffCommand : Command
    {
        public Gtx2GeoTiffCommand(string name, string description = null) : base(name, description)
        {
            Name = name;
            Description = description;

            AddArgument(new Argument<FileInfo>("input", "Input gtx file"));
            AddArgument(new Argument<FileInfo>("output", "Output GeoTiff file"));

            AddOption(new Option<GeoTiffFile.TiffOutputTypeshort>("--type", "TiffOutputType") { Argument = new Argument<GeoTiffFile.TiffOutputTypeshort>("type"), IsRequired = true });
            AddOption(new Option("--gridname", "Grid name") { Argument = new Argument<string>("gridname"), IsRequired = true });
            AddOption(new Option("--desc", "Description") { Argument = new Argument<string>("desc") });
            AddOption(new Option("--area", "Area of use") { Argument = new Argument<string>("area") });
            AddOption(new Option("--email", "Product manager") { Argument = new Argument<string>("email") });
            AddOption(new Option("--epsg2d", "Source EPSG interpolation CRS ('autority:XXXX')") { Argument = new Argument<string>("epsg2d"), IsRequired = true });
            AddOption(new Option("--epsg3d", "Source EPSG 3D CRS ('autority:XXXX')") { Argument = new Argument<string>("epsg3d"), IsRequired = true });
            AddOption(new Option("--epsgtarget", "Target EPSG CRS ('autority:XXXX')") { Argument = new Argument<string>("epsgtarget"), IsRequired = true });
            AddOption(new Option("--tilesize", "Tile size (multiple of 16)") { Argument = new Argument<int>("tilesize"), IsRequired = true });

            Handler = CommandHandler.Create((Gtx2GeoTiffParams pars) =>
            {
                return HandleCommand(pars);
            });
        }
        
        private int HandleCommand(Gtx2GeoTiffParams parameters)
        {
            try
            {
                var tiff = new GeoTiffFile();

                tiff.Grid_name = parameters.GridName;
                tiff.ImageDescription = parameters.Desc ?? "";
                tiff.Area_of_use = parameters.Area ?? "";
                tiff.Email = parameters.Email ?? "";
                tiff.TileSize = parameters.TileSize;
                tiff.Dimensions = 1;
                tiff.Epsg2d.CodeString = parameters.Epsg2d;
                tiff.Epsg3d.CodeString = parameters.Epsg3d;
                tiff.EpsgTarget.CodeString = parameters.EpsgTarget;
                tiff.TiffOutput = (GeoTiffFile.TiffOutputType)parameters.Type;

                if (!tiff.Gtx.ReadGtx(parameters.Input.FullName))
                {
                    Console.WriteLine($"Cound not read the gtx file {parameters.Input.Name}.");
                    return -1;
                }
                if (!tiff.GenerateGridFile(parameters.Output.FullName))
                {
                    Console.WriteLine($"Generation of tiff file {parameters.Output.Name} failed.");
                    return -1;
                }              
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return -1;
                throw ex;
            }
        }
    }
    
    public class Ct2Gtx2GeoTiffCommand : Command
    {    
        public Ct2Gtx2GeoTiffCommand(string name, string description = null) : base(name, description)
        {
            Name = name;
            Description = description;

            AddArgument(new Argument<FileInfo>("output", "Output GeoTiff file") { ArgumentType = typeof(FileInfo) });

            AddOption(new Option("--ct2", "Input ct2 file") { Argument = new Argument<FileInfo>("ct2") });
            AddOption(new Option("--gtx", "Input gtx file") { Argument = new Argument<FileInfo>("gtx") });
            AddOption(new Option<GeoTiffFile.TiffOutputTypeshort>("--type", "TiffOutputType") { Argument = new Argument<GeoTiffFile.TiffOutputTypeshort>("type"), IsRequired = true });
            AddOption(new Option("--gridname", "Grid name") { Argument = new Argument<string>("gridname"), IsRequired = true });
            AddOption(new Option("--desc", "Description") { Argument = new Argument<string>("desc") });
            AddOption(new Option("--area", "Area of use") { Argument = new Argument<string>("area") });
            AddOption(new Option("--email", "Product manager") { Argument = new Argument<string>("email") });
            AddOption(new Option("--dim", "Dimension") { Argument = new Argument<int>("dim") });

            // TODO: Are the EPSG codes correct? Make unit or factory tests.           
            AddOption(new Option("--epsg2d", "Source EPSG interpolation CRS ('autority:XXXX')") { Argument = new Argument<string>("epsg2d"), IsRequired = true });
            AddOption(new Option("--epsgsource", "Source EPSG CRS ('autority:XXXX')") { Argument = new Argument<string>("epsgsource"), IsRequired = true });
            AddOption(new Option("--epsgtarget", "Target EPSG CRS ('autority:XXXX')") { Argument = new Argument<string>("epsgtarget"), IsRequired = true });
            AddOption(new Option("--tilesize", "Tile size (multiple of 16)") { Argument = new Argument<int>("tilesize"), IsRequired = true });
         
            Handler = CommandHandler.Create((Ct2Gtx2GeoTiffParams pars) =>
            {
                return HandleCommand(pars);
            });
        }

        private int HandleCommand(Ct2Gtx2GeoTiffParams par)
        {
            try
            {
                var tiff = new GeoTiffFile();

                tiff.OutputFileName = par.Output.FullName;
                tiff.Grid_name = par.GridName;
                tiff.ImageDescription = par.Desc ?? "";
                tiff.Area_of_use = par.Area ?? "";
                tiff.Email = par.Email ?? "";
                tiff.TileSize = par.TileSize;
                tiff.Dimensions = (par.Dim == 0) ? 
                    ((par.Ct2 != null ? 2 : 0) + (par.Gtx != null ? 1 : 0)) :
                    par.Dim;    
                tiff.Epsg2d.CodeString = par.Epsg2d;
                tiff.EpsgSource.CodeString = par.EpsgSource;
                tiff.EpsgTarget.CodeString = par.EpsgTarget;
                tiff.TiffOutput = (GeoTiffFile.TiffOutputType)par.Type;
            
                if (par.Ct2 != null && !tiff.Ct2.ReadCt2(par.Ct2.FullName))
                {
                    Console.WriteLine($"Cound not read the ct2 file {par.Ct2.Name}.");
                    return -1;
                }
                if (par.Gtx != null && !tiff.Gtx.ReadGtx(par.Gtx.FullName))
                {
                    Console.WriteLine($"Cound not read the gtx file {par.Gtx.Name}.");
                    return -1;
                }
                if (!tiff.GenerateGridFile(par.Output.FullName))
                {
                    Console.WriteLine($"Feil i generering av tiff-fil {par.Output.Name}.");
                    return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return -1;
                throw ex;
            }
        }
    }
}
