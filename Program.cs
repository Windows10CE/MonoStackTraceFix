using AsmResolver.PE;
using AsmResolver.PE.Debug.CodeView;
using AsmResolver.PE.File;
using AsmResolver.Symbols.Pdb;
using AsmResolver.Symbols.Pdb.Records;

if (args is not [var monoPath])
{
    Console.Error.WriteLine($"Usage: {Environment.ProcessPath} [path to unity mono]");
    return 1;
}

var file = PEFile.FromFile(monoPath);
var image = PEImage.FromFile(file);

var debug = image.DebugData.Select(d => d.Contents).OfType<RsdsDataSegment>().FirstOrDefault();

if (debug is not { Path: not null })
{
    Console.Error.WriteLine($"{monoPath} does not have a PDB associated with it");
    return 1;
}

var fileName = Path.GetFileName(debug.Path.Replace('\\', '/').TrimEnd('\0'));

var pdbPath = $"https://symbolserver.unity3d.com/{fileName}/{debug.Guid.ToString("N").ToUpper()}{debug.Age:X}/{fileName}";

Console.WriteLine($"Downloading: {pdbPath}");

var client = new HttpClient();

var pdb = PdbImage.FromBytes(await client.GetByteArrayAsync(pdbPath));

var sym = pdb.Symbols.OfType<DataSymbol>().FirstOrDefault(s => s.Name == "restore_stack");

if (sym is null)
{
    Console.Error.WriteLine("Could not find restore_stack");
    return 1;
}

Console.WriteLine($"restore_stack offset: 0x{file.Sections[sym.SegmentIndex - 1].Rva + sym.Offset:x}");

return 0;
